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
// Copia con mejora de brillo/contraste usada solo para la deteccion
let _enhCanvas = null;
let _enhContext = null;

// tiny face detector options for better performance
let tinyOptions = null;

// --- Parametros de verificacion ---
const DETECTOR_INPUT_SIZE = 320;
// Ventana de tiempo maxima para obtener capturas validas (en vez de 5 fotogramas fijos)
const VERIFY_WINDOW_MS = 4000;
const VERIFY_MAX_FRAMES = 25;
// Capturas con textura valida requeridas antes de comparar
const VERIFY_MIN_TEXTURE_OK = 2;
// Plantillas registradas que deben coincidir (se limita al total que tenga el colaborador)
const MIN_TEMPLATE_MATCHES = 2;
// Validacion de rostro completo (ver faceIncompleteReason)
const FACE_MIN_SCORE = 0.4;        // score minimo del detector (el detector acepta desde 0.35)
const FACE_MAX_OUTSIDE = 0.15;     // fraccion maxima de la caja que puede quedar fuera del encuadre
const FACE_MIN_ASPECT = 0.6;       // ancho/alto minimo de la caja
const FACE_MAX_ASPECT = 1.3;       // ancho/alto maximo de la caja
const FACE_MIN_JAW_RATIO = 0.6;    // ancho de mandibula / ancho de caja
const FACE_MIN_SYMMETRY = 0.4;     // relacion izquierda/derecha de la nariz respecto a la mandibula

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
    let hadActiveCamera = false;
    if (stream) {
        try { stream.getTracks().forEach(t => t.stop()); } catch (e) {}
        stream = null;
        hadActiveCamera = true;
    }
    if (video && video.srcObject) {
        try { video.srcObject = null; } catch (e) {}
        hadActiveCamera = true;
    }
    // Pausa breve solo si habia una camara activa que liberar (HAL de la plataforma)
    if (hadActiveCamera) await new Promise(r => setTimeout(r, 300));

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

// Espera a que el video tenga fotogramas reales (dimensiones y datos). Con los modelos
// precargados, startMedia/startCamera ya no esperan la carga y de lo contrario se podria
// analizar un fotograma vacio. El pequeno margen final deja estabilizar exposicion/balance.
async function waitForVideoReady(vid, timeoutMs) {
    timeoutMs = timeoutMs || 3000;
    const t0 = Date.now();
    try { if (vid && vid.paused) await vid.play(); } catch (e) { /* autoplay ya activo */ }
    while (vid && (vid.readyState < 2 || !vid.videoWidth) && (Date.now() - t0) < timeoutMs) {
        await new Promise(r => setTimeout(r, 50));
    }
    await new Promise(r => setTimeout(r, 150));
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
                width: { ideal: 1280 },
                height: { ideal: 720 },
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

        await preloadFaceModels();
        await waitForVideoReady(video);
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
                width: { ideal: 1280 },
                height: { ideal: 720 },
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
        await preloadFaceModels();
        await waitForVideoReady(video);
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

let _modelsReady = false;
let _modelsLoadingPromise = null;

// Precarga los modelos y el backend de TF una sola vez por sesion de la app,
// para que las marcas siguientes no paguen ese costo.
function preloadFaceModels() {
    if (_modelsReady) return Promise.resolve(true);
    if (!_modelsLoadingPromise) {
        _modelsLoadingPromise = loadModels()
            .then(function () { return _modelsReady ? warmupInference().then(function () { return true; }) : false; })
            .catch(function () { return false; })
            .finally(function () { _modelsLoadingPromise = null; });
    }
    return _modelsLoadingPromise;
}

// Cargar los pesos no basta: la primera inferencia compila los shaders de WebGL (o inicializa
// WASM) y tarda varios segundos. Se ejecuta cada red una vez sobre una imagen en blanco para
// que esa compilacion ocurra en la precarga y no durante la primera marca.
let _inferenceWarmed = false;
async function warmupInference() {
    if (_inferenceWarmed) return;
    _inferenceWarmed = true;
    const t0 = Date.now();
    try {
        const c = document.createElement('canvas');
        c.width = 320; c.height = 240;
        const ctx = c.getContext('2d');
        ctx.fillStyle = '#808080';
        ctx.fillRect(0, 0, c.width, c.height);
        try { await faceapi.detectSingleFace(c, tinyOptions); } catch (e) { console.warn('warmup detector', e); }

        const small = document.createElement('canvas');
        small.width = 112; small.height = 112;
        small.getContext('2d').drawImage(c, 0, 0, 112, 112);
        try { await faceapi.nets.faceLandmark68TinyNet.detectLandmarks(small); } catch (e) { console.warn('warmup landmarks', e); }

        const rec = document.createElement('canvas');
        rec.width = 150; rec.height = 150;
        rec.getContext('2d').drawImage(c, 0, 0, 150, 150);
        try { await faceapi.nets.faceRecognitionNet.computeFaceDescriptor(rec); } catch (e) { console.warn('warmup descriptor', e); }
    } catch (e) {
        console.warn('warmupInference failed', e);
    }
    console.log('face-api: warmup de inferencia en ' + (Date.now() - t0) + ' ms');
}

// Dispara la precarga sin esperar (para llamarla desde Blazor sin bloquear la UI)
function warmupFaceModels() {
    try { preloadFaceModels(); } catch (e) { console.warn('warmupFaceModels', e); }
    return true;
}

async function loadModels() {
    try {
        // Si ya estan cargados solo se reaplican las opciones
        if (_modelsReady && faceapi.nets.tinyFaceDetector.isLoaded && faceapi.nets.faceLandmark68TinyNet.isLoaded && faceapi.nets.faceRecognitionNet.isLoaded) {
            if (!tinyOptions) tinyOptions = new faceapi.TinyFaceDetectorOptions({ inputSize: DETECTOR_INPUT_SIZE, scoreThreshold: 0.35 });
            await initSavedLogs();
            return;
        }

        const MODEL_URL = '/faceapi-models';

        // initialize saved logs from IndexedDB
        await initSavedLogs();

        // Verify WebGL or fall back to CPU before loading any model
        await ensureTfBackend();

        // Load tiny models
        await faceapi.nets.tinyFaceDetector.loadFromUri(MODEL_URL);
        await faceapi.nets.faceLandmark68TinyNet.loadFromUri(MODEL_URL);
        await faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL);

        // inputSize 320 detecta mejor rostros con angulo o a mayor distancia que 224
        tinyOptions = new faceapi.TinyFaceDetectorOptions({ inputSize: DETECTOR_INPUT_SIZE, scoreThreshold: 0.35 });

        // ssdMobilenetv1 loaded lazily on demand (see loadSsdIfNeeded)
        ssdLoaded = false;
        _modelsReady = true;
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
        if (!tinyOptions || !_modelsReady) {
            console.warn('registerFace: modelos no listos, reintentando carga', { tinyOptions: !!tinyOptions, ready: _modelsReady });
            await preloadFaceModels();
        }
        if (!tinyOptions) {
            console.error('registerFace: los modelos no se pudieron cargar (/faceapi-models)');
            Swal.fire({ title: 'Atención', text: 'No se pudieron cargar los modelos de reconocimiento facial.', icon: 'error' });
            return 0;
        }

        // Reintenta durante una ventana de tiempo: el primer fotograma suele salir sin rostro
        // (exposición de la cámara, movimiento) y antes se cancelaba el registro de inmediato.
        let detection = null;
        let canvas = null;
        const startedAt = Date.now();
        let attempts = 0;
        let rejectReason = '';
        while (!detection && (Date.now() - startedAt) < 4000) {
            attempts++;
            canvas = getEnhancedCanvasFromVideo(video);
            detection = await detectFaceWithFallback(canvas);
            if (detection) {
                const incomplete = faceIncompleteReason(detection);
                if (incomplete) {
                    rejectReason = incomplete;
                    detection = null;
                }
            }
            if (!detection) await new Promise(r => setTimeout(r, 100));
        }
        console.log('registerFace: intentos=' + attempts + ' rostro=' + !!detection +
            ' video=' + video.videoWidth + 'x' + video.videoHeight + ' readyState=' + video.readyState);

        if (!detection) {
            const msg = rejectReason
                ? 'El rostro se ve incompleto o de lado. Coloque todo su rostro de frente dentro del encuadre, sin cubrirlo. (' + rejectReason + ')'
                : 'No se logró detectar un rostro en el video, por favor acerque su rostro más a la cámara o mejore la iluminación.';
            Swal.fire({ title: 'Atencion', text: msg, icon: 'error' });
            return 0;
        }

        // Reuse the already-drawn offscreen canvas data (no double processing)
        const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
        const texture = calculateFaceTexture(imgData, detection.detection && detection.detection.box);

        console.log('registerFace: textura=' + texture + ' referencia=' + faceText);
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
        return 2; // ya estaba completo: no hay nada que reintentar
    }
}

// Captura rostros durante una ventana de tiempo (VERIFY_WINDOW_MS) en lugar de 5 fotogramas fijos.
// Un fotograma sin rostro (parpadeo, movimiento) ya no cancela el intento: simplemente se reintenta.
// Retorna 1 cuando hay al menos VERIFY_MIN_TEXTURE_OK capturas con textura válida.
async function verifyFace() {
    if (!tinyOptions) {
        Swal.fire({ title: 'Atención', text: 'Los modelos de reconocimiento facial no están listos. Por favor recargue la página e intente nuevamente.', icon: 'error' });
        return 0;
    }

    captures = [];
    contador = 0;
    const faceText = Number((await idbGet('textura')) || 0);
    const idnum = Number((await idbGet('idnum')) || 0);
    let texturesDetected = 0;
    let misses = 0;
    const pendingLogs = [];
    const startedAt = Date.now();

    for (let i = 0; i < VERIFY_MAX_FRAMES && (Date.now() - startedAt) < VERIFY_WINDOW_MS; i++) {
        const canvas = getEnhancedCanvasFromVideo(video);
        let currentFace = await safeDetectSingleFace(canvas, tinyOptions);

        if (!currentFace && _offscreenContext) {
            // En poca luz se intenta con ssd (solo si la luminancia del fotograma original es baja)
            const lum = meanLuminanceSampled(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height);
            if (lum < 60 && (await loadSsdIfNeeded())) {
                try {
                    currentFace = await safeDetectSingleFace(canvas, new faceapi.SsdMobilenetv1Options({ minConfidence: 0.35 }));
                } catch (e) { console.warn('ssd fallback error', e); }
            }
        }

        if (!currentFace) {
            misses++;
            await new Promise(resolve => setTimeout(resolve, 60));
            continue;
        }

        const incomplete = faceIncompleteReason(currentFace);
        if (incomplete) {
            misses++;
            pendingLogs.push('Id: ' + idnum + ' Rostro rechazado: ' + incomplete);
            await new Promise(resolve => setTimeout(resolve, 60));
            continue;
        }

        // Textura sobre el rostro y en el fotograma original (sin mejora de imagen)
        const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
        const texture = calculateFaceTexture(imgData, currentFace.detection && currentFace.detection.box);
        contador++;
        pendingLogs.push('Id: ' + idnum + ' Item: ' + contador + '. Textura (valor > referencia) Valor: ' + texture + ' Referencia: ' + faceText);

        if (texture > faceText) {
            // Solo las capturas con textura válida participan en la comparación
            captures.push({ face: currentFace });
            texturesDetected++;
            if (texturesDetected >= VERIFY_MIN_TEXTURE_OK) {
                pendingLogs.push('Id: ' + idnum + ' Capturas válidas: ' + texturesDetected + ' Fotogramas sin rostro: ' + misses + ' Tiempo(ms): ' + (Date.now() - startedAt));
                for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
                await idbSet('faceLogs', savedLogs);
                return 1;
            }
        }

        await new Promise(resolve => setTimeout(resolve, 40));
    }

    pendingLogs.push('Id: ' + idnum + ' Sin capturas suficientes. Válidas: ' + texturesDetected + ' Fotogramas sin rostro: ' + misses + ' Tiempo(ms): ' + (Date.now() - startedAt));
    for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
    await idbSet('faceLogs', savedLogs);

    return 0;
}

// Compara las capturas contra TODAS las plantillas del colaborador en una sola llamada
// (antes eran ~3 viajes SignalR por plantilla). Una plantilla "coincide" si alguna captura
// queda a menos de la distancia configurada. Retorna 1 si coinciden al menos
// min(MIN_TEMPLATE_MATCHES, total de plantillas).
async function compareFaceMulti(templates, idnumValue) {
    if (!templates || templates.length === 0) { return 0; }
    if (!captures || captures.length === 0) { return 0; }

    const maxDistance = Number((await idbGet('distancia')) || 0);
    const idnum = idnumValue || (await idbGet('idnum')) || 0;
    const required = Math.min(MIN_TEMPLATE_MATCHES, templates.length);
    const pendingLogs = [];
    let matches = 0;

    for (let t = 0; t < templates.length; t++) {
        const data = String(templates[t] || '').split(',');
        if (data.length < 128) continue;
        const ref = new Float32Array(data.length);
        for (let i = 0; i < data.length; i++) ref[i] = parseFloat(data[i]);

        let best = Infinity;
        for (const item of captures) {
            const d = faceapi.euclideanDistance(ref, item.face.descriptor);
            if (d < best) best = d;
        }
        const ok = best < maxDistance;
        if (ok) matches++;
        pendingLogs.push('Id: ' + idnum + ' Plantilla: ' + (t + 1) + '. Distancia (valor<referencia) Valor:' + best + ' Referencia:' + maxDistance + (ok ? ' OK' : ''));

        if (matches >= required) break;
    }

    pendingLogs.push('Id: ' + idnum + ' Coincidencias: ' + matches + ' Requeridas: ' + required);
    for (const msg of pendingLogs) savedLogs.push({ ts: new Date().toISOString(), msg });
    await idbSet('faceLogs', savedLogs);

    return matches >= required ? 1 : 0;
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
        // Tabla de 256 valores: evita Math.pow por cada canal de cada pixel
        const lut = new Uint8ClampedArray(256);
        for (let v = 0; v < 256; v++) {
            let n = Math.pow(v / 255, 1 / gamma) * 255;
            n = factor * (n - 128) + 128;
            lut[v] = n;
        }
        for (let i = 0; i < data.length; i += 4) {
            data[i] = lut[data[i]];
            data[i + 1] = lut[data[i + 1]];
            data[i + 2] = lut[data[i + 2]];
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

    // El video se muestra en alta resolucion, pero el analisis se mantiene en ~640px
    // (lado mayor) para conservar velocidad y descriptores equivalentes a los ya enrolados.
    const srcW = video.videoWidth || 640;
    const srcH = video.videoHeight || 480;
    const scale = Math.min(1, 640 / Math.max(srcW, srcH));
    _offscreenCanvas.width = Math.round(srcW * scale);
    _offscreenCanvas.height = Math.round(srcH * scale);
    if (!_offscreenContext) return _offscreenCanvas;

    _offscreenContext.drawImage(video, 0, 0, _offscreenCanvas.width, _offscreenCanvas.height);

    // measure luminance using sampled version (faster)
    const lum = meanLuminanceSampled(_offscreenContext, _offscreenCanvas.width, _offscreenCanvas.height);

    if (lum >= 120) return _offscreenCanvas;

    // La mejora de imagen se aplica sobre una copia para que _offscreenCanvas conserve
    // el fotograma original (la medicion de textura debe usar colores sin alterar).
    if (!_enhCanvas) {
        _enhCanvas = document.createElement('canvas');
        _enhContext = _enhCanvas.getContext('2d', { willReadFrequently: true });
    }
    _enhCanvas.width = _offscreenCanvas.width;
    _enhCanvas.height = _offscreenCanvas.height;
    if (!_enhContext) return _offscreenCanvas;
    _enhContext.drawImage(_offscreenCanvas, 0, 0);

    if (lum < 80) {
        enhanceImage(_enhContext, _enhCanvas.width, _enhCanvas.height, 1.1, 30);
    }
    else {
        enhanceImage(_enhContext, _enhCanvas.width, _enhCanvas.height, 1.15, 18);
    }
    return _enhCanvas;
}

// Textura (saturacion de color) medida solo en la zona central del rostro detectado,
// para que no dependa del color del fondo ni de la distancia a la camara.
function calculateFaceTexture(imageData, box) {
    const w = imageData.width, h = imageData.height;
    let x0 = 0, y0 = 0, x1 = w, y1 = h;
    if (box && box.width > 0 && box.height > 0) {
        x0 = Math.max(0, Math.floor(box.x + box.width * 0.2));
        y0 = Math.max(0, Math.floor(box.y + box.height * 0.2));
        x1 = Math.min(w, Math.ceil(box.x + box.width * 0.8));
        y1 = Math.min(h, Math.ceil(box.y + box.height * 0.8));
        if (x1 <= x0 || y1 <= y0) { x0 = 0; y0 = 0; x1 = w; y1 = h; }
    }
    const data = imageData.data;
    let total = 0, count = 0;
    for (let y = y0; y < y1; y += 2) {
        let i = (y * w + x0) * 4;
        for (let x = x0; x < x1; x += 2, i += 8) {
            const r = data[i], g = data[i + 1], b = data[i + 2];
            total += Math.abs(r - g) + Math.abs(g - b) + Math.abs(b - r);
            count++;
        }
    }
    return count > 0 ? total / count : 0;
}

// Valida que la deteccion corresponda a un rostro COMPLETO y razonablemente frontal.
// El detector tiny devuelve una caja aunque solo se vea media cara (score >= 0.35), y el
// descriptor resultante puede parecerse al de la plantilla. Retorna null si es valido o
// un texto con el motivo del rechazo.
function faceIncompleteReason(face, relaxSymmetry) {
    try {
        const det = face && face.detection;
        const lm = face && face.landmarks;
        if (!det || !lm) return 'sin landmarks';

        const dims = det.imageDims || { width: _offscreenCanvas.width, height: _offscreenCanvas.height };
        const W = dims.width, H = dims.height;
        const box = det.box;

        if (det.score < FACE_MIN_SCORE) return 'score bajo ' + det.score.toFixed(2);

        // Parte de la caja fuera del encuadre. El detector extiende la caja a frente y barbilla, asi que un
        // rostro grande y cercano la toca o la excede un poco; solo se rechaza si falta una porcion apreciable.
        const fueraX = (Math.max(0, -box.x) + Math.max(0, box.x + box.width - W)) / box.width;
        const fueraY = (Math.max(0, -box.y) + Math.max(0, box.y + box.height - H)) / box.height;
        if (fueraX > FACE_MAX_OUTSIDE || fueraY > FACE_MAX_OUTSIDE) {
            return 'rostro cortado por el borde del encuadre (fuera ' + Math.round(Math.max(fueraX, fueraY) * 100) + '%)';
        }

        // Proporcion ancho/alto: un rostro a medias produce cajas muy angostas o muy anchas
        const ratio = box.width / box.height;
        if (ratio < FACE_MIN_ASPECT || ratio > FACE_MAX_ASPECT) return 'proporcion anomala ' + ratio.toFixed(2);

        const pts = lm.positions;
        if (!pts || pts.length < 68) return 'landmarks incompletos';

        // Los landmarks deben caer dentro del fotograma (se toleran unos pocos puntos del contorno en el borde)
        let puntosFuera = 0;
        for (let i = 0; i < pts.length; i++) {
            if (pts[i].x < 0 || pts[i].y < 0 || pts[i].x > W || pts[i].y > H) puntosFuera++;
        }
        if (puntosFuera > 4) return 'landmarks fuera del encuadre (' + puntosFuera + ')';

        // El contorno de la mandibula (0..16) debe abarcar casi todo el ancho de la caja
        const jawWidth = Math.abs(pts[16].x - pts[0].x);
        if (jawWidth < box.width * FACE_MIN_JAW_RATIO) return 'mandibula incompleta';

        // La nariz debe quedar entre los lados de la cara (descarta perfil / media cara)
        const left = pts[30].x - Math.min(pts[0].x, pts[16].x);
        const right = Math.max(pts[0].x, pts[16].x) - pts[30].x;
        if (left <= 0 || right <= 0) return 'nariz fuera del contorno';
        const sym = Math.min(left, right) / Math.max(left, right);
        if (!relaxSymmetry && sym < FACE_MIN_SYMMETRY) return 'rostro de perfil/incompleto (simetria ' + sym.toFixed(2) + ')';

        return null;
    } catch (e) {
        console.warn('faceIncompleteReason', e);
        return null;
    }
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




