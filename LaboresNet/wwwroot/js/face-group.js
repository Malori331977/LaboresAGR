// Registro de marcas grupal: identificación 1:N, prueba de vida (parpadeo y giro),
// recuadro del rostro y foto de evidencia. Usa las globales de face-reco.js sin modificarlo.

// Versión visible en cada línea del log para confirmar qué archivo está ejecutando la app
const GRUPO_VERSION = '1.3.1';
console.log(`[MarcaGrupal v${GRUPO_VERSION}] face-group.js cargado`);

const GRUPO_CFG = {
    capturas: 3,
    coincidenciasRequeridas: 2,
    margen: 0.05,
    parpadeoRatio: 0.88,       // cierre del ojo (EAR/base). Con ~200 ms por cuadro el parpadeo se ve a medias (0.84-0.86); el ruido llega a ~0.92
    reaperturaRatio: 0.88,
    cadaDescriptor: 8,         // cuadro completo (detector+descriptor, ~350 ms) cada N cuadros; el resto son recortes ligeros
    recorteMargen: 1.4,
    recorteLado: 128,          // el modelo de 68 puntos trabaja a 112 px: un recorte mayor solo cuesta tiempo
    contrasteCierre: 0.7,      // contraste del ojo cerrado respecto al abierto
    contrasteReapertura: 0.85,
    retoTimeoutMs: 7000,
    giroUmbral: 0.12,
    toleranciaCambio: 1.2,
    fotoAncho: 320,
    fotoCalidad: 0.6,
    maxLogs: 300
};

let _grupo = [];
let _grupoColor = '#2192FF';

function loadGroupDescriptors(items) {
    _grupo = [];
    for (const item of items || []) {
        const descriptores = (item.descriptores || []).map(_parseDescriptor).filter(d => d !== null);
        if (descriptores.length > 0) _grupo.push({ idnumero: item.idnumero, descriptores });
    }
    _grupoCargarLandmarksCompleto(); // precarga para la primera prueba de vida
    return _grupo.length;
}

function clearGroupDescriptors() {
    _grupo = [];
    clearGroupBox();
}

function setGroupBoxColor(color) {
    _grupoColor = color;
}

function clearGroupBox() {
    const o = _grupoOverlay();
    if (o) o.ctx.clearRect(0, 0, o.canvas.width, o.canvas.height);
}

async function identifyFace() {
    const maxDist = Number((await idbGet('distancia')) || 0);
    const minTextura = Number((await idbGet('textura')) || 0);
    if (!video || _grupo.length === 0 || maxDist <= 0) return { idnumero: null, distancia: null, sinRostro: true };

    const votos = {};
    const mejores = {};
    const log = [];
    let rostros = 0;

    for (let i = 0; i < GRUPO_CFG.capturas; i++) {
        const canvas = getEnhancedCanvasFromVideo(video);
        let det = await safeDetectSingleFace(canvas, tinyOptions);
        _grupoDibujar(det);
        const n = `Captura ${i + 1}/${GRUPO_CFG.capturas}`;
        // Rostro cortado por el encuadre o a medias: no se identifica (ver faceIncompleteReason en face-reco)
        const incompleto = det && typeof faceIncompleteReason === 'function' ? faceIncompleteReason(det) : null;
        if (incompleto) {
            log.push(`${n}: rostro incompleto (${incompleto}), descartada`);
            det = null;
        } else if (!det) {
            log.push(`${n}: sin rostro`);
        }
        if (det) {
            rostros++;
            const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
            const textura = calculateFaceTexture(imgData, det.detection && det.detection.box);
            if (textura <= minTextura) {
                log.push(`${n}: textura ${_grupoNum(textura)} <= ref ${_grupoNum(minTextura)} (descartada)`);
            } else {
                const ranking = _grupoRanking(det.descriptor);
                const primero = ranking[0];
                const segundo = ranking[1];
                const margen = segundo ? segundo.distancia - primero.distancia : null;
                const vota = primero && primero.distancia < maxDist &&
                    (!segundo || margen >= GRUPO_CFG.margen);
                if (vota) {
                    votos[primero.idnumero] = (votos[primero.idnumero] || 0) + 1;
                    mejores[primero.idnumero] = Math.min(mejores[primero.idnumero] ?? 99, primero.distancia);
                }
                log.push(`${n}: textura ${_grupoNum(textura)} (ref ${_grupoNum(minTextura)})` +
                    ` · 1º ${primero?.idnumero} d=${_grupoNum(primero?.distancia)} (FACEDIST ${_grupoNum(maxDist)})` +
                    (segundo ? ` · 2º ${segundo.idnumero} d=${_grupoNum(segundo.distancia)} · margen ${_grupoNum(margen)} (ref ${_grupoNum(GRUPO_CFG.margen)})` : ' · sin 2º candidato') +
                    ` → ${vota ? 'voto' : 'sin voto'}`);
            }
        }
        await _grupoEsperar(80);
    }

    // Si nadie está frente a la cámara no se registra nada (evita llenar el log)
    if (rostros === 0) return { idnumero: null, distancia: null, sinRostro: true };
    const ganador = Object.keys(votos).find(k => votos[k] >= GRUPO_CFG.coincidenciasRequeridas);
    log.push(ganador
        ? `Identificado ${ganador}: ${votos[ganador]}/${GRUPO_CFG.capturas} votos (requeridos ${GRUPO_CFG.coincidenciasRequeridas}), mejor d=${_grupoNum(mejores[ganador])}`
        : `No reconocido. Votos: ${JSON.stringify(votos)} (requeridos ${GRUPO_CFG.coincidenciasRequeridas})`);
    await _grupoLog(log);
    return ganador
        ? { idnumero: ganador, distancia: mejores[ganador], sinRostro: false }
        : { idnumero: null, distancia: null, sinRostro: false };
}

async function checkLiveness(idnumero, reto) {
    const persona = _grupo.find(g => g.idnumero === idnumero);
    const maxDist = Number((await idbGet('distancia')) || 0);
    if (!persona || maxDist <= 0) {
        await _grupoLog([`Prueba de vida ${idnumero}: sin plantilla o FACEDIST no configurado (${_grupoNum(maxDist)})`]);
        return { ok: false, motivo: 'sin_plantilla', foto: null };
    }

    const inicio = Date.now();
    let earBase = null;
    let earMin = null;
    let cerrado = false;
    let parpadeo = false;
    // Segundo detector de parpadeo por contraste de la zona del ojo: con el ojo abierto conviven
    // esclerótica clara e iris oscuro; con el párpado cerrado solo queda piel, y el contraste cae.
    // Los landmarks de face-api casi no cierran el párpado, pero sí ubican bien la caja del ojo.
    const contrastes = [];
    let contBase = null;
    let contMin = null;
    let cerradoC = false;
    let parpadeoC = false;
    let parpadeoEar = false;
    let giro = reto === 'parpadeo';
    let giroMax = null;
    const rangoDet = { min: Infinity, max: -Infinity };
    const rangoRec = { min: Infinity, max: -Infinity };
    let coincideAlguna = false;
    let ultimoCanvas = null;
    let cuadros = 0;
    let distMax = 0;
    let distFinal = null;
    const trazaE = [];   // serie de EAR por cuadro ligero (diagnostico del parpadeo)
    const trazaC = [];   // serie de contraste del ojo por cuadro ligero

    // Landmarks completos (no tiny) para parpadeo y giro: el modelo tiny casi no mueve los párpados
    const completo = await _grupoCargarLandmarksCompleto();
    let msLigero = 0;
    let nLigero = 0;
    let msDescriptor = 0;
    let nDescriptor = 0;

    const terminar = async (ok, motivo, foto) => {
        await _grupoLog([`Prueba de vida ${idnumero} reto=${reto}: ` +
            `EAR base ${_grupoNum(earBase)} mín ${_grupoNum(earMin)} (mín/base ${_grupoNum(earBase && earMin !== null ? earMin / earBase : null)}, cierre < ${_grupoNum(earBase === null ? null : earBase * GRUPO_CFG.parpadeoRatio)}) parpadeo EAR=${parpadeoEar ? 'sí' : 'no'}` +
            ` · contraste base ${_grupoNum(contBase)} mín ${_grupoNum(contMin)} (mín/base ${_grupoNum(contBase && contMin !== null ? contMin / contBase : null)}, cierre < ${_grupoNum(contBase === null ? null : contBase * GRUPO_CFG.contrasteCierre)}) parpadeo contraste=${parpadeoC ? 'sí' : 'no'}` +
            (reto === 'parpadeo' ? '' : ` · giro máx ${_grupoNum(giroMax)} (ref ${_grupoNum(GRUPO_CFG.giroUmbral)}) giro=${giro ? 'sí' : 'no'}` +
                ` (yaw crudo detección ${_grupoNum(rangoDet.min)}..${_grupoNum(rangoDet.max)}, recorte ${_grupoNum(rangoRec.min)}..${_grupoNum(rangoRec.max)})`) +
            ` · d final ${_grupoNum(distFinal)} / d máx ${_grupoNum(distMax)} (FACEDIST ${_grupoNum(maxDist)}, tolerancia ${_grupoNum(maxDist * GRUPO_CFG.toleranciaCambio)})` +
            ` · serie EAR [${trazaE.slice(-40).join(',')}] · serie contraste [${trazaC.slice(-40).join(',')}]` +
            ` · ${cuadros} cuadros · ${Date.now() - inicio} ms` +
            ` · landmarks ${completo ? 'completo' : 'tiny'} · backend ${_grupoBackend()}` +
            ` · ms/cuadro ligero ${nLigero ? Math.round(msLigero / nLigero) : '-'} descriptor ${nDescriptor ? Math.round(msDescriptor / nDescriptor) : '-'}` +
            ` → ${ok ? 'OK' : motivo}`]);
        return { ok, motivo, foto };
    };

    const ears = [];
    let intentos = 0;
    let caja = null;
    let canvas = null;
    while (Date.now() - inicio < GRUPO_CFG.retoTimeoutMs) {
        const t0 = performance.now();
        // Cuadro completo (detector + landmarks + descriptor) cada N cuadros o si se perdió el rostro.
        // Entre ellos solo se corren los landmarks sobre un recorte del rostro: sin detector ni mejora
        // de imagen, mucho más rápido, para muestrear el parpadeo con más frecuencia.
        // Con parpadeo y giro ya logrados se fuerza un cuadro completo para confirmar identidad y terminar sin esperar
        const conDescriptor = !caja || intentos % GRUPO_CFG.cadaDescriptor === 0 || (parpadeo && giro);
        intentos++;
        let lm;
        if (conDescriptor) {
            canvas = getEnhancedCanvasFromVideo(video);
            const det = await safeDetectSingleFace(canvas, tinyOptions);
            msDescriptor += performance.now() - t0; nDescriptor++;
            _grupoDibujar(det);
            if (!det) { caja = null; await _grupoEsperar(15); continue; }
            caja = det.detection.box;
            lm = det.landmarks;

            // Anti-cambio de persona: los cuadros con descriptor deben parecerse al empleado identificado
            const dist = _grupoDistancia(persona, det.descriptor);
            distFinal = dist;
            distMax = Math.max(distMax, dist);
            if (dist >= maxDist * GRUPO_CFG.toleranciaCambio) return terminar(false, 'cambio_persona', null);
            if (dist < maxDist) coincideAlguna = true;
        } else {
            lm = await _grupoLandmarksEnRecorte(caja, completo);
            if (lm) {
                const c = _grupoContrasteOjos(lm);
                if (c !== null) {
                    trazaC.push(Math.round(c));
                    if (!cerradoC) {
                        contrastes.push(c);
                        contBase = _grupoPercentil(contrastes, 0.75);
                    }
                    contMin = contMin === null ? c : Math.min(contMin, c);
                    if (contrastes.length >= 3) {
                        if (!cerradoC && c < contBase * GRUPO_CFG.contrasteCierre) cerradoC = true;
                        else if (cerradoC && c > contBase * GRUPO_CFG.contrasteReapertura) parpadeoC = true;
                    }
                }
            }
            msLigero += performance.now() - t0; nLigero++;
            if (!lm) { caja = null; continue; }
        }
        cuadros++;

        // Con landmarks completos, los cuadros de descriptor (landmarks tiny) no se mezclan en la medición
        const medir = !completo || !conDescriptor;
        if (medir) {
            const ear = (_grupoEar(lm.getLeftEye()) + _grupoEar(lm.getRightEye())) / 2;
            trazaE.push(Math.round(ear * 1000) / 1000);
            // Referencia robusta: percentil 75 (un solo cuadro con ojos muy abiertos no la infla)
            // Mientras el ojo está cerrado la referencia se congela: el giro posterior al parpadeo
            // agranda el EAR y subiría el umbral de reapertura
            if (!cerrado) {
                ears.push(ear);
                earBase = _grupoPercentil(ears, 0.75);
            }
            earMin = earMin === null ? ear : Math.min(earMin, ear);
            if (!cerrado && ear < earBase * GRUPO_CFG.parpadeoRatio) cerrado = true;
            else if (cerrado && ear > earBase * GRUPO_CFG.reaperturaRatio) parpadeoEar = true;
        }

        // El giro se mide en todos los cuadros (detección completa y recorte) y se registra el rango
        // crudo de cada fuente para diagnosticar si una de ellas no refleja el movimiento
        const yaw = _grupoYaw(lm);
        const r = conDescriptor ? rangoDet : rangoRec;
        r.min = Math.min(r.min, yaw);
        r.max = Math.max(r.max, yaw);
        if (!giro) {
            // Se acepta el giro hacia cualquier lado: según la cámara (algunas webcams entregan la imagen
            // espejada) el mismo giro sale positivo o negativo, y la persona no siempre gira al lado pedido
            const valor = Math.abs(yaw);
            giroMax = giroMax === null ? valor : Math.max(giroMax, valor);
            if (valor > GRUPO_CFG.giroUmbral) giro = true;
        }

        parpadeo = parpadeoEar || parpadeoC;
        if (parpadeo && giro && conDescriptor && distFinal < maxDist) {
            ultimoCanvas = canvas;
            break;
        }
        // Los recortes no esperan: cuanto más seguido se muestrea, menos parpadeos se pierden
        if (conDescriptor) await _grupoEsperar(15);
    }

    if (!parpadeo) return terminar(false, 'sin_parpadeo', null);
    if (!giro) return terminar(false, 'sin_giro', null);
    if (!coincideAlguna || !ultimoCanvas) return terminar(false, 'cambio_persona', null);
    return terminar(true, null, _grupoFoto(ultimoCanvas));
}

// Mensaje desde C# (resultado del servidor) al mismo log de la sesión
async function logGrupo(mensaje) {
    await _grupoLog([mensaje]);
}

// Escribe en la consola y en faceLogs (IndexedDB), el mismo almacén que muestra FaceLogsPop.
// Usa el arreglo global savedLogs de face-reco.js, que startMedia limpia al iniciar la sesión.
async function _grupoLog(mensajes) {
    const ts = new Date().toISOString();
    for (const msg of mensajes) {
        console.log(`[MarcaGrupal v${GRUPO_VERSION}] ` + msg);
        savedLogs.push({ ts, msg: `v${GRUPO_VERSION} ${msg}` });
    }
    if (savedLogs.length > GRUPO_CFG.maxLogs) savedLogs.splice(0, savedLogs.length - GRUPO_CFG.maxLogs);
    try { await idbSet('faceLogs', savedLogs); } catch (e) { console.warn('No se pudo guardar faceLogs', e); }
}

function _grupoNum(valor) {
    return typeof valor === 'number' && isFinite(valor) ? valor.toFixed(3) : '-';
}

function _parseDescriptor(texto) {
    const partes = (texto || '').split(',');
    if (partes.length !== 128) return null;
    const arr = new Float32Array(128);
    for (let i = 0; i < 128; i++) arr[i] = parseFloat(partes[i]);
    return arr;
}

function _grupoDistancia(persona, descriptor) {
    let min = 99;
    for (const d of persona.descriptores) min = Math.min(min, faceapi.euclideanDistance(d, descriptor));
    return min;
}

function _grupoRanking(descriptor) {
    return _grupo
        .map(g => ({ idnumero: g.idnumero, distancia: _grupoDistancia(g, descriptor) }))
        .sort((a, b) => a.distancia - b.distancia);
}

// Landmarks sobre un recorte cuadrado alrededor de la última caja detectada (coordenadas del video).
// EAR y giro son proporciones, así que no importa que los puntos queden en coordenadas del recorte.
let _grupoRecorte = null;
async function _grupoLandmarksEnRecorte(caja, completo) {
    const vw = video.videoWidth, vh = video.videoHeight;
    if (!vw || !vh) return null;
    // La caja viene en coordenadas del canvas de analisis, que puede estar reducido respecto al video
    // (camara a mayor resolucion): se escala a coordenadas del video antes de recortar.
    const k = _offscreenCanvas && _offscreenCanvas.width ? vw / _offscreenCanvas.width : 1;
    const cx = caja.x * k, cy = caja.y * k, cw = caja.width * k, ch = caja.height * k;
    const lado = Math.min(Math.max(cw, ch) * GRUPO_CFG.recorteMargen, vw, vh);
    const x = Math.min(Math.max(0, cx + cw / 2 - lado / 2), vw - lado);
    const y = Math.min(Math.max(0, cy + ch / 2 - lado / 2), vh - lado);
    if (!_grupoRecorte) _grupoRecorte = document.createElement('canvas');
    _grupoRecorte.width = GRUPO_CFG.recorteLado;
    _grupoRecorte.height = GRUPO_CFG.recorteLado;
    _grupoRecorteCtx().drawImage(video, x, y, lado, lado, 0, 0, GRUPO_CFG.recorteLado, GRUPO_CFG.recorteLado);
    try {
        return completo ? await faceapi.detectFaceLandmarks(_grupoRecorte) : await faceapi.detectFaceLandmarksTiny(_grupoRecorte);
    } catch (e) {
        console.warn('Landmarks en recorte fallaron', e);
        return null;
    }
}

function _grupoRecorteCtx() {
    if (!_grupoRecorte._ctx) _grupoRecorte._ctx = _grupoRecorte.getContext('2d', { willReadFrequently: true });
    return _grupoRecorte._ctx;
}

// Desviación estándar de la luminancia en la caja de cada ojo (sobre el último recorte), promedio de ambos
function _grupoContrasteOjos(lm) {
    const ctx = _grupoRecorte && _grupoRecorte._ctx;
    if (!ctx) return null;
    const ojo = puntos => {
        const xs = puntos.map(p => p.x), ys = puntos.map(p => p.y);
        const x0 = Math.max(0, Math.floor(Math.min(...xs)));
        const x1 = Math.min(_grupoRecorte.width, Math.ceil(Math.max(...xs)));
        const ancho = x1 - x0;
        const cy = (Math.min(...ys) + Math.max(...ys)) / 2;
        const alto = Math.max(Math.max(...ys) - Math.min(...ys), ancho * 0.45);
        const y0 = Math.max(0, Math.round(cy - alto / 2));
        const y1 = Math.min(_grupoRecorte.height, Math.round(cy + alto / 2));
        if (ancho < 4 || y1 - y0 < 3) return null;
        const d = ctx.getImageData(x0, y0, ancho, y1 - y0).data;
        let suma = 0, suma2 = 0, n = 0;
        for (let i = 0; i < d.length; i += 4) {
            const l = 0.299 * d[i] + 0.587 * d[i + 1] + 0.114 * d[i + 2];
            suma += l; suma2 += l * l; n++;
        }
        const media = suma / n;
        return Math.sqrt(Math.max(0, suma2 / n - media * media));
    };
    const a = ojo(lm.getLeftEye()), b = ojo(lm.getRightEye());
    if (a === null || b === null) return null;
    return (a + b) / 2;
}

// Carga perezosa del modelo de 68 puntos completo (ya incluido en /faceapi-models).
// Se ejecuta una vez en vacío para compilar los shaders de WebGL antes de la primera prueba de vida.
let _grupoLandmarksPromesa = null;
function _grupoCargarLandmarksCompleto() {
    if (faceapi.nets.faceLandmark68Net.isLoaded && !_grupoLandmarksPromesa) return Promise.resolve(true);
    if (!_grupoLandmarksPromesa) {
        _grupoLandmarksPromesa = faceapi.nets.faceLandmark68Net.loadFromUri('/faceapi-models')
            .then(async () => {
                const c = document.createElement('canvas');
                c.width = GRUPO_CFG.recorteLado;
                c.height = GRUPO_CFG.recorteLado;
                await faceapi.detectFaceLandmarks(c);
                return true;
            })
            .catch(e => { console.warn('No se pudo cargar faceLandmark68Net', e); _grupoLandmarksPromesa = null; return false; });
    }
    return _grupoLandmarksPromesa;
}

function _grupoBackend() {
    try { return faceapi.tf.getBackend(); } catch (e) { return '?'; }
}

function _grupoPercentil(valores, p) {
    const orden = [...valores].sort((a, b) => a - b);
    return orden[Math.min(orden.length - 1, Math.floor(orden.length * p))];
}

function _grupoPuntoDist(a, b) {
    return Math.hypot(a.x - b.x, a.y - b.y);
}

// Relación de apertura del ojo (6 puntos): (|p1-p5| + |p2-p4|) / (2 |p0-p3|)
function _grupoEar(ojo) {
    const ancho = _grupoPuntoDist(ojo[0], ojo[3]) || 1;
    return (_grupoPuntoDist(ojo[1], ojo[5]) + _grupoPuntoDist(ojo[2], ojo[4])) / (2 * ancho);
}

// Giro horizontal aproximado: desplazamiento de la punta de la nariz respecto al centro de los ojos
function _grupoYaw(lm) {
    const nariz = lm.getNose()[3];
    const izq = lm.getLeftEye()[0];
    const der = lm.getRightEye()[3];
    const centro = (izq.x + der.x) / 2;
    const distanciaOjos = Math.abs(der.x - izq.x) || 1;
    return (nariz.x - centro) / distanciaOjos;
}

function _grupoOverlay() {
    const canvas = document.getElementById('overlayGrupo');
    const vid = document.getElementById('video');
    if (!canvas || !vid) return null;
    const w = vid.clientWidth;
    const h = vid.clientHeight;
    if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; }
    return { canvas, ctx: canvas.getContext('2d') };
}

function _grupoDibujar(det) {
    const o = _grupoOverlay();
    if (!o) return;
    o.ctx.clearRect(0, 0, o.canvas.width, o.canvas.height);
    if (!det || !_offscreenCanvas) return;
    const sx = o.canvas.width / _offscreenCanvas.width;
    const sy = o.canvas.height / _offscreenCanvas.height;
    const b = det.detection.box;
    o.ctx.lineWidth = 3;
    o.ctx.strokeStyle = _grupoColor;
    o.ctx.strokeRect(b.x * sx, b.y * sy, b.width * sx, b.height * sy);
}

function _grupoFoto(canvas) {
    const escala = Math.min(1, GRUPO_CFG.fotoAncho / canvas.width);
    const tmp = document.createElement('canvas');
    tmp.width = Math.round(canvas.width * escala);
    tmp.height = Math.round(canvas.height * escala);
    tmp.getContext('2d').drawImage(canvas, 0, 0, tmp.width, tmp.height);
    return tmp.toDataURL('image/jpeg', GRUPO_CFG.fotoCalidad).split(',')[1];
}

function _grupoEsperar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
