async function getCoords(options = { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }, retries = 2) {
    // small helper to wait
    const delay = ms => new Promise(res => setTimeout(res, ms));

    function posPromise(opts) {
        return new Promise((resolve, reject) => {
            if (!('geolocation' in navigator)) {
                reject(new Error('Geolocation API not available'));
                return;
            }

            var timer = null;

            const success = (pos) => {
                if (timer) {
                    clearTimeout(timer);
                    timer = null;
                }
                resolve(pos);
            };

            const failure = (err) => {
                if (timer) {
                    clearTimeout(timer);
                    timer = null;
                }
                reject(err);
            };

            try {
                navigator.geolocation.getCurrentPosition(success, failure, opts);
            } catch (e) {
                // defensive: some environments may throw synchronously
                failure(e);
                return;
            }

            // Fallback timeout in case the browser ignores options.timeout
            var effectiveTimeout = (opts && typeof opts.timeout === 'number' && opts.timeout > 0) ? opts.timeout + 2000 : 12000;
            timer = setTimeout(function () {
                timer = null;
                // create an error similar to GeolocationPositionError when possible
                var toErr = new Error('Geolocation timeout');
                toErr.code = 3; // POSITION_UNAVAILABLE/timeout style
                failure(toErr);
            }, effectiveTimeout);
        });
    }

    let attempt = 0;
    let lastError = null;

    while (attempt <= retries) {
        try {
            const pos = await posPromise(options);
            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;
            // return in existing format expected by Blazor code
            return `${lat}|${lng}`;
        } catch (err) {
            lastError = err;
            // If user denied permission, no point in retrying
            if (err && (err.code === 1 || (err.message && err.message.toLowerCase().indexOf('permission') !== -1))) {
                throw new Error('Permission denied for geolocation');
            }

            // prepare for retry: relax options (less accuracy, longer timeout)
            attempt++;
            if (attempt > retries) break;

            // increase timeout and disable high accuracy for next attempt
            try {
                options = Object.assign({}, options, { enableHighAccuracy: false, timeout: Math.max((options.timeout || 10000) * 1.5, 15000) });
            } catch (e) { }

            // small backoff before retrying
            await delay(400 * attempt);
        }
    }

    // If reached here, all attempts failed
    throw lastError || new Error('Unknown geolocation error');
}


// Inicializa la API de Google Maps cargando dinámicamente el script.
// Recibe el `apiKey` obligatorio y devuelve una Promise que resuelve con `window.google.maps`.
// Inicializa la API de Google Maps de forma asíncrona.
// Devuelve una Promise (la función es `async`) que resuelve con `window.google.maps`.
// Si se llama múltiples veces mientras la carga está en progreso, todas las llamadas
// esperan la misma Promise (singleton) para evitar cargar el script varias veces.
window.initGoogleMaps = async function (apiKey) {
    if (!apiKey) {
        throw new Error('Google Maps API key is required');
    }

    // Si ya está cargado, resolver inmediatamente.
    if (window.google && window.google.maps) {
        return window.google.maps;
    }

    // Si ya hay una carga en progreso, devolverla.
    if (window._initGoogleMapsPromise) {
        return await window._initGoogleMapsPromise;
    }

    window._initGoogleMapsPromise = new Promise(function (resolve, reject) {
        var callbackName = '__initGoogleMapsCallback';

        // Definir el callback global una sola vez; si ya existe, lo sobrescribimos
        // porque la promise controla la resolución.
        window[callbackName] = function () {
            resolve(window.google && window.google.maps ? window.google.maps : null);
            try { delete window[callbackName]; } catch (e) { window[callbackName] = undefined; }
        };

// Interop para manipular mapas nativos creados con la API de Google
window.mapInterop = (function () {
    var maps = {};
    var markers = {};

    function createOrUpdateMap(containerId, lat, lng, markersData, zoom) {
        if (!window.google || !window.google.maps) {
            console.warn('Google Maps API no está inicializada');
            return;
        }

        var center = { lat: parseFloat(lat), lng: parseFloat(lng) };

        var container = document.getElementById(containerId);
        if (!container) {
            console.warn('Container element not found for id:', containerId);
            return;
        }

        // If no map stored, or stored map's div is missing or different (page re-rendered), recreate it.
        var needCreate = false;
        if (!maps[containerId]) {
            needCreate = true;
        } else {
            try {
                var existingDiv = maps[containerId].getDiv ? maps[containerId].getDiv() : null;
                if (!existingDiv || existingDiv !== container || existingDiv.parentNode == null) {
                    // previous map's div is gone (Blazor re-rendered). recreate map.
                    try { maps[containerId] = null; } catch (e) { }
                    needCreate = true;
                }
            } catch (e) {
                needCreate = true;
            }
        }

        if (needCreate) {
            var map = new window.google.maps.Map(container, {
                center: center,
                zoom: zoom || 8,
            });
            maps[containerId] = map;
            markers[containerId] = [];
        } else {
            maps[containerId].setCenter(center);
            if (zoom) maps[containerId].setZoom(zoom);
        }

        // limpiar marcadores previos
        if (markers[containerId] && markers[containerId].length > 0) {
            markers[containerId].forEach(function (m) { m.setMap(null); });
            markers[containerId] = [];
        }

        if (Array.isArray(markersData)) {
            markersData.forEach(function (m) {
                try {
                    var pos = { lat: parseFloat(m.lat), lng: parseFloat(m.lng) };
                    // Prefer AdvancedMarkerElement if available (recommended by Google)
                    if (window.google.maps.marker && window.google.maps.marker.AdvancedMarkerElement) {
                        // create simple content element for the advanced marker
                        var content = document.createElement('div');
                        content.className = 'advanced-marker';
                        // Optionally style or add inner content here
                        content.style.display = 'inline-block';
                        content.title = m.title || '';

                        var advMarker = new window.google.maps.marker.AdvancedMarkerElement({
                            position: pos,
                            map: maps[containerId],
                            title: m.title || '',
                            content: content
                        });
                        markers[containerId].push(advMarker);
                    } else {
                        var gMarker = new window.google.maps.Marker({ position: pos, map: maps[containerId], title: m.title || '' });
                        markers[containerId].push(gMarker);
                    }
                } catch (e) {
                    console.warn('Error creating marker', e);
                }
            });
        }
    }

    function setCenter(containerId, lat, lng) {
        if (maps[containerId]) maps[containerId].setCenter({ lat: parseFloat(lat), lng: parseFloat(lng) });
    }

    function clearMarkers(containerId) {
        if (markers[containerId]) {
            markers[containerId].forEach(function (m) { m.setMap(null); });
            markers[containerId] = [];
        }
    }

    return {
        createOrUpdateMap: createOrUpdateMap,
        setCenter: setCenter,
        clearMarkers: clearMarkers,
    };
})();

        var script = document.createElement('script');
        // include version and loading=async per Google recommended loading pattern to avoid warning
        script.src = 'https://maps.googleapis.com/maps/api/js?key=' + encodeURIComponent(apiKey) + '&callback=' + callbackName + '&v=weekly&loading=async';
        script.async = true;
        script.defer = true;
        script.onerror = function () {
            // Permitir reintentos limpiando la promise global en caso de error.
            window._initGoogleMapsPromise = null;
            reject(new Error('Failed to load Google Maps script'));
            try { delete window[callbackName]; } catch (e) { window[callbackName] = undefined; }
        };

        document.head.appendChild(script);
    });

    return await window._initGoogleMapsPromise;
};

