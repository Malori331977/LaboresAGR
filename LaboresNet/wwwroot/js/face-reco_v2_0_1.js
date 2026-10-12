let video = document.getElementById('video');
let registeredDescriptor = null;
let verifyDescriptor = null;
let captures = [];
let stream = null;
let _selectedCameraDeviceId = null;

let landmarks1;
let landmarks0;

let dotNetHelper;
let contador = 0;
// Persisted log history (IndexedDB-backed)
let savedLogs = [];
let ssdLoaded = false;

// Reusable offscreen canvas to avoid creating a new one for each frame
let _offscreenCanvas = null;
let _offscreenContext = null;

// tiny face detector options for better performance
let tinyOptions = null;

// --- IndexedDB simple key/value helpers with in-memory fallback ---
// If IndexedDB is unavailable (iOS private browsing, restricted WebViews,
// QuotaExceededError, or the iOS Safari bug where open() never resolves),
// all operations fall back transparently to an in-memory Map so the face
// recognition flow keeps working for the duration of the session.
const IDB_DB_NAME = 'faceRecoDB';
const IDB_STORE_NAME = 'kv';
const IDB_CAMERA_KEY = 'preferredCameraId';
let _cachedDb = null;
let _idbAvailable = null;  // null = not tested yet, true/false after first attempt
const _memoryStore = new Map();  // fallback storage

// Timeout for IndexedDB open – covers the iOS Safari bug where the request
// stays pending forever in private browsing mode.
const IDB_OPEN_TIMEOUT_MS = 3000;

function openIdb() {
    if (_idbAvailable === false) return Promise.reject(new Error('IndexedDB not available'));
    if (_cachedDb) return Promise.resolve(_cachedDb);

    return new Promise((resolve, reject) => {
        // Guard: indexedDB global may not exist in some restricted WebViews
        if (typeof indexedDB === 'undefined' || !indexedDB) {
            _idbAvailable = false;
            reject(new Error('IndexedDB not supported'));
            return;
        }

        var timer = null;
        var settled = false;

        try {
            var req = indexedDB.open(IDB_DB_NAME, 1);

            req.onupgradeneeded = function () {
                try {
                    var db = req.result;
                    if (!db.objectStoreNames.contains(IDB_STORE_NAME)) db.createObjectStore(IDB_STORE_NAME);
                } catch (e) {
                    // QuotaExceededError or SecurityError during upgrade
                    if (!settled) { settled = true; clearTimeout(timer); _idbAvailable = false; reject(e); }
                }
            };
            req.onsuccess = function () {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                _cachedDb = req.result;
                _cachedDb.onclose = function () { _cachedDb = null; };
                _idbAvailable = true;
                resolve(_cachedDb);
            };
            req.onerror = function (e) {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                _idbAvailable = false;
                reject(req.error || e);
            };

            // iOS Safari private mode: open() may never fire onsuccess/onerror
            timer = setTimeout(function () {
                if (!settled) {
                    settled = true;
                    _idbAvailable = false;
                    console.warn('IndexedDB open timed out – falling back to memory storage');
                    reject(new Error('IndexedDB open timeout'));
                }
            }, IDB_OPEN_TIMEOUT_MS);

        } catch (e) {
            // SecurityError thrown synchronously in some environments
            if (!settled) { settled = true; clearTimeout(timer); _idbAvailable = false; reject(e); }
        }
    });
}

async function idbGet(key) {
    try {
        var db = await openIdb();
        return await new Promise(function (resolve, reject) {
            var tx = db.transaction(IDB_STORE_NAME, 'readonly');
            var store = tx.objectStore(IDB_STORE_NAME);
            var rq = store.get(key);
            rq.onsuccess = function () { resolve(rq.result); };
            rq.onerror = function () { reject(rq.error); };
        });
    } catch (e) {
        // Fallback to memory
        return _memoryStore.has(key) ? _memoryStore.get(key) : null;
    }
}

async function idbSet(key, value) {
    // Always keep memory copy in sync so fallback reads are current
    _memoryStore.set(key, value);
    try {
        var db = await openIdb();
        return await new Promise(function (resolve, reject) {
            var tx = db.transaction(IDB_STORE_NAME, 'readwrite');
            var store = tx.objectStore(IDB_STORE_NAME);
            var rq = store.put(value, key);
            rq.onsuccess = function () { resolve(true); };
            rq.onerror = function () { reject(rq.error); };
        });
    } catch (e) {
        console.warn('idbSet fell back to memory for key:', key);
        return false;
    }
}

async function idbRemove(key) {
    _memoryStore.delete(key);
    try {
        var db = await openIdb();
        return await new Promise(function (resolve, reject) {
            var tx = db.transaction(IDB_STORE_NAME, 'readwrite');
            var store = tx.objectStore(IDB_STORE_NAME);
            var rq = store.delete(key);
            rq.onsuccess = function () { resolve(true); };
            rq.onerror = function () { reject(rq.error); };
        });
    } catch (e) {
        console.warn('idbRemove fell back to memory for key:', key);
        return false;
    }
}

async function initSavedLogs() {
    try {
        var existing = await idbGet('faceLogs');
        if (existing) savedLogs = existing || [];
    } catch (err) {
        console.error('Error initializing faceLogs from IndexedDB:', err);
        savedLogs = [];
    }
}
// -------------------------------------------

// Returns [{deviceId, label}] for all video input devices.
// Falls back to generic labels when permission hasn't been granted yet.
async function getCameras() {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices
            .filter(d => d.kind === 'videoinput')
            .map((d, i) => ({
                deviceId: d.deviceId,
                label: d.label || `Cámara ${i + 1}`
            }));
    } catch (e) {
        console.warn('getCameras error', e);
        return [];
    }
}

// Persists the preferred camera deviceId in IndexedDB and applies it for subsequent
// getUserMedia calls. Pass null or '' to clear the preference.
async function setPreferredCamera(deviceId) {
    _selectedCameraDeviceId = deviceId || null;
    try {
        await idbSet(IDB_CAMERA_KEY, deviceId || '');
    } catch (e) {
        console.warn('setPreferredCamera: could not persist preference', e);
    }
}

// Reads the persisted camera preference from IndexedDB, applies it in memory,
// and returns the deviceId string (or null if none was saved).
async function loadPreferredCamera() {
    try {
        const id = await idbGet(IDB_CAMERA_KEY);
        if (id) {
            _selectedCameraDeviceId = id;
            return id;
        }
    } catch (e) {
        console.warn('loadPreferredCamera error', e);
    }
    return null;
}

async function _acquireCamera(constraints) {
    // Release any existing stream before requesting a new one to avoid AbortError/NotReadableError
    if (stream) {
        try { stream.getTracks().forEach(t => t.stop()); } catch (e) {}
        stream = null;
    }
    if (video && video.srcObject) {
        try { video.srcObject = null; } catch (e) {}
    }
    // Brief pause so the platform camera HAL has time to release hardware
    await new Promise(r => setTimeout(r, 300));

    // Use specific device if one was selected; otherwise use the provided constraints as-is
    const finalConstraints = _selectedCameraDeviceId
        ? { video: { deviceId: { exact: _selectedCameraDeviceId }, width: constraints.video && constraints.video.width, height: constraints.video && constraints.video.height } }
        : constraints;

    try {
        return await navigator.mediaDevices.getUserMedia(finalConstraints);
    } catch (e) {
        if (e.name === 'AbortError' || e.name === 'NotReadableError' || e.name === 'OverconstrainedError') {
            console.warn('face-reco: getUserMedia retry with minimal constraints after', e.name);
            await new Promise(r => setTimeout(r, 500));
            // On retry, keep deviceId preference but drop resolution constraints
            const retryConstraints = _selectedCameraDeviceId
                ? { video: { deviceId: { exact: _selectedCameraDeviceId } } }
                : { video: { facingMode: 'user' } };
            return await navigator.mediaDevices.getUserMedia(retryConstraints);
        }
        throw e;
    }
}

async function startMedia() {
    try {
        captures = [];
        savedLogs = [];
        // remove previous stored descriptors from IndexedDB
        await idbRemove('faceRec');
        await idbRemove('face1');
        await idbRemove('face2');
        await idbRemove('face3');
        await idbRemove('face4');
        await idbRemove('face5');
        await idbRemove('faceLogs');

        video = document.getElementById('video');

        const constraints = {
            video: {
                width: { ideal: 640 },
                height: { ideal: 480 },
                facingMode: 'user'
            }
        };

        stream = await _acquireCamera(constraints);

        // Asignar el flujo de medios al elemento de video
        video.srcObject = stream;

        // Try to enable torch if available
        try {
            const [track] = stream.getVideoTracks();
            const caps = track.getCapabilities ? track.getCapabilities() : null;
            if (caps && caps.torch) {
                try { await track.applyConstraints({ advanced: [{ torch: true }] }); }
                catch (e) { console.warn('Torch enable failed or not permitted', e); }
            }
        } catch (e) { console.warn('Could not check/enable torch', e); }

        // Mostrar el video en pantalla
        video.play();

        await loadModels();
        return 1;
    }
    catch (error) {
        console.error('Error al acceder a la cámara. ', error);
        return 0;
    }
}

async function startCamera() {
    try {
        video = document.getElementById('video');

        const constraints = {
            video: {
                width: { ideal: 640 },
                height: { ideal: 480 },
                facingMode: 'user'
            }
        };

        stream = await _acquireCamera(constraints);

        // Asignar el flujo de medios al elemento de video
        video.srcObject = stream;

        // Try to enable torch if available
        try {
            const [track] = stream.getVideoTracks();
            const caps = track.getCapabilities ? track.getCapabilities() : null;
            if (caps && caps.torch) {
                try { await track.applyConstraints({ advanced: [{ torch: true }] }); }
                catch (e) { console.warn('Torch enable failed or not permitted', e); }
            }
        } catch (e) { console.warn('Could not check/enable torch', e); }

        // Mostrar el video en pantalla
        video.play();
        await loadModels();
        return 1;
    }
    catch (error) {
        console.error('Error al acceder a la cámara. ', error);
        return 0;
    }
}

async function stopMedia() {
    if (video != null) {
        // Detener todas las pistas si existen
        const tracks = video.srcObject?.getTracks();
        if (tracks && tracks.length > 0) {
            tracks.forEach(function (track) { try { track.stop(); } catch (e) { console.warn('Error stopping track', e); } });
        }

        // Liberar la referencia al stream para asegurar la liberación de recursos
        try { video.srcObject = null; } catch (e) { console.warn('Error clearing video.srcObject', e); }
    }
}

async function clearAll() {
    savedLogs = []
    await idbSet('faceRec', '');
    await idbSet('face1', '');
    await idbSet('face2', '');
    await idbSet('face3', '');
    await idbSet('face4', '');
    await idbSet('face5', '');
    await idbSet('faceLogs', savedLogs);
    await idbSet('distancia', '');
    await idbSet('textura', '');
    await idbSet('idnum', '');
}

async function clearFaces() {
    await idbSet('faceRec', '');
    await idbSet('face1', '');
    await idbSet('face2', '');
    await idbSet('face3', '');
    await idbSet('face4', '');
    await idbSet('face5', '');
    await idbSet('faceVerify', '');
    savedLogs = []
    await idbSet('faceLogs', savedLogs);
}

// --- TensorFlow.js backend management ---
// Evaluates WebGL support initially and attempts fallback to WASM or CPU.
let _webglFailed = false;

function _getTf() {
    return faceapi.tf || (typeof window !== 'undefined' ? window.tf : null) || null;
}

function checkWebGLSupport() {
    try {
        const canvas = document.createElement('canvas');
        return !!(window.WebGLRenderingContext && (canvas.getContext('webgl2') || canvas.getContext('webgl') || canvas.getContext('experimental-webgl')));
    } catch (e) {
        return false;
    }
}

function showFallbackAlert(backendType) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Modo de compatibilidad',
            text: 'Tu dispositivo no tiene soporte WebGL completo. El reconocimiento será más lento (' + backendType + ').',
            icon: 'info',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 5000
        });
    }
}

async function setupFallbackBackend(tf) {
    try {
        // Intentar WASM si la librería está disponible (generalmente más rápido que CPU)
        await tf.setBackend('wasm');
        await tf.ready();
        console.log('face-api: WASM backend active (faster fallback)');
        showFallbackAlert('WASM');
    } catch (errWasm) {
        // Fallback final a CPU
        try {
            await tf.setBackend('cpu');
            await tf.ready();
            console.log('face-api: CPU backend active (slower but compatible)');
            showFallbackAlert('CPU');
        } catch (e2) {
            console.error('face-api: CPU backend setup also failed', e2);
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'Error de Compatibilidad',
                    text: 'Tu dispositivo no soporta el motor necesario para el reconocimiento facial.',
                    icon: 'error'
                });
            }
        }
    }
}

async function switchToCpuBackend() {
    if (_webglFailed) return;
    _webglFailed = true;
    var tf = _getTf();
    if (!tf) return;
    await setupFallbackBackend(tf);
}

async function ensureTfBackend() {
    var tf = _getTf();
    if (!tf) { console.warn('face-api: tf reference not available'); return; }

    const hasWebGL = checkWebGLSupport();
    if (!hasWebGL) {
        console.warn('face-api: No WebGL context available. Forcing fallback backend.');
        _webglFailed = true;
        await setupFallbackBackend(tf);
        return;
    }

    try {
        await tf.setBackend('webgl');
        await tf.ready();
        // Use a tensor size close to the actual model input (224×224×3)
        // to trigger the same complex im2col / convolution shaders.
        var input = tf.zeros([1, 112, 112, 3]);
        var filter = tf.zeros([3, 3, 3, 32]);
        var conv = tf.conv2d(input, filter, 1, 'same');
        conv.dataSync();           // forces shader compilation
        tf.dispose([input, filter, conv]);
        console.log('face-api: WebGL backend verified');
    } catch (e) {
        console.warn('face-api: WebGL shader test failed, switching to fallback.', e.message || e);
        _webglFailed = true;
        await setupFallbackBackend(tf);
    }
}

// Safe wrapper around faceapi.detectSingleFace that catches WebGL shader
// errors at runtime, switches to fallback, and retries the detection once.
async function safeDetectSingleFace(canvas, options) {
    try {
        return await faceapi.detectSingleFace(canvas, options)
            .withFaceLandmarks(true)
            .withFaceDescriptor();
    } catch (e) {
        var msg = (e.message || '').toLowerCase();
        if (!_webglFailed && (msg.indexOf('shader') !== -1 || msg.indexOf('compile') !== -1 || msg.indexOf('webgl') !== -1)) {
            console.warn('face-api: WebGL error during detection, switching to fallback and retrying.', e.message);
            await switchToCpuBackend();
            try {
                return await faceapi.detectSingleFace(canvas, options)
                    .withFaceLandmarks(true)
                    .withFaceDescriptor();
            } catch (e2) {
                console.error('face-api: detection failed on fallback backend too', e2);
                return null;
            }
        }
        console.error('face-api: detection error', e);
        return null;
    }
}

async function loadModels() {
    try {
        const MODEL_URL = '/faceapi-models';

        // initialize saved logs from IndexedDB
        await initSavedLogs();

        // Verify WebGL or fall back to CPU before loading any model
        await ensureTfBackend();

        // Load tiny models
        await faceapi.nets.tinyFaceDetector.loadFromUri(MODEL_URL);
        await faceapi.nets.faceLandmark68TinyNet.loadFromUri(MODEL_URL);
        await faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL);

        // inputSize 224 is faster than 320 while still accurate enough for frontal faces
        tinyOptions = new faceapi.TinyFaceDetectorOptions({ inputSize: 224, scoreThreshold: 0.35 });

        // ssdMobilenetv1 loaded lazily on demand (see loadSsdIfNeeded)
        ssdLoaded = false;
    }
    catch (error) { console.error('Error al cargar modelos para reconocimiento facial:', error); }
}

// Lazy-load ssdMobilenetv1 only when actually needed (low-light fallback)
async function loadSsdIfNeeded() {
    if (ssdLoaded) return true;
    try {
        const MODEL_URL = '/faceapi-models';
        await faceapi.nets.ssdMobilenetv1.loadFromUri(MODEL_URL);
        ssdLoaded = true;
        return true;
    } catch (e) {
        console.warn('ssdMobilenetv1 failed to load', e);
        return false;
    }
}

// Detect face with tiny detector, fallback to ssd only in low light
async function detectFaceWithFallback(canvas) {
    let detection = await safeDetectSingleFace(canvas, tinyOptions);

    if (!detection && _offscreenContext) {
        const lum = meanLuminanceSampled(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height);
        if (lum < 60) {
            const loaded = await loadSsdIfNeeded();
            if (loaded) {
                try {
                    detection = await safeDetectSingleFace(canvas, new faceapi.SsdMobilenetv1Options({ minConfidence: 0.35 }));
                } catch (e) { console.warn('ssd fallback detection error', e); }
            }
        }
    }
    return detection;
}

async function registerFace() {
    const face1 = (await idbGet('face1')) || '';
    const face2 = (await idbGet('face2')) || '';
    const face3 = (await idbGet('face3')) || '';
    const face4 = (await idbGet('face4')) || '';
    const face5 = (await idbGet('face5')) || '';
    const faceText = Number((await idbGet('textura')) || 0);

    if (face1 === '' || face2 === '' || face3 === '' || face4 === '' || face5 === '') {
        const canvas = getEnhancedCanvasFromVideo(video);
        const detection = await detectFaceWithFallback(canvas);

        if (!detection) {
            Swal.fire({ title: 'Atencion', text: 'No se logró detectar un rostro en el video, por favor acerque su rostro más a la cámara o mejore la iluminación.', icon: 'error' });
            return 0;
        }

        // Reuse the already-drawn offscreen canvas data (no double processing)
        const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
        const texture = calculateTextureSampled(imgData);

        if (texture > faceText) {
            registeredDescriptor = detection.descriptor;
            const descriptorString = Array.from(detection.descriptor).toString();
            await idbSet('faceRec', descriptorString);

            if (face1 === '') await idbSet('face1', descriptorString);
            else if (face2 === '') await idbSet('face2', descriptorString);
            else if (face3 === '') await idbSet('face3', descriptorString);
            else if (face4 === '') await idbSet('face4', descriptorString);
            else await idbSet('face5', descriptorString);

            Swal.fire({ title: 'Felicidades', text: 'El rostro se ha registrado correctamente.', icon: 'success' });
            return 1;
        }
        else {
            Swal.fire({ title: 'Atencion', text: 'No se ha logrado capturar el rostro, por favor acerque su rostro más a la cámara. (Textura: ' + texture + ' > Referencia: ' + faceText + ')', icon: 'error' });
            return 0;
        }
    }
    else {
        Swal.fire({ title: 'Atención', text: 'Ya se realizó la captura de los rostros requerida por el proceso. Puede proceder a guardar los datos.', icon: 'info' });
        return 0;
    }
}

async function verifyFace() {
    captures = [];
    contador = 0;
    const maxCaptures = 5;
    const faceText = Number((await idbGet('textura')) || 0);
    const idnum = Number((await idbGet('idnum')) || 0);
    let texturesDetected = 0;
    const pendingLogs = [];

    for (let i = 0; i < maxCaptures; i++) {
        const canvas = getEnhancedCanvasFromVideo(video);
        const currentFace = await safeDetectSingleFace(canvas, tinyOptions);

        if (!currentFace) {
            // attempt ssd fallback if low light
            if (_offscreenContext) {
                const lum = meanLuminanceSampled(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height);
                if (lum < 60) {
                    const loaded = await loadSsdIfNeeded();
                    if (loaded) {
                        try {
                            const ssd = await safeDetectSingleFace(canvas, new faceapi.SsdMobilenetv1Options({ minConfidence: 0.35 }));
                            if (ssd) {
                                const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
                                captures.push({ face: ssd, imgData });
                                await new Promise(resolve => setTimeout(resolve, 50));
                                continue;
                            }
                        } catch (e) { console.warn('ssd fallback error', e); }
                    }
                }
            }
            return 0;
        }

        // Reuse already-drawn offscreen canvas (no double getEnhancedCanvasFromVideo call)
        const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
        captures.push({ face: currentFace, imgData });

        // Check texture immediately for early exit
        const texture = calculateTextureSampled(imgData);
        contador++;
        pendingLogs.push('Id: ' + idnum + ' Item: ' + contador + '. Textura (valor > referencia) Valor: ' + texture + ' Referencia: ' + faceText);

        if (texture > faceText) {
            texturesDetected++;
            if (texturesDetected >= 2) {
                // Batch write all logs at once before returning
                for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
                await idbSet('faceLogs', savedLogs);
                return 1;
            }
        }

        // shorter delay between captures
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    // Batch write logs
    for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
    await idbSet('faceLogs', savedLogs);

    return 0;
}

async function compareFace() {
    const faceRec = await idbGet('faceVerify');

    if (!faceRec) { Swal.fire({ title: 'Atencion', text: 'No existen rostros que comparar.', icon: 'warning' }); return 0; }

    const data = faceRec.split(',');

    if (!data || data.length === 0) { Swal.fire({ title: 'Atencion', text: 'Descriptor inválido.', icon: 'warning' }); return 0; }

    const array = new Float32Array(data.length);
    for (let i = 0; i < data.length; i++) array[i] = parseFloat(data[i]);

    const compareDescriptor = array;
    const bestDistance = Number((await idbGet('distancia')) || 0);
    const idnum = Number((await idbGet('idnum')) || 0);
    const pendingLogs = [];

    for (const item of captures) {
        const distance = faceapi.euclideanDistance(compareDescriptor, item.face.descriptor);

        contador++;
        pendingLogs.push('Id: ' + idnum + ' Item: ' + contador + '. Distancia (valor<referencia) Valor:' + distance + ' Referencia:' + bestDistance);

        if (distance < bestDistance) {
            // Batch write logs before returning
            for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
            await idbSet('faceLogs', savedLogs);
            return 1;
        }
    }

    // Batch write logs
    for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
    await idbSet('faceLogs', savedLogs);
    return 0;
}

function calculateTexture(imageData) {
    let totalTexture = 0;
    for (let i = 0; i < imageData.data.length; i += 4) {
        const r = imageData.data[i];
        const g = imageData.data[i + 1];
        const b = imageData.data[i + 2];
        totalTexture += Math.abs(r - g) + Math.abs(g - b) + Math.abs(b - r);
    }
    return totalTexture / (imageData.data.length / 4);
}

// Sampled texture: process every 4th pixel (~4x faster)
function calculateTextureSampled(imageData) {
    let totalTexture = 0;
    let count = 0;
    const step = 16; // every 4th pixel = step of 16 bytes
    for (let i = 0; i < imageData.data.length; i += step) {
        const r = imageData.data[i];
        const g = imageData.data[i + 1];
        const b = imageData.data[i + 2];
        totalTexture += Math.abs(r - g) + Math.abs(g - b) + Math.abs(b - r);
        count++;
    }
    return totalTexture / count;
}

// simple mean luminance helper
function meanLuminance(imageData) {
    let s = 0;
    const data = imageData.data;
    for (let i = 0; i < data.length; i += 4) {
        s += 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2];
    }
    return s / (data.length / 4);
}

// Sampled luminance: read directly from canvas context (no extra getImageData)
function meanLuminanceSampled(ctx, width, height) {
    const imgData = ctx.getImageData(0, 0, width, height);
    const data = imgData.data;
    let s = 0;
    let count = 0;
    const step = 16;
    for (let i = 0; i < data.length; i += step) {
        s += 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2];
        count++;
    }
    return s / count;
}

// image enhancement: gamma and contrast
function enhanceImage(ctx, width, height, gamma = 1.2, contrast = 20) {
    try {
        const img = ctx.getImageData(0, 0, width, height);
        const data = img.data;
        const factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
        for (let i = 0; i < data.length; i += 4) {
            for (let c = 0; c < 3; c++) {
                // normalize
                let v = data[i + c] / 255;
                // gamma correction (invert gamma param)
                v = Math.pow(v, 1 / gamma);
                // scale back
                v = v * 255;
                // contrast
                v = factor * (v - 128) + 128;
                data[i + c] = Math.max(0, Math.min(255, v));
            }
        }
        ctx.putImageData(img, 0, 0);
    }
    catch (e) {
        console.warn('enhanceImage failed', e);
    }
}

function getEnhancedCanvasFromVideo(video) {
    if (!_offscreenCanvas) {
        _offscreenCanvas = document.createElement('canvas');
        _offscreenContext = _offscreenCanvas.getContext('2d', { willReadFrequently: true });
    }

    _offscreenCanvas.width = video.videoWidth || 640;
    _offscreenCanvas.height = video.videoHeight || 480;
    if (!_offscreenContext) return _offscreenCanvas;

    _offscreenContext.drawImage(video, 0, 0, _offscreenCanvas.width, _offscreenCanvas.height);

    // measure luminance using sampled version (faster)
    const lum = meanLuminanceSampled(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height);

    if (lum < 80) {
        enhanceImage(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height, 1.1, 30);
    }
    else if (lum < 120) {
        enhanceImage(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height, 1.15, 18);
    }
    return _offscreenCanvas;
}

async function sendLog(message) {
    if (!message) return;
    try {
        savedLogs.push({ ts: new Date().toISOString(), msg: message });
        await idbSet('faceLogs', savedLogs);
    } catch (err) {
        console.error('Error saving faceLogs to IndexedDB:', err);
    }
}

function getImageDataFromVideo(video) {
    if (!_offscreenContext) {
        const canvas = getEnhancedCanvasFromVideo(video);
    }
    if (!_offscreenContext) return { data: [] };
    return _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
}

async function getFaceLogsFromIdb() {
    try {
        const logs = await idbGet('faceLogs');
        return logs || [];
    } catch (e) {
        console.warn('getFaceLogsFromIdb failed', e);
        return [];
    }
}

window.interopDemo = (function () {
    var reference = null;
    return {
        initialize: function (ref) { reference = ref; },
        sweetAlertDisplay: function (message, level) { reference.invokeMethodAsync('SweetAlertDisplay', message, level); }
    };
})();




