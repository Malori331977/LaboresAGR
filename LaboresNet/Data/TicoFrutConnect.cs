using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Request;
using com.gsitcr.geotime.Models.Response;
using LaboresNet.Data.Interfaces;
using Microsoft.Extensions.Logging;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using PortalEmpleadoGeotime.Client.Modelo;
using PortalEmpleadoGeotime.Client.Modelo.Utils;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TicoFrutLibs.Models;

namespace LaboresNet.Data
{
    public class TicoFrutConnect : ITicoFrutConnect
    {
        private readonly ILogger<TicoFrutConnect> _logger;
        private UserRequest? userData;

        private string? _cachedToken;
        private DateTime _tokenExpiraEn = DateTime.MinValue;
        private static readonly TimeSpan MargenSeguridadToken = TimeSpan.FromSeconds(30);

        public event Action OnChange;
        public TicoFrutConnect(ILogger<TicoFrutConnect> logger)
        {
            _logger = logger;
            userData = new UserRequest
            {
                BDName = "",
                Schema = "",
                ClientId = "",
                Password = "",
                User = "",
            };
        }
        public void setSchema(cPh_Compania compania)
        {
            userData = new UserRequest
            {
                BDName = compania.APIDATABASE,
                Schema = compania.IDCOMP,
                ClientId = compania.APICLIENTID,
                Password = compania.APIPASSWORD,
                User = compania.APIUSER,
            };

            // El token cacheado quedó emitido para el esquema/compañía anterior; ya no sirve.
            _cachedToken = null;
            _tokenExpiraEn = DateTime.MinValue;

            NotifyStateChanged();

        }
        private void NotifyStateChanged()
        {

            OnChange?.Invoke();
        }


        

        /// <summary>
        /// GetUrlApi: Método para obtener los datos de configuracion que se encuentran en el appsettings.json
        /// </summary>
        /// <returns>Instancia de la clase AppSettings</returns>
        private async Task<cParametroApiLocal> GetUrlApi()
        {
            cParametroApiLocal? appSettings = null;

            try
            {
                if ((await ConfigFile.ExistsConfigFile()))
                {
                    appSettings = await ConfigFile.GetConfigFile();
                }
            }
            catch (Exception e)
            {
                string error = e.InnerException is null ? e.Message : e.InnerException.Message;
                _logger.LogError($"GeoTimeConnect.GetUrlApi: Se ha presentado un error al ejecutar el proceso. Detalle de Error: {error}");
            }


            return appSettings!;

        }

        /// <summary>
        /// Agrega un User-Agent explícito: HttpClient no envía ninguno por defecto,
        /// y Cloudflare (delante de la API de GeoTime) usa su ausencia como señal de bot
        /// y responde con un challenge (403) en vez de dejar pasar la petición.
        /// </summary>
        private static void ConfigurarUserAgent(HttpClient client)
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LaboresNet-MAUI/1.0 (Android)");
        }

        /// <summary>
        /// Post: Método para crear o actualizar registros en objetos de la base de datos a traves del método Post
        /// </summary>
        /// <returns>Objeto de la clase EventResponse </returns>
        /// <param name="apiName">Nombre del método de la Api a Ejecutar</param>
        /// <param name="model">Objeto de la clase a enviar en el cuerpo del mensaje</param>
        public async Task<EventResponse> Post(string apiName, object model)
        {
            EventResponse eventoAdd = new();

            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();

                // Use HttpClientHandler to configure low-level settings if needed
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                };
                // Create HttpClient with optional timeout from settings (default 100s)
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
                ConfigurarUserAgent(client);

                apiName = appSettings.ApiGeoTimeUrl + "/" + apiName;
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await GetTicoFrutToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var postData = JsonSerializer.Serialize(model);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                // Use cancellation token with the same timeout as HttpClient to be explicit
                using var cts = new CancellationTokenSource(client.Timeout);

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(apiName, contentData, cts.Token);

                }
                catch (TaskCanceledException tex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        _logger.LogError($"Post: Request to '{apiName}' timed out after {client.Timeout.TotalSeconds} seconds.");
                    }
                    else
                    {
                        _logger.LogError($"Post: Request to '{apiName}' was cancelled. {tex.Message}");
                    }
                    return new EventResponse
                    {
                        Respuesta = "ERROR",
                        Descripcion = "Request timed out or was cancelled.",
                        Id = "1",
                        ValorRetorno = ""
                    };
                }

                // Verify response and handle errors
                if (response.IsSuccessStatusCode)
                {
                    eventoAdd = await response.Content.ReadFromJsonAsync<EventResponse>() ?? new EventResponse();
                }
                else
                {
                    EventResponse respBadRequest = new();
                    try
                    {
                        respBadRequest = await response.Content.ReadFromJsonAsync<EventResponse>() ?? new EventResponse();

                    }
                    catch
                    {

                    }
                    eventoAdd.Id = "1";
                    eventoAdd.Respuesta = "ERROR";
                    eventoAdd.Descripcion = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

                    if (!String.IsNullOrEmpty(respBadRequest.Descripcion))
                    {
                        eventoAdd.Descripcion = eventoAdd.Descripcion + ": " + respBadRequest.Descripcion;
                    }
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.InnerException is null ? e.Message : e.InnerException.Message;
                _logger.LogError(errorMessage);

                eventoAdd = new EventResponse
                {
                    Respuesta = "ERROR",
                    Descripcion = $"{errorMessage}",
                    Id = "1",
                };
            }
            return eventoAdd;
        }

        /// <summary>
        /// Put: Método para actualizar registros en objetos de la base de datos a traves del método Put
        /// </summary>
        /// <returns>Objeto de la clase EventResponse </returns>
        /// <param name="apiName">Nombre del método de la Api a Ejecutar</param>
        /// <param name="model">Objeto de la clase a enviar en el cuerpo del mensaje</param>
        public async Task<EventResponse> Put(string apiName, object model)
        {
            EventResponse? eventoAdd = new();

            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                // Use HttpClientHandler to configure low-level settings if needed
                // Use HttpClientHandler to configure low-level settings if needed
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                };

                // Create HttpClient with optional timeout from settings (default 100s)
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
                ConfigurarUserAgent(client);

                apiName = appSettings.ApiGeoTimeUrl + "/" + apiName;
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await GetTicoFrutToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var putData = JsonSerializer.Serialize(model);
                var contentData = new StringContent(putData, Encoding.UTF8, "application/json");

                // Use cancellation token with the same timeout as HttpClient to be explicit
                using var cts = new CancellationTokenSource(client.Timeout);

                HttpResponseMessage response;
                try
                {
                    response = await client.PutAsync(apiName, contentData, cts.Token);
                }
                catch (TaskCanceledException tex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        _logger.LogError($"Put: Request to '{apiName}' timed out after {client.Timeout.TotalSeconds} seconds.");
                    }
                    else
                    {
                        _logger.LogError($"Put: Request to '{apiName}' was cancelled. {tex.Message}");
                    }

                    return new EventResponse
                    {
                        Respuesta = "ERROR",
                        Descripcion = "Request timed out or was cancelled.",
                        Id = "1",
                        ValorRetorno = ""
                    };
                }

                // Verify response and handle errors
                if (response.IsSuccessStatusCode)
                {
                    eventoAdd = await response.Content.ReadFromJsonAsync<EventResponse>() ?? new EventResponse();
                }
                else
                {
                    eventoAdd.Id = "1";
                    eventoAdd.Respuesta = "ERROR";
                    eventoAdd.Descripcion = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.InnerException is null ? e.Message : e.InnerException.Message;
                _logger.LogError(errorMessage);

                eventoAdd = new EventResponse
                {
                    Respuesta = "ERROR",
                    Descripcion = $"{errorMessage}",
                    Id = "1",
                };

            }
            return eventoAdd!;
        }

        /// <summary>
        /// Delete:  Metodo generico para borrado de datos de las tablas
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="id"></param>
        /// <returns>EventResponse</returns>
        public async Task<EventResponse> Delete(string apiName, string id)
        {
            EventResponse respuesta = new();
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();

                // Use HttpClientHandler to configure low-level settings if needed
                var handler = new HttpClientHandler();

                // Create HttpClient with optional timeout from settings (default 100s)
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
                ConfigurarUserAgent(client);

                apiName = $"{appSettings.ApiGeoTimeUrl}/{apiName}/{id}";
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await GetTicoFrutToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                // Use cancellation token with the same timeout as HttpClient to be explicit
                using var cts = new CancellationTokenSource(client.Timeout);

                HttpResponseMessage response;
                try
                {
                    response = await client.DeleteAsync(apiName, cts.Token);
                }
                catch (TaskCanceledException tex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        _logger.LogError($"Delete: Request to '{apiName}' timed out after {client.Timeout.TotalSeconds} seconds.");
                    }
                    else
                    {
                        _logger.LogError($"Delete: Request to '{apiName}' was cancelled. {tex.Message}");
                    }

                    return new EventResponse
                    {
                        Respuesta = "ERROR",
                        Descripcion = "Request timed out or was cancelled.",
                        Id = "1",
                        ValorRetorno = ""
                    };
                }

                // Verify response and handle errors
                if (response.IsSuccessStatusCode)
                {
                    respuesta = await response.Content.ReadFromJsonAsync<EventResponse>() ?? new EventResponse();
                }
                else
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Delete: Error calling '{apiName}'. Status: {response.StatusCode}. Content: {contentString}");

                    try
                    {
                        respuesta = JsonSerializer.Deserialize<EventResponse>(contentString) ?? new EventResponse();
                        // If the API indicates an error, normalize the Respuesta/Descripcion
                        if (respuesta.Respuesta is null || respuesta.Respuesta.ToUpper() != "OK")
                        {
                            respuesta.Id = "1";
                            respuesta.Respuesta = respuesta.Respuesta ?? "ERROR";
                            respuesta.Descripcion = respuesta.Descripcion ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Delete: Failed to deserialize error response from '{apiName}'. Exception: {ex.Message}");
                        respuesta = new EventResponse
                        {
                            Respuesta = "ERROR",
                            Descripcion = $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {contentString}",
                            Id = "0",
                            ValorRetorno = "-1"
                        };
                    }
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.InnerException is null ? e.Message : e.InnerException.Message;
                _logger.LogError(errorMessage);

                respuesta = new EventResponse
                {
                    Respuesta = "ERROR",
                    Descripcion = $"{errorMessage}",
                    Id = "1",
                };

            }

            return respuesta;

        }

        /// <summary>
        /// Delete:  Metodo generico para borrado de datos de TipoMarca
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="id"></param>
        /// <returns>EventResponse</returns>
        public async Task<EventResponse> Delete(string apiName, string id, string nivel)
        {
            EventResponse respuesta = new();
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                // Use HttpClientHandler to configure low-level settings if needed
                var handler = new HttpClientHandler();

                // Create HttpClient with optional timeout from settings (default 100s)
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
                ConfigurarUserAgent(client);

                apiName = $"{appSettings.ApiGeoTimeUrl}/{apiName}/{id}/{nivel}";
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await GetTicoFrutToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                // Use cancellation token with the same timeout as HttpClient to be explicit
                using var cts = new CancellationTokenSource(client.Timeout);

                HttpResponseMessage response;
                try
                {
                    response = await client.DeleteAsync(apiName, cts.Token);
                }
                catch (TaskCanceledException tex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        _logger.LogError($"Delete: Request to '{apiName}' timed out after {client.Timeout.TotalSeconds} seconds.");
                    }
                    else
                    {
                        _logger.LogError($"Delete: Request to '{apiName}' was cancelled. {tex.Message}");
                    }

                    return new EventResponse
                    {
                        Respuesta = "ERROR",
                        Descripcion = "Request timed out or was cancelled.",
                        Id = "1",
                        ValorRetorno = ""
                    };
                }

                // Verify response and handle errors
                if (response.IsSuccessStatusCode)
                {
                    respuesta = await response.Content.ReadFromJsonAsync<EventResponse>() ?? new EventResponse();
                }
                else
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Delete: Error calling '{apiName}'. Status: {response.StatusCode}. Content: {contentString}");

                    try
                    {
                        respuesta = JsonSerializer.Deserialize<EventResponse>(contentString) ?? new EventResponse();
                        // If the API indicates an error, normalize the Respuesta/Descripcion
                        if (respuesta.Respuesta is null || respuesta.Respuesta.ToUpper() != "OK")
                        {
                            respuesta.Id = "1";
                            respuesta.Respuesta = respuesta.Respuesta ?? "ERROR";
                            respuesta.Descripcion = respuesta.Descripcion ?? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Delete: Failed to deserialize error response from '{apiName}'. Exception: {ex.Message}");
                        respuesta = new EventResponse
                        {
                            Respuesta = "ERROR",
                            Descripcion = $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {contentString}",
                            Id = "0",
                            ValorRetorno = "-1"
                        };
                    }
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.InnerException is null ? e.Message : e.InnerException.Message;
                _logger.LogError(errorMessage);

                respuesta = new EventResponse
                {
                    Respuesta = "ERROR",
                    Descripcion = $"{errorMessage}",
                    Id = "1",
                };

            }

            return respuesta;

        }

        /// <summary>
        /// GetTicoFrutToken: Obtiene el token JWT del API de TicoFrut
        /// </summary>
        private async Task<string> GetTicoFrutToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiraEn)
                return _cachedToken;

            string token = "";
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();

                var handler = new HttpClientHandler();
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
                ConfigurarUserAgent(client);

                var api = $"{appSettings.ApiAuxiliarUrl}/api/Auth/login";
                client.BaseAddress = new Uri(appSettings.ApiAuxiliarUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var loginRequest = new LoginRequest
                {
                    Username = appSettings.ApiUser ?? "admin",
                    Password = appSettings.ApiPassword ?? "admin"
                };

                var postData = JsonSerializer.Serialize(loginRequest);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(client.Timeout);
                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(api, contentData, cts.Token);
                }
                catch (TaskCanceledException tex)
                {
                    _logger.LogError($"GetTicoFrutToken: Request timed out or cancelled. {tex.Message}");
                    throw;
                }

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (loginResponse is not null)
                    {
                        token = loginResponse.Token ?? "";

                        var expiracion = Convert.ToDateTime(loginResponse.Expiration);
                        _cachedToken = token;
                        _tokenExpiraEn = expiracion - MargenSeguridadToken;
                    }
                }
                else
                {
                    _logger.LogError($"GetTicoFrutToken: Error {response.StatusCode}. {response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetTicoFrutToken: {e.Message}");
            }
            return token;
        }


        private async Task<bool> GetConnection()
        {
           
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();

                var handler = new HttpClientHandler
                {
                    // Se detectó un bucle de redirección 301 del servidor (resuelto del lado de
                    // TicofrutApi, ver Program.cs). Se deja en false a propósito como defensa:
                    // si el bug vuelve a aparecer, falla rápido en vez de perseguir 50 redirects.
                    AllowAutoRedirect = false
                };
                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                ConfigurarUserAgent(client);

                var api = $"{appSettings.ApiAuxiliarUrl}/api/Auth/login";
                client.BaseAddress = new Uri(appSettings.ApiAuxiliarUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var loginRequest = new LoginRequest
                {
                    Username = appSettings.ApiUser ?? "admin",
                    Password = appSettings.ApiPassword ?? "admin"
                };

                var postData = JsonSerializer.Serialize(loginRequest);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(client.Timeout);
                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(api, contentData, cts.Token);

                    if ((int)response.StatusCode is >= 300 and < 400)
                    {
                        // DIAGNÓSTICO (deshabilitado, dejar comentado): esto fue lo que permitió
                        // encontrar el bucle de redirección 301 causado por Cloudflare mandando
                        // el esquema en "CF-Visitor" en vez de "X-Forwarded-Proto" (resuelto en
                        // TicofrutApi/Program.cs). Reactivar si "TicoFrut desconectado" reaparece.
                        // var location = response.Headers.Location?.ToString() ?? "(sin Location)";
                        // var todosHeaders = string.Join(" | ", response.Headers
                        //     .Concat(response.Content.Headers)
                        //     .Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                        // _logger.LogWarning($"[LaboresNet] TicoFrut GetConnection REDIRECT: {api} => {(int)response.StatusCode}, Location={location}, Headers: {todosHeaders}");
                        // Console.WriteLine($"[LaboresNet] TicoFrut GetConnection REDIRECT: {api} => {(int)response.StatusCode}, Location={location}");
                        return false;
                    }
                }
                catch (TaskCanceledException)
                {
                    return false;
                }

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"TicoFrut GetConnection: {e.Message}");
            }
            return false;
        }

        public async Task<bool> ApiDisponible()
        {
            try
            {
                return await GetConnection();

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get: Método HTTP GET genérico contra el API de TicoFrut (ApiAuxiliarUrl)
        /// </summary>
        /// <param name="method">Ruta relativa del endpoint, ej: /api/GeoAgrFinca</param>
        public async Task<HttpResponseMessage> Get(string method)
        {
            cParametroApiLocal appSettings = await GetUrlApi();

            var handler = new HttpClientHandler();
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(300)
            };
            ConfigurarUserAgent(client);

            client.BaseAddress = new Uri(appSettings.ApiAuxiliarUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var token = await GetTicoFrutToken();
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var api = $"{appSettings.ApiAuxiliarUrl}{method}";

            using var cts = new CancellationTokenSource(client.Timeout);
            try
            {
                var response = await client.GetAsync(api, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Get: Error calling '{api}'. Status: {(int)response.StatusCode} {response.ReasonPhrase}. Content: {content}");
                }

                return response;
            }
            catch (TaskCanceledException tex)
            {
                if (cts.IsCancellationRequested)
                {
                    _logger.LogError($"Get: Request to '{api}' timed out after {client.Timeout.TotalSeconds} seconds.");
                    return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                    {
                        Content = new StringContent($"Request timed out after {client.Timeout.TotalSeconds} seconds.")
                    };
                }
                else
                {
                    _logger.LogError($"Get: Request to '{api}' was cancelled. {tex.Message}");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent($"Request was cancelled: {tex.Message}")
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get: Exception calling '{api}': {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Internal error calling API: {ex.Message}")
                };
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrFinca
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrFinca: Obtiene la lista completa de fincas
        /// </summary>
        public async Task<IEnumerable<GeoAgrFinca>> GetGeoAgrFinca()
        {
            IEnumerable<GeoAgrFinca> result = Enumerable.Empty<GeoAgrFinca>();
            try
            {
                var response = await Get("/api/GeoAgrFinca");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrFinca>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrFinca: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrFinca: Obtiene la lista de fincas por compañía y país
        /// </summary>
        public async Task<IEnumerable<GeoAgrFinca>> GetGeoAgrFinca(string compania, string pais)
        {
            IEnumerable<GeoAgrFinca> result = Enumerable.Empty<GeoAgrFinca>();
            try
            {
                var response = await Get($"/api/GeoAgrFinca/{compania}/{pais}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrFinca>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrFinca({compania},{pais}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrFinca: Obtiene una finca específica
        /// </summary>
        public async Task<GeoAgrFinca?> GetGeoAgrFinca(string compania, string pais, string bloque, string finca)
        {
            GeoAgrFinca? result = null;
            try
            {
                var response = await Get($"/api/GeoAgrFinca/{compania}/{pais}/{bloque}/{finca}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrFinca>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrFinca({compania},{pais},{bloque},{finca}): {e.Message}");
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrLote
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrLote: Obtiene la lista completa de lotes
        /// </summary>
        public async Task<IEnumerable<GeoAgrLote>> GetGeoAgrLote()
        {
            IEnumerable<GeoAgrLote> result = Enumerable.Empty<GeoAgrLote>();
            try
            {
                var response = await Get("/api/GeoAgrLote");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrLote>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLote: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrLote: Obtiene la lista de lotes por compañía, bloque y finca
        /// </summary>
        public async Task<IEnumerable<GeoAgrLote>> GetGeoAgrLote(string compania, string bloque, string finca)
        {
            IEnumerable<GeoAgrLote> result = Enumerable.Empty<GeoAgrLote>();
            try
            {
                var response = await Get($"/api/GeoAgrLote/{compania}/{bloque}/{finca}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrLote>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLote({compania},{bloque},{finca}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrLote: Obtiene un lote específico
        /// </summary>
        public async Task<GeoAgrLote?> GetGeoAgrLote(string compania, string bloque, string finca, string lote)
        {
            GeoAgrLote? result = null;
            try
            {
                var response = await Get($"/api/GeoAgrLote/{compania}/{bloque}/{finca}/{lote}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrLote>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLote({compania},{bloque},{finca},{lote}): {e.Message}");
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrOt
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrOt: Obtiene la lista completa de Órdenes de Trabajo
        /// </summary>
        public async Task<IEnumerable<GeoAgrOt>> GetGeoAgrOt()
        {
            IEnumerable<GeoAgrOt> result = Enumerable.Empty<GeoAgrOt>();
            try
            {
                var response = await Get("/api/GeoAgrOt");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrOt>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOt: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrOt: Obtiene la lista de OT por compañía, bloque y finca
        /// </summary>
        public async Task<IEnumerable<GeoAgrOt>> GetGeoAgrOt(string compania, string bloque, string finca)
        {
            IEnumerable<GeoAgrOt> result = Enumerable.Empty<GeoAgrOt>();
            try
            {
                var response = await Get($"/api/GeoAgrOt/{compania}/{bloque}/{finca}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrOt>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOt({compania},{bloque},{finca}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrOt: Obtiene una OT específica
        /// </summary>
        public async Task<GeoAgrOt?> GetGeoAgrOt(string compania, string bloque, string finca, string ot)
        {
            GeoAgrOt? result = null;
            try
            {
                var response = await Get($"/api/GeoAgrOt/{compania}/{bloque}/{finca}/{ot}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrOt>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOt({compania},{bloque},{finca},{ot}): {e.Message}");
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrOtLabor
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista completa de labores de OT
        /// </summary>
        public async Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor()
        {
            IEnumerable<GeoAgrOtLabor> result = Enumerable.Empty<GeoAgrOtLabor>();
            try
            {
                var response = await Get("/api/GeoAgrOtLabor");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrOtLabor>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOtLabor: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista de labores por compañía, bloque y finca
        /// </summary>
        public async Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor(string compania, string bloque, string finca)
        {
            IEnumerable<GeoAgrOtLabor> result = Enumerable.Empty<GeoAgrOtLabor>();
            try
            {
                var response = await Get($"/api/GeoAgrOtLabor/{compania}/{bloque}/{finca}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrOtLabor>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOtLabor({compania},{bloque},{finca}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista de labores por compañía, bloque, finca y OT
        /// </summary>
        public async Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor(string compania, string bloque, string finca, string ot)
        {
            IEnumerable<GeoAgrOtLabor> result = Enumerable.Empty<GeoAgrOtLabor>();
            try
            {
                var response = await Get($"/api/GeoAgrOtLabor/{compania}/{bloque}/{finca}/{ot}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrOtLabor>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOtLabor({compania},{bloque},{finca},{ot}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene una labor de OT específica
        /// </summary>
        public async Task<GeoAgrOtLabor?> GetGeoAgrOtLabor(string compania, string bloque, string finca, string ot, string labor)
        {
            GeoAgrOtLabor? result = null;
            try
            {
                var response = await Get($"/api/GeoAgrOtLabor/{compania}/{bloque}/{finca}/{ot}/{labor}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrOtLabor>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrOtLabor({compania},{bloque},{finca},{ot},{labor}): {e.Message}");
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrRecursoConcepto
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrRecursoConcepto: Obtiene la lista completa de recursos/conceptos
        /// </summary>
        public async Task<IEnumerable<GeoAgrRecursoConcepto>> GetGeoAgrRecursoConcepto()
        {
            IEnumerable<GeoAgrRecursoConcepto> result = Enumerable.Empty<GeoAgrRecursoConcepto>();
            try
            {
                var response = await Get("/api/GeoAgrRecursoConcepto");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrRecursoConcepto>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrRecursoConcepto: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrRecursoConcepto: Obtiene un recurso/concepto específico
        /// </summary>
        public async Task<GeoAgrRecursoConcepto?> GetGeoAgrRecursoConcepto(string compania, string recurso)
        {
            GeoAgrRecursoConcepto? result = null;
            try
            {
                var response = await Get($"/api/GeoAgrRecursoConcepto/{compania}/{recurso}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrRecursoConcepto>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrRecursoConcepto({compania},{recurso}): {e.Message}");
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // GeoAgrLaborAuxiliar
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene la lista completa de labores auxiliares
        /// </summary>
        public async Task<IEnumerable<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliar()
        {
            IEnumerable<GeoAgrLaborAuxiliar> result = Enumerable.Empty<GeoAgrLaborAuxiliar>();
            try
            {
                var response = await Get("/api/GeoAgrLaborAuxiliar");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrLaborAuxiliar>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLaborAuxiliar: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene la lista de labores auxiliares por compañía y empleado
        /// </summary>
        public async Task<IEnumerable<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliar(string compania, string idEmpleado)
        {
            IEnumerable<GeoAgrLaborAuxiliar> result = Enumerable.Empty<GeoAgrLaborAuxiliar>();
            try
            {
                var response = await Get($"/api/GeoAgrLaborAuxiliar/{compania}/{idEmpleado}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<IList<GeoAgrLaborAuxiliar>>();
                    readJob.Wait();
                    result = readJob.Result ?? result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLaborAuxiliar({compania},{idEmpleado}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene el encabezado de labor auxiliar para un empleado y fecha específicos
        /// </summary>
        public async Task<GeoAgrLaborAuxiliar?> GetGeoAgrLaborAuxiliar(string compania, string idEmpleado, DateTime fecha)
        {
            GeoAgrLaborAuxiliar? result = null;
            try
            {
                var fechaStr = fecha.ToString("yyyy-MM-dd");
                var response = await Get($"/api/GeoAgrLaborAuxiliar/{compania}/{idEmpleado}/{fechaStr}");
                if (response.IsSuccessStatusCode)
                {
                    var readJob = response.Content.ReadFromJsonAsync<GeoAgrLaborAuxiliar>();
                    readJob.Wait();
                    result = readJob.Result;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"GetGeoAgrLaborAuxiliar({compania},{idEmpleado},{fecha:yyyy-MM-dd}): {e.Message}");
            }
            return result;
        }
    }
}
