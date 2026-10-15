// Enrolamiento guiado: una sola sesión de cámara, captura automática por pose, rechazo de fotos
// repetidas, verificación de misma persona y aviso de rostros parecidos a otros empleados.
// Usa las globales de face-reco.js (video, safeDetectSingleFace, tinyOptions, getEnhancedCanvasFromVideo,
// calculateFaceTexture, _offscreenContext, _offscreenCanvas, idbGet) sin modificarlo.

const ENROLL_VERSION = '1.0.0';

const ENROLL_CFG = {
    cuadrosSostener: 3,        // cuadros válidos seguidos antes de capturar (~0,5 s)
    distanciaMinFotos: 0.08,   // por debajo, la foto no aporta variedad
    frontalMax: 0.08,          // |giro| máximo para considerar el rostro de frente
    giroMin: 0.12,             // |giro| mínimo para las poses de lado
    giroMax: 0.40,
    inclinacionMin: 0.06,      // cambio mínimo de inclinación respecto a la foto base
    lejosRatio: 0.85,          // ancho del rostro respecto a la foto base
    cercaRatio: 1.15,
    pausaMs: 60
};

// Poses en el mismo orden que la lista de la página (EnrolarEmpleado.razor)
const ENROLL_POSES = ['frente', 'frente', 'lado', 'ladoOpuesto', 'inclinar', 'inclinarOpuesto', 'lejos', 'cerca', 'frente', 'frente'];

let _enr = null;

// config: { total, poseInicial, existentes: [string], otros: [{ idnumero, descriptores: [string] }] }
async function enrollStart(dotnet, config) {
    enrollStop(false);
    const parse = _enrollParse;
    _enr = {
        dotnet,
        total: config.total,
        poseInicial: config.poseInicial || 0,
        existentes: (config.existentes || []).map(parse).filter(d => d),
        otros: (config.otros || []).map(o => ({ idnumero: o.idnumero, descriptores: (o.descriptores || []).map(parse).filter(d => d) }))
            .filter(o => o.descriptores.length > 0),
        capturas: [],          // { descriptor, texto, yaw, pitch, ancho }
        base: null,            // medidas de la primera foto de la sesión
        signoLado: 0,
        signoInclinar: 0,
        sostener: 0,
        manual: false,
        activo: true,
        parecidos: {},         // idnumero -> mejor distancia
        ultimoEstado: ''
    };
    _enr.maxDist = Number((await idbGet('distancia')) || 0);
    _enr.minTextura = Number((await idbGet('textura')) || 0);
    if (_enr.maxDist <= 0) return { ok: false, motivo: 'Falta configurar la distancia de reconocimiento (FACEDIST).' };

    const camara = await startCamera();
    if (camara !== 1) return { ok: false, motivo: 'No se pudo iniciar la cámara.' };
    _enrollCiclo();
    return { ok: true, motivo: null };
}

function enrollStop(apagar = true) {
    if (_enr) _enr.activo = false;
    if (apagar && typeof stopMedia === 'function') stopMedia();
}

function enrollManual() {
    if (_enr) _enr.manual = true;
}

// Descriptores capturados, como texto separado por comas (mismo formato que registerFace)
function enrollDescriptores() {
    return _enr ? _enr.capturas.map(c => c.texto) : [];
}

// Quita las capturas y vuelve a empezar desde la pose inicial
function enrollReiniciar() {
    if (!_enr) return;
    _enr.capturas = [];
    _enr.base = null;
    _enr.signoLado = 0;
    _enr.signoInclinar = 0;
    _enr.sostener = 0;
    _enr.parecidos = {};
}

async function _enrollCiclo() {
    const s = _enr;
    while (s.activo && s === _enr) {
        if (s.capturas.length >= s.total) {
            await _enrollEstado('completo', 'Plantillas completas', 0);
            await _enrollEsperar(250);
            continue;
        }
        try {
            await _enrollCuadro(s);
        } catch (e) {
            console.warn('[Enrolar] error en el cuadro', e);
        }
        await _enrollEsperar(ENROLL_CFG.pausaMs);
    }
}

async function _enrollCuadro(s) {
    const canvas = getEnhancedCanvasFromVideo(video);
    const det = await safeDetectSingleFace(canvas, tinyOptions);
    const indice = s.capturas.length;
    const pose = ENROLL_POSES[(s.poseInicial + indice) % ENROLL_POSES.length];

    if (!det) {
        s.sostener = 0;
        return _enrollEstado('buscando', 'Ubique el rostro dentro del óvalo', 0);
    }

    // Rostro completo (no cortado, ni a medias). La simetria se relaja: las poses de lado
    // ya se controlan con el giro mas abajo.
    const incompleto = typeof faceIncompleteReason === 'function' ? faceIncompleteReason(det, true) : null;
    if (incompleto) {
        // Un fotograma flojo (el score oscila) no reinicia el avance: solo lo resta
        s.sostener = Math.max(0, s.sostener - 1);
        console.log('[Enrolar] rostro rechazado: ' + incompleto);
        return _enrollEstado('aviso', 'Muestre el rostro completo dentro del óvalo (' + incompleto + ')', 0);
    }

    const imgData = _offscreenContext.getImageData(0, 0, _offscreenCanvas.width, _offscreenCanvas.height);
    const textura = calculateFaceTexture(imgData, det.detection.box);
    if (textura <= s.minTextura) {
        s.sostener = 0;
        return _enrollEstado('aviso', 'Acérquese a la cámara o mejore la luz', 0);
    }

    // Misma persona: contra la primera foto de la sesión, o contra las fotos ya guardadas
    const referencia = s.capturas.length > 0 ? s.capturas.map(c => c.descriptor) : s.existentes;
    const distRef = referencia.length > 0 ? _enrollMinDist(referencia, det.descriptor) : 0;
    if (referencia.length > 0 && distRef >= s.maxDist) {
        s.sostener = 0;
        const detalle = ' (distancia ' + distRef.toFixed(2) + ', máx ' + s.maxDist.toFixed(2) + ')';
        console.log('[Enrolar] no coincide:' + detalle);
        return _enrollEstado('error', (s.capturas.length > 0
            ? 'El rostro no coincide con las plantillas anteriores de esta sesión'
            : 'El rostro no coincide con las plantillas ya registradas del colaborador') + detalle, 0);
    }

    const medidas = _enrollMedidas(det);
    const poseOk = s.manual || _enrollPoseOk(s, pose, medidas);
    if (!poseOk) {
        s.sostener = 0;
        return _enrollEstado('pose', _enrollPista(s, pose, medidas), 0);
    }

    // Variedad: la foto debe diferir de las ya tomadas y de las guardadas
    const previas = s.capturas.map(c => c.descriptor).concat(s.existentes);
    if (previas.length > 0 && _enrollMinDist(previas, det.descriptor) < ENROLL_CFG.distanciaMinFotos) {
        s.sostener = 0;
        s.manual = false;
        return _enrollEstado('aviso', 'Muy parecida a una plantilla anterior. Cambie un poco la posición', 0);
    }

    s.sostener = s.manual ? ENROLL_CFG.cuadrosSostener : s.sostener + 1;
    if (s.sostener < ENROLL_CFG.cuadrosSostener) {
        return _enrollEstado('sosteniendo', 'Mantenga la posición…', s.sostener / ENROLL_CFG.cuadrosSostener);
    }

    // Captura
    s.sostener = 0;
    s.manual = false;
    if (!s.base) s.base = medidas;
    if (pose === 'lado') s.signoLado = Math.sign(medidas.yaw);
    if (pose === 'inclinar' && s.base) s.signoInclinar = Math.sign(medidas.pitch - s.base.pitch);
    s.capturas.push({ descriptor: det.descriptor, texto: Array.from(det.descriptor).toString(), ...medidas });
    _enrollParecidos(s, det.descriptor);

    const parecidos = Object.entries(s.parecidos)
        .sort((a, b) => a[1] - b[1])
        .map(([idnumero, distancia]) => ({ idnumero, distancia: Math.round(distancia * 1000) / 1000 }));
    console.log(`[Enrolar v${ENROLL_VERSION}] Plantilla ${s.capturas.length}/${s.total} pose=${pose} giro=${medidas.yaw.toFixed(3)} incl=${medidas.pitch.toFixed(3)} ancho=${Math.round(medidas.ancho)} textura=${textura.toFixed(1)}`);
    s.ultimoEstado = '';
    await s.dotnet.invokeMethodAsync('OnCaptura', s.capturas.length, parecidos);
}

function _enrollMedidas(det) {
    const lm = det.landmarks;
    const izq = lm.getLeftEye()[0];
    const der = lm.getRightEye()[3];
    const nariz = lm.getNose()[3];
    const boca = lm.getMouth();
    const ojosX = (izq.x + der.x) / 2;
    const ojosY = (izq.y + der.y) / 2;
    const bocaY = (boca[3].y + boca[9].y) / 2;
    const anchoOjos = Math.abs(der.x - izq.x) || 1;
    return {
        yaw: (nariz.x - ojosX) / anchoOjos,
        // Posición vertical de la punta de la nariz entre ojos y boca: cambia al subir o bajar la barbilla
        pitch: (nariz.y - ojosY) / ((bocaY - ojosY) || 1),
        ancho: det.detection.box.width
    };
}

function _enrollPoseOk(s, pose, m) {
    const ayaw = Math.abs(m.yaw);
    const frente = ayaw < ENROLL_CFG.frontalMax;
    switch (pose) {
        case 'frente':
            return frente;
        case 'lado':
            return ayaw >= ENROLL_CFG.giroMin && ayaw <= ENROLL_CFG.giroMax;
        case 'ladoOpuesto':
            return ayaw >= ENROLL_CFG.giroMin && ayaw <= ENROLL_CFG.giroMax &&
                (s.signoLado === 0 || Math.sign(m.yaw) === -s.signoLado);
        case 'inclinar':
            return ayaw < ENROLL_CFG.giroMin && (!s.base || Math.abs(m.pitch - s.base.pitch) >= ENROLL_CFG.inclinacionMin);
        case 'inclinarOpuesto':
            if (!s.base) return ayaw < ENROLL_CFG.giroMin;
            return ayaw < ENROLL_CFG.giroMin && Math.abs(m.pitch - s.base.pitch) >= ENROLL_CFG.inclinacionMin &&
                (s.signoInclinar === 0 || Math.sign(m.pitch - s.base.pitch) === -s.signoInclinar);
        case 'lejos':
            return frente && (!s.base || m.ancho <= s.base.ancho * ENROLL_CFG.lejosRatio);
        case 'cerca':
            return frente && (!s.base || m.ancho >= s.base.ancho * ENROLL_CFG.cercaRatio);
        default:
            return true;
    }
}

function _enrollPista(s, pose, m) {
    switch (pose) {
        case 'frente': return 'Mire de frente a la cámara';
        case 'lado':
        case 'ladoOpuesto':
            if (Math.abs(m.yaw) > ENROLL_CFG.giroMax) return 'Gire un poco menos';
            if (pose === 'ladoOpuesto' && Math.abs(m.yaw) >= ENROLL_CFG.giroMin) return 'Gire hacia el otro lado';
            return 'Gire un poco más la cabeza';
        case 'inclinar':
        case 'inclinarOpuesto':
            if (Math.abs(m.yaw) >= ENROLL_CFG.giroMin) return 'Mantenga la cabeza de frente';
            return pose === 'inclinarOpuesto' && s.base && Math.abs(m.pitch - s.base.pitch) >= ENROLL_CFG.inclinacionMin
                ? 'Incline la cabeza hacia el otro lado'
                : 'Incline un poco más la cabeza';
        case 'lejos': return Math.abs(m.yaw) >= ENROLL_CFG.frontalMax ? 'Mire de frente a la cámara' : 'Aléjese un poco de la cámara';
        case 'cerca': return Math.abs(m.yaw) >= ENROLL_CFG.frontalMax ? 'Mire de frente a la cámara' : 'Acérquese un poco a la cámara';
        default: return '';
    }
}

// Guarda, por cada otro empleado, la menor distancia a las fotos capturadas que quede bajo FACEDIST
function _enrollParecidos(s, descriptor) {
    for (const o of s.otros) {
        const d = _enrollMinDist(o.descriptores, descriptor);
        if (d < s.maxDist && (s.parecidos[o.idnumero] === undefined || d < s.parecidos[o.idnumero]))
            s.parecidos[o.idnumero] = d;
    }
}

function _enrollParse(texto) {
    const partes = (texto || '').split(',');
    if (partes.length !== 128) return null;
    const arr = new Float32Array(128);
    for (let i = 0; i < 128; i++) arr[i] = parseFloat(partes[i]);
    return arr;
}

function _enrollMinDist(lista, descriptor) {
    let min = 99;
    for (const d of lista) min = Math.min(min, faceapi.euclideanDistance(d, descriptor));
    return min;
}

// Notifica a C# solo cuando cambia el estado o el avance de "sostener", para no saturar el render
async function _enrollEstado(tipo, mensaje, progreso) {
    const s = _enr;
    if (!s) return;
    const clave = `${tipo}|${mensaje}|${progreso}`;
    if (clave === s.ultimoEstado) return;
    s.ultimoEstado = clave;
    try { await s.dotnet.invokeMethodAsync('OnEstado', tipo, mensaje, progreso); }
    catch (e) { s.activo = false; }
}

function _enrollEsperar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
