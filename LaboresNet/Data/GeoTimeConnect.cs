
using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Request;
using com.gsitcr.geotime.Models.Response;
using com.gsitcr.geotime.Models.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Modelo;
using PortalEmpleadoGeotime.Client.Modelo.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;



namespace PortalEmpleadoGeotime.Client.Data
{
    internal class GeoTimeConnect : IGeoTimeConnect
    {

        private readonly NavigationManager navigationManager;
        private readonly ILogger<GeoTimeConnect> _logger;
        private UserRequest? userData;

        private string? _cachedToken;
        private DateTime _tokenExpiraEnUtc = DateTime.MinValue;
        private const int MargenSeguridadTokenSegundos = 30;

        public event Action OnChange;
        public GeoTimeConnect(NavigationManager NavigationManager, ILogger<GeoTimeConnect> logger)
        {
            navigationManager = NavigationManager;
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
            _tokenExpiraEnUtc = DateTime.MinValue;

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
        /// Cachea el token en memoria hasta su vencimiento. La API no expone "expires_in":
        /// el vencimiento va implícito en el claim "exp" del propio JWT, así que hay que decodificarlo.
        /// </summary>
        private void CachearToken(string? token)
        {
            _cachedToken = null;
            _tokenExpiraEnUtc = DateTime.MinValue;

            if (string.IsNullOrEmpty(token))
                return;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return;

                var jwt = handler.ReadJwtToken(token);
                if (jwt.ValidTo == DateTime.MinValue)
                    return; // el token no trae claim "exp"; no cacheamos para no arriesgar usar uno vencido

                _cachedToken = token;
                _tokenExpiraEnUtc = jwt.ValidTo.AddSeconds(-MargenSeguridadTokenSegundos);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"CachearToken: no se pudo decodificar el JWT para determinar su vencimiento. {e.Message}");
            }
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

        public async Task<bool> getConnection()
        {

            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                using var client = new HttpClient();
                ConfigurarUserAgent(client);
                var api = appSettings.ApiGeoTimeUrl + "/User";
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                client.Timeout = new TimeSpan(0, 0, 5);
                var userRequest = new UserRequest
                {
                    ClientId = String.IsNullOrEmpty(userData!.ClientId) ? appSettings.ApiClientId : userData!.ClientId,
                    User = String.IsNullOrEmpty(userData!.User) ? appSettings.ApiUser : userData!.User,
                    Password = String.IsNullOrEmpty(userData!.Password) ? appSettings.ApiPassword : userData!.Password,
                    Schema = String.IsNullOrEmpty(userData!.Schema) ? appSettings.ApiGeotimeSchema : userData!.Schema,
                    BDName = String.IsNullOrEmpty(userData!.BDName) ? appSettings.ApiGeotimeDataBase : userData!.BDName
                };

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var postData = JsonSerializer.Serialize(userRequest);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(api, contentData);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // DIAGNÓSTICO (deshabilitado, dejar comentado): esto fue lo que permitió
                // detectar el 403 de Cloudflare (Cf-Mitigated: challenge) que causaba el
                // badge "Modo sin conexión" aunque hubiera red. Reactivar si vuelve a
                // aparecer el badge sin explicación.
                // var cfMitigated = response.Headers.TryGetValues("Cf-Mitigated", out var v) ? string.Join(",", v) : "-";
                // Console.WriteLine($"[LaboresNet] getConnection FALLO: {api} => {(int)response.StatusCode} {response.ReasonPhrase}, Cf-Mitigated={cfMitigated}");
                // _logger.LogWarning($"getConnection FALLO: {api} => {(int)response.StatusCode}, Cf-Mitigated={cfMitigated}");
            }
            catch (Exception e)
            {
                // DIAGNÓSTICO (deshabilitado, dejar comentado): detalle de la excepción real
                // (timeout, TLS, DNS, etc.) en vez de tragarla en silencio.
                // var detalle = e.InnerException is null ? e.Message : $"{e.Message} | {e.InnerException.Message}";
                // Console.WriteLine($"[LaboresNet] getConnection EXCEPCION {e.GetType().Name}: {detalle}");
                // _logger.LogWarning($"getConnection EXCEPCION {e.GetType().Name}: {detalle}");
                Console.WriteLine(e.Message);
            }
            return false;
        }


        /// <summary>
        /// getToken: Método para obtener token de acceso a la Api
        /// </summary>
        /// <returns>String con el token solicitado</returns>

        public async Task<string> getToken()
        {
            if (!String.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiraEnUtc)
                return _cachedToken;

            string? token = "";
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                using var client = new HttpClient();
                ConfigurarUserAgent(client);
                var api = appSettings.ApiGeoTimeUrl + "/User";
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var userRequest = new UserRequest
                {
                    ClientId = String.IsNullOrEmpty(userData!.ClientId) ? appSettings.ApiClientId : userData!.ClientId,
                    User = String.IsNullOrEmpty(userData!.User) ? appSettings.ApiUser : userData!.User,
                    Password = String.IsNullOrEmpty(userData!.Password) ? appSettings.ApiPassword : userData!.Password,
                    Schema = String.IsNullOrEmpty(userData!.Schema) ? appSettings.ApiGeotimeSchema : userData!.Schema,
                    BDName = String.IsNullOrEmpty(userData!.BDName) ? appSettings.ApiGeotimeDataBase : userData!.BDName
                };

                _logger.LogInformation($"GeoTimeConnect.getToken.  BD=>{userRequest.BDName}, Schema=>{userRequest.Schema}");

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var postData = JsonSerializer.Serialize(userRequest);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(api, contentData);

                if (response.IsSuccessStatusCode)
                {
                    var respuesta = await response.Content.ReadFromJsonAsync<UserResponse>();

                    if (respuesta is not null)
                    {
                        token = respuesta.Token;
                        CachearToken(token);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return token == null ? "" : token;
        }


        /// <summary>
        /// Post: Método para crear o actualizar registros en objetos de la base de datos a traves del método Post
        /// </summary>
        /// <returns>Objeto de la clase EventResponse </returns>
        /// <param name="apiName">Nombre del método de la Api a Ejecutar</param>
        /// <param name="model">Objeto de la clase a enviar en el cuerpo del mensaje</param>
        public async Task<EventResponse> Post(string apiName, object model)
        {
            EventResponse? eventoAdd = new();

            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                apiName = appSettings.ApiGeoTimeUrl + "/" + apiName;
                using var client = new HttpClient();
                ConfigurarUserAgent(client);
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await getToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var postData = JsonSerializer.Serialize(model);
                var contentData = new StringContent(postData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiName, contentData);

                eventoAdd = await response.Content.ReadFromJsonAsync<EventResponse>();
            }
            catch (Exception e)
            {
                if (e.InnerException is null)
                {
                    Console.Write(e.Message);
                }
                else
                {
                    Console.Write(e.InnerException.Message);
                }
                throw;
            }
            return eventoAdd!;
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
                apiName = appSettings.ApiGeoTimeUrl + "/" + apiName;
                using var client = new HttpClient();
                ConfigurarUserAgent(client);
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await getToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var putData = JsonSerializer.Serialize(model);
                var contentData = new StringContent(putData, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(apiName, contentData);

                eventoAdd = await response.Content.ReadFromJsonAsync<EventResponse>();
            }
            catch (Exception e)
            {
                if (e.InnerException is null)
                {
                    Console.Write(e.Message);
                }
                else
                {
                    Console.Write(e.InnerException.Message);
                }
                throw;
            }
            return eventoAdd!;
        }

        //Metodo para eliminar un objeto segun el metodo indicado en el apiname y el modelo indicado como parametros
        public async Task<EventResponse> Delete(string apiName, string id)
        {
            EventResponse? respuesta = new();
            try
            {
                cParametroApiLocal appSettings = await GetUrlApi();
                apiName = $"{appSettings.ApiGeoTimeUrl}/{apiName}/{id}";
                using var client = new HttpClient();
                ConfigurarUserAgent(client);
                client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
                var token = await getToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var contentType = new MediaTypeWithQualityHeaderValue("application/json");
                client.DefaultRequestHeaders.Accept.Add(contentType);

                var responseTask = await client.DeleteAsync(apiName);

                respuesta = await responseTask.Content.ReadFromJsonAsync<EventResponse>();

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                throw;
            }
            return respuesta!;

        }

        public async Task<bool> ApiDisponible()
        {
            try
            {
                return await getConnection();
                    
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Get: Método para obtener una lista de registros de cualquier objeto de la base de datos a traves del método Get
        /// </summary>
        /// <returns>Lista de registros</returns>

        public async Task<HttpResponseMessage> Get(string method)
        {
            cParametroApiLocal appSettings = await GetUrlApi();
            using var client = new HttpClient();
            ConfigurarUserAgent(client);
            client.BaseAddress = new Uri(appSettings.ApiGeoTimeUrl);
            var api = $"{appSettings.ApiGeoTimeUrl}{method}";
            var token = await getToken();

            _logger.LogInformation($"GeoTimeConnect.Get. Method=>{api}");
            Console.WriteLine(api);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await client.GetAsync(api);
        }

        // ── Catálogos de labores (Ph_Labores*) ───────────────────────────────

        /// <summary>
        /// GetListaLabores: GET de un catálogo. Devuelve null si la respuesta no fue exitosa
        /// o si hubo excepción, para que el llamador distinga "falló" de "vacío".
        /// </summary>
        private async Task<IEnumerable<T>?> GetListaLabores<T>(string method)
        {
            try
            {
                var result = await Get(method);
                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"GetListaLabores({method}): {(int)result.StatusCode} {result.ReasonPhrase}");
                    return null;
                }

                List<T>? lista = await result.Content.ReadFromJsonAsync<List<T>>();
                return lista;
            }
            catch (Exception e)
            {
                _logger.LogError($"GetListaLabores({method}): {e.Message}");
                return null;
            }
        }

        /// <summary>Arma "/Ruta?clave=valor&amp;..." omitiendo los filtros nulos o vacíos.</summary>
        private static string ConFiltros(string ruta, params (string clave, string? valor)[] filtros)
        {
            var partes = filtros
                .Where(f => !string.IsNullOrEmpty(f.valor))
                .Select(f => $"{f.clave}={Uri.EscapeDataString(f.valor!)}")
                .ToList();
            return partes.Count == 0 ? ruta : $"{ruta}?{string.Join("&", partes)}";
        }

        public Task<IEnumerable<cPh_LaboresBloque>?> GetLaboresBloques()
            => GetListaLabores<cPh_LaboresBloque>("/PhLaboresBloque");

        public Task<IEnumerable<cPh_LaboresFinca>?> GetLaboresFincas(string? bloqueId = null)
            => GetListaLabores<cPh_LaboresFinca>(ConFiltros("/PhLaboresFinca", ("bloqueId", bloqueId)));

        public Task<IEnumerable<cPh_LaboresLote>?> GetLaboresLotes(string? bloqueId = null, string? fincaId = null)
            => GetListaLabores<cPh_LaboresLote>(ConFiltros("/PhLaboresLote", ("bloqueId", bloqueId), ("fincaId", fincaId)));

        public Task<IEnumerable<cPh_LaboresOrdenTrabajo>?> GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null)
            => GetListaLabores<cPh_LaboresOrdenTrabajo>(ConFiltros("/PhLaboresOrdenTrabajo", ("bloqueId", bloqueId), ("fincaId", fincaId)));

        public Task<IEnumerable<cPh_LaboresActividad>?> GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null)
            => GetListaLabores<cPh_LaboresActividad>(ConFiltros("/PhLaboresActividad", ("bloqueId", bloqueId), ("fincaId", fincaId), ("ordenTrabajoId", ordenTrabajoId)));

        public Task<IEnumerable<cPh_LaboresUnidad>?> GetLaboresUnidades()
            => GetListaLabores<cPh_LaboresUnidad>("/PhLaboresUnidad");

        public Task<IEnumerable<cPh_LaboresRecurso>?> GetLaboresRecursos(string? unidadId = null, int? idConcepto = null)
            => GetListaLabores<cPh_LaboresRecurso>(ConFiltros("/PhLaboresRecurso", ("unidadId", unidadId), ("idConcepto", idConcepto?.ToString())));
        public async Task<IEnumerable<cEmpleado>> GetEmpleado()
        {
            IEnumerable<cEmpleado>? empleado = null;
            HttpResponseMessage? result = null;

            try
            {
                var method = "/Empleado";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEmpleado>>();
                    readJob.Wait();
                    empleado = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                empleado = Enumerable.Empty<cEmpleado>();

            }
            return empleado!;
        }

        public async Task<IEnumerable<cEmpleado>> GetEmpleadoProgramador(string grupos)
        {
            IEnumerable<cEmpleado>? empleado = null;
            HttpResponseMessage? result = null;

            try
            {
                var method = $"/EmpleadoProgramador/{grupos}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEmpleado>>();
                    readJob.Wait();
                    empleado = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                empleado = Enumerable.Empty<cEmpleado>();

            }
            return empleado!;
        }

        /// <summary>
        /// GetEmpleado: Método para un empleado específico
        /// </summary>
        /// <returns>Una instancia de la clase cEmpleado</returns>
        /// ///<param name="id">Id del empleado requerido</param>
        public async Task<cEmpleado> GetEmpleado(string id)
        {
            cEmpleado empleado = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/Empleado/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cEmpleado>();
                    readJob.Wait();
                    empleado = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return empleado;
        }

        public async Task<cEmpleado> GetEmpleadoByEmail(string email)
        {
            cEmpleado? empleado = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/Empleado/ByEmail/{email}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cEmpleado>();
                    readJob.Wait();
                    empleado = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return empleado!;
        }


        /// <summary>
        /// getPeriodo: Método para obtener una lista de Periodos 
        /// </summary>
        /// <returns>Lista de cPeriodos</returns>
        public async Task<IEnumerable<cPh_Periodos>> GetPeriodo()
        {
            IEnumerable<cPh_Periodos> periodo = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Periodo";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Periodos>>();
                    readJob.Wait();
                    periodo = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                periodo = Enumerable.Empty<cPh_Periodos>();
            }
            return periodo;
        }

        /// <summary>
        /// getPeriodo: Método para un periodo específico
        /// </summary>
        /// <returns>Una instancia de la clase cPeriodo</returns>
        /// ///<param name="id">Id del periodo requerido</param>
        public async Task<cPh_Periodos> GetPeriodo(string id)
        {
            cPh_Periodos periodo = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Periodo/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Periodos>();
                    readJob.Wait();
                    periodo = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return periodo;
        }

        /// <summary>
        /// getPeriodo: Método para obtener una lista de Periodos 
        /// </summary>
        /// <returns>Lista de cPeriodos</returns>
        /// <param name="fecha">Fecha del periodo</param>
        /// <param name="vigente">Periodo está vigente </param>
        public async Task<IEnumerable<cPh_Periodos>> GetPeriodo(string fecha, string vigente)
        {
            IEnumerable<cPh_Periodos> periodo = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Periodo/{fecha}/{vigente}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Periodos>>();
                    readJob.Wait();
                    periodo = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                periodo = Enumerable.Empty<cPh_Periodos>();
            }
            return periodo;
        }

        /// <summary>
        /// GetPeriodoVigenteEmpleado: Método para obtener periodo vigente de un empleado 
        /// </summary>
        /// <returns>Una instancia de la clase cPeriodo</returns>
        /// <param name="idnumero">Número del empleado requerido</param>
        /// <param name="fechaPeriodo">Fecha del periodo</param>
        public async Task<cPh_Periodos> GetPeriodoVigenteEmpleado(string idnumero, string fechaPeriodo)
        {
            cPh_Periodos periodo = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PeriodoVigenteEmpleado/{idnumero}/{fechaPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Periodos>();
                    readJob.Wait();
                    periodo = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return periodo;
        }

        /// <summary>
        /// GetAccionPersonal: Método para una Acciones de Personal 
        /// </summary>
        /// <returns>Una instancia de cAccionPersonal</returns>
        /// <param name="idregistro">Id de acción</param>
        public async Task<cAccionPersonal> GetAccionPersonal(long idregistro)
        {
            cAccionPersonal accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonal/{idregistro}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cAccionPersonal>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return accionPersonal;
        }


        /// <summary>
        /// GetAccionPersonal: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="FechaInicio">Fecha de Inicio del reporte para obtener la lista de acciones de personal</param>
        /// <param name="FechaFin">Fecha de Final del reporte para obtener la lista de acciones de personal</param>
        public async Task<IEnumerable<cAccionPersonal>> GetAccionPersonal(string IdPlanilla, DateTime FechaInicio, DateTime FechaFin)
        {
            IEnumerable<cAccionPersonal> accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonal/{IdPlanilla}/{FechaInicio}/{FechaFin}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cAccionPersonal>>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                accionPersonal = Enumerable.Empty<cAccionPersonal>();
            }
            return accionPersonal;
        }

        /// <summary>
        /// GetAccionPersonal: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="FechaInicio">Fecha de Inicio del reporte para obtener la lista de acciones de personal</param>
        /// <param name="FechaFin">Fecha de Final del reporte para obtener la lista de acciones de personal</param>
        /// <param name="usuario">id de usuario que realizó el registro de la acción</param>
        public async Task<IEnumerable<cAccionPersonal>> GetAccionPersonal(string IdPlanilla, DateTime FechaInicio, DateTime FechaFin, string usuario)
        {
            IEnumerable<cAccionPersonal> accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonal/{IdPlanilla}/{FechaInicio}/{FechaFin}/{usuario}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cAccionPersonal>>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                accionPersonal = Enumerable.Empty<cAccionPersonal>();
            }
            return accionPersonal;
        }

        /// <summary>
        /// GetAccionPersonalPorEstado: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="usuario">id de usuario que realizó el registro de la acción</param>
        /// <param name="estado">Estado de la acción de personal</param>
        public async Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorEstado(string IdPlanilla, string usuario, char estado)
        {
            IEnumerable<cAccionPersonal> accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonalPorEstado/{IdPlanilla}/{usuario}/{estado}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cAccionPersonal>>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                accionPersonal = Enumerable.Empty<cAccionPersonal>();
            }
            return accionPersonal;

        }

        /// <summary>
        /// GetAccionPersonalPorPeriodo: Método para obtener una lista de Acciones de Personal por periodo 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="idnumero">id del empleado</param>
        /// <param name="fecha">fecha para determinar periodo vigente</param>
        /// <param name="idplanilla">id de planilla</param>
        public async Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorPeriodo(string idnumero, string fecha, string idplanilla)
        {
            IEnumerable<cAccionPersonal> accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonalPorPeriodo/{idnumero}/{fecha}/{idplanilla}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cAccionPersonal>>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                accionPersonal = Enumerable.Empty<cAccionPersonal>();
            }
            return accionPersonal;

        }

        /// <summary>
        /// GetAccionPersonalPorEstado: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="estado">Estado de la acción de personal</param>
        public async Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorEstado(string IdPlanilla, char estado)
        {
            IEnumerable<cAccionPersonal> accionPersonal = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/AccionPersonalPorEstado/{IdPlanilla}/{estado}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cAccionPersonal>>();
                    readJob.Wait();
                    accionPersonal = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                accionPersonal = Enumerable.Empty<cAccionPersonal>();
            }
            return accionPersonal;

        }


        /// <summary>
        /// getMarcaIn: Método para obtener una lista de Marcas de ingreso, salida y descanso 
        /// </summary>
        /// <returns>Lista de cMarcaIn</returns>
        public async Task<IEnumerable<cMarcaIn>> GetMarcaIn()
        {
            IEnumerable<cMarcaIn> marcaIn = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaIn";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaIn>>();
                    readJob.Wait();
                    marcaIn = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                marcaIn = Enumerable.Empty<cMarcaIn>();
            }
            return marcaIn;
        }

        /// <summary>
        /// getMarcaIn: Método para obtener una lista de Marcas de ingreso, salida y descanso para un colaborador
        /// </summary>
        /// <returns>Lista de cMarcaIn</returns>
        /// ///<param name="idtarjeta">idtarjeta del empleado requerido</param>
        public async Task<IEnumerable<cMarcaIn>> GetMarcaIn(string idtarjeta)
        {
            IEnumerable<cMarcaIn> marcaIn = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaIn/{idtarjeta}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaIn>>();
                    readJob.Wait();
                    marcaIn = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaIn = Enumerable.Empty<cMarcaIn>();
                Console.WriteLine(e);
            }
            return marcaIn;
        }


  
        public async Task<IEnumerable<cMarca>> GetMarcaDiariaByGrupo(string grupos, string fecha)
        {
            IEnumerable<cMarca> marcas = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaDiariaByGrupo/{grupos}/{fecha}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarca>>();
                    readJob.Wait();
                    marcas = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                marcas = Enumerable.Empty<cMarca>();
            }
            return marcas!;
        }

        public async Task<IEnumerable<cMarca>> GetMarcaDiariaByGrupoTotal(string grupos, string fecha)
        {
            IEnumerable<cMarca> marcas = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaDiariaByGrupo/total/{grupos}/{fecha}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarca>>();
                    readJob.Wait();
                    marcas = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                marcas = Enumerable.Empty<cMarca>();
            }
            return marcas!;
        }

        public async Task<IEnumerable<cMarcaExtraApb>> GetMarcaExtraApb()
        {
            IEnumerable<cMarcaExtraApb> marcaExtraApb = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaExtraApb";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaExtraApb>>();
                    readJob.Wait();
                    marcaExtraApb = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaExtraApb = Enumerable.Empty<cMarcaExtraApb>();
                Console.WriteLine(e);
            }
            return marcaExtraApb;
        }


        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una lista de Solicitudes de Horas Extra
        /// </summary>
        /// <returns>Lista de cMarcaExtraApb</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public async Task<IEnumerable<cMarcaExtraApb>> GetMarcaExtraApb(string idnumero, string fechaPeriodo, string idplanilla, bool byPeriodo)
        {
            IEnumerable<cMarcaExtraApb>? marcaExtraApb = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaExtraApb/{idnumero}/{fechaPeriodo}/{idplanilla}/{byPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaExtraApb>>();
                    readJob.Wait();
                    marcaExtraApb = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaExtraApb = Enumerable.Empty<cMarcaExtraApb>();
                Console.WriteLine(e);
            }
            return marcaExtraApb;
        }

        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una Solicitud de Horas Extra
        /// </summary>
        /// <returns>Una instancia de cMarcaExtraApb</returns>
        /// <param name="idregistro">nuero de registro</param>
        public async Task<cMarcaExtraApb> GetMarcaExtraApb(long idregistro)
        {
            cMarcaExtraApb? marcaExtraApb = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaExtraApb/{idregistro}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cMarcaExtraApb>();
                    readJob.Wait();
                    marcaExtraApb = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return marcaExtraApb;
        }

        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una lista de Horas Extra para aprobacion
        /// </summary>
        /// <returns>Lista de cMarcaExtraApb</returns>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idGrupos">id de grupos asociados al empleado </param>
        public async Task<IEnumerable<cMarcaExtraApb>> GetMarcaExtraApb(string fechaPeriodo, string idGrupos)
        {
            IEnumerable<cMarcaExtraApb>? marcaExtraApb = null;
            HttpResponseMessage result = null;
            try
            {
                idGrupos = HttpUtility.UrlEncode(idGrupos);
                var method = $"/MarcaExtraApb/{fechaPeriodo}/{idGrupos}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaExtraApb>>();
                    readJob.Wait();
                    marcaExtraApb = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaExtraApb = Enumerable.Empty<cMarcaExtraApb>();
                Console.WriteLine(e);
            }
            return marcaExtraApb;
        }

        /// <summary>
        /// GetMarcaMovTurno: Método para obtener todos los datos de la tabla Marcas_Mov_Turnos 
        /// </summary>
        /// <returns>Lista de registros de la clase cMarcaMovTurno</returns>
        public async Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno()
        {
            IEnumerable<cMarcaMovTurno>? marcaMovTurno = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaMovTurno";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaMovTurno>>();
                    readJob.Wait();
                    marcaMovTurno = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaMovTurno = Enumerable.Empty<cMarcaMovTurno>();
                Console.WriteLine(e);
            }
            return marcaMovTurno;

        }

        /// <summary>
        /// GetMarcaMovTurno: Método para obtener una lista de Marcas Mov Turnos por empleado 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren los turnos</param>
        public async Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno(string idnumero, string fechaPeriodo)
        {
            IEnumerable<cMarcaMovTurno>? marcaMovTurno = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaMovTurno/{idnumero}/{fechaPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaMovTurno>>();
                    readJob.Wait();
                    marcaMovTurno = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaMovTurno = Enumerable.Empty<cMarcaMovTurno>();
                Console.WriteLine(e);
            }
            return marcaMovTurno;
        }

        /// <summary>
        /// GetMarcaMovTurnoByGrupo: Método para obtener una lista de Marcas Mov Turnos por Grupos 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idgrupo">Número de grupos</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren los turnos</param>
        public async Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurnoByGrupo(string fechaPeriodo, string idGrupos)
        {
            IEnumerable<cMarcaMovTurno>? marcaMovTurno = null;
            HttpResponseMessage result = null;
            try
            {
                idGrupos = HttpUtility.UrlEncode(idGrupos);
                var method = $"/MarcaMovTurnoByGrupo/{fechaPeriodo}/{idGrupos}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaMovTurno>>();
                    readJob.Wait();
                    marcaMovTurno = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaMovTurno = Enumerable.Empty<cMarcaMovTurno>();
                Console.WriteLine(e);
            }
            return marcaMovTurno;
        }

        /// <summary>
        /// GetTurno: Método para obtener una lista de turnos
        /// </summary>
        /// <returns>Lista de cTurno</returns>
        public async Task<IEnumerable<cTurno>> GetTurno()
        {
            IEnumerable<cTurno> turno = null;
            HttpResponseMessage result = null;

            try
            {
                var method = "/Turno";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cTurno>>();
                    readJob.Wait();
                    turno = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                turno = Enumerable.Empty<cTurno>();

            }
            return turno;
        }

        /// <summary>
        /// GetTurno: Método para obtener un Turno específico
        /// </summary>
        /// <returns>Una instancia de la clase cturno</returns>
        /// ///<param name="id">Id del turno requerido</param>
        public async Task<cTurno> GetTurno(int id)
        {
            cTurno turno = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/Turno/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cTurno>();
                    readJob.Wait();
                    turno = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return turno;
        }

        /// <summary>
        /// GetMarca: Método para obtener una lista de Marcas por empleado y Periodo
        /// </summary>
        /// <returns>Lista de cMarca</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public async Task<IEnumerable<cMarca>> GetMarca(string idnumero, string fechaPeriodo)
        {
            IEnumerable<cMarca>? marca = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Marca/{idnumero}/{fechaPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarca>>();
                    readJob.Wait();
                    marca = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marca = Enumerable.Empty<cMarca>();
                Console.WriteLine(e);
            }
            return marca;
        }

        /// <summary>
        /// GetMarcaProceso: Método para obtener una lista de Marcas Proceso por Periodo
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public async Task<IEnumerable<cMarcaProceso>> GetMarcaProceso(string fechaPeriodo)
        {
            IEnumerable<cMarcaProceso>? marcaProceso = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaProceso/{fechaPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaProceso>>();
                    readJob.Wait();
                    marcaProceso = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaProceso = Enumerable.Empty<cMarcaProceso>();
                Console.WriteLine(e);
            }
            return marcaProceso;
        }

        /// <summary>
        /// GetMarcaProceso: Método para obtener una lista de Marcas Proceso por empleado y Periodo
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public async Task<IEnumerable<cMarcaProceso>> GetMarcaProceso(string idnumero, string fechaPeriodo)
        {
            IEnumerable<cMarcaProceso>? marcaProceso = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaProceso/{idnumero}/{fechaPeriodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaProceso>>();
                    readJob.Wait();
                    marcaProceso = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaProceso = Enumerable.Empty<cMarcaProceso>();
                Console.WriteLine(e);
            }
            return marcaProceso;
        }

        /// <summary>
        /// GetMarcaAudit: Método para obtener una lista de Marcas Auditoria por empleado, periodo y planilla
        /// </summary>
        /// <returns>Lista de cMarcaAudit</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public async Task<IEnumerable<cMarcaAudit>> GetMarcaAudit(string idnumero, string fechaPeriodo, string idplanilla)
        {
            IEnumerable<cMarcaAudit>? marcaAudit = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaAudit/{idnumero}/{fechaPeriodo}/{idplanilla}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaAudit>>();
                    readJob.Wait();
                    marcaAudit = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaAudit = Enumerable.Empty<cMarcaAudit>();
                Console.WriteLine(e);
            }
            return marcaAudit;
        }

        /// <summary>
        /// GetMarcaDescanso: Método para obtener una lista de Marcas de Descanso por empleado, periodo y planilla
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public async Task<IEnumerable<cMarcaDescanso>> GetMarcaDescanso(string idnumero, string fechaPeriodo, string idplanilla)
        {
            IEnumerable<cMarcaDescanso>? marcaDescanso = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaDescanso/{idnumero}/{fechaPeriodo}/{idplanilla}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaDescanso>>();
                    readJob.Wait();
                    marcaDescanso = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaDescanso = Enumerable.Empty<cMarcaDescanso>();
                Console.WriteLine(e);
            }
            return marcaDescanso;
        }

        /// <summary>
        /// GetCentroCosto: Método para obtener una lista de Centros de Costo
        /// </summary>
        /// <returns>Lista de cCentroCosto</returns>
        public async Task<IEnumerable<cCentroCosto>> GetCentroCosto()
        {
            IEnumerable<cCentroCosto> centroCosto = null;
            HttpResponseMessage result = null;

            try
            {
                var method = "/CentroCosto";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cCentroCosto>>();
                    readJob.Wait();
                    centroCosto = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                centroCosto = Enumerable.Empty<cCentroCosto>();

            }
            return centroCosto!;
        }

        /// <summary>
        /// GetIncidencia: Método para obtener una lista de Incidencias
        /// </summary>
        /// <returns>Lista de cIncidencia</returns>
        public async Task<IEnumerable<cIncidencia>> GetIncidencia()
        {
            IEnumerable<cIncidencia> incidencia = null;
            HttpResponseMessage result = null;

            try
            {
                var method = "/Incidencia";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cIncidencia>>();
                    readJob.Wait();
                    incidencia = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                incidencia = Enumerable.Empty<cIncidencia>();

            }
            return incidencia!;
        }

        /// <summary>
        /// GetIncidencia: Método para obtener una incidencia específica
        /// </summary>
        /// <returns>Una instancia de la clase cIncidencia</returns>
        /// ///<param name="id">Id de incidencia requerido</param>
        public async Task<cIncidencia> GetIncidencia(int id)
        {
            cIncidencia incidencia = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/Incidencia/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cIncidencia>();
                    readJob.Wait();
                    incidencia = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return incidencia;
        }

        /// <summary>
        /// GetPhPlanilla: Método para obtener una lista de Planillas
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<cPh_Planilla>> GetPhPlanilla()
        {
            IEnumerable<cPh_Planilla> planilla = null;
            HttpResponseMessage result = null;

            try
            {
                var method = "/PhPlanilla";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Planilla>>();
                    readJob.Wait();
                    planilla = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                planilla = Enumerable.Empty<cPh_Planilla>();

            }
            return planilla;
        }

        /// <summary>
        /// GetPhPlanilla: Método para obtener una planilla específica
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Planilla</returns>
        /// <param name="id">Id de planilla requerido</param>
        public async Task<cPh_Planilla> GetPhPlanilla(string id)
        {
            cPh_Planilla phPlanilla = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PhPlanilla/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Planilla>();
                    readJob.Wait();
                    phPlanilla = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return phPlanilla;
        }

        /// <summary>
        /// /Obtener lista de Compañias asociadas al usuario
        /// </summary>
        /// <returns>Lista de Companias de Usuario </returns>

        public async Task<IEnumerable<cPh_CompaniaUsuario>> GetPhCompaniaUsuario(string idnumero)
        {
            IEnumerable<cPh_CompaniaUsuario>? companiaUsuario = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PhCompaniaUsuario/{idnumero}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_CompaniaUsuario>>();
                    readJob.Wait();
                    companiaUsuario = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                companiaUsuario = Enumerable.Empty<cPh_CompaniaUsuario>();
                Console.WriteLine(e);
            }
            return companiaUsuario!;
        }

        /// <summary>
        /// /Obtener lista de Compañias del sistema
        /// </summary>
        /// <returns>Lista de Companias del sistema </returns>

        public async Task<IEnumerable<cPh_Compania>> GetPhCompania()
        {
            IEnumerable<cPh_Compania>? companias = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PhCompania";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Compania>>();
                    readJob.Wait();
                    companias = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                companias = Enumerable.Empty<cPh_Compania>();
                Console.WriteLine(e);
            }
            return companias;
        }

        /// <summary>
        /// GetMarcasIncidencias: Obtener las Marcas Incidencias para un empleado, planilla y un periodo especifico
        /// </summary>
        /// <param name="idnumero">numero de empleado a buscar</param>
        /// <param name="fecha">fecha para determinar periodo</param>
        /// <param name="idplanilla">id de planilla</param>
        /// <returns>Lista de Marcas Incidencias</returns>
        public async Task<IEnumerable<cMarcaIncidencia>> GetMarcaIncidencia(string idnumero, string fecha, string idplanilla)
        {

            IEnumerable<cMarcaIncidencia>? marcaIncidencia = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaIncidencia/{idnumero}/{fecha}/{idplanilla}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaIncidencia>>();
                    readJob.Wait();
                    marcaIncidencia = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaIncidencia = Enumerable.Empty<cMarcaIncidencia>();
                Console.WriteLine(e);
            }
            return marcaIncidencia;
        }

        /// <summary>
        /// GetMarcaIncidencia: Obtener una Marca Incidencia especifica segun el id indicado en el parámetro
        /// </summary>
        /// <param name="id">numero de marca incidencia</param>
        /// <returns>Una instancia de Marcas Incidencias</returns>
        public async Task<cMarcaIncidencia> GetMarcaIncidencia(long id)
        {
            cMarcaIncidencia? marcaIncidencia = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaIncidencia/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cMarcaIncidencia>();
                    readJob.Wait();
                    marcaIncidencia = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return marcaIncidencia;
        }

        /// <summary>
        /// GetPhUsuario: Método para obtener un registro de Usuario
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Usuario</returns>
        /// <param name="id">Id de empleado requerido</param>
        public async Task<cPh_Usuario> GetPhUsuario(string id)
        {
            cPh_Usuario phUsuario = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PhUsuario/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Usuario>();
                    readJob.Wait();
                    phUsuario = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return phUsuario;
        }

        public async Task<cPh_Usuario> GetPhUsuarioById(string id)
        {
            cPh_Usuario phUsuario = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PhUsuarioById/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Usuario>();
                    readJob.Wait();
                    phUsuario = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return phUsuario;
        }

        /// <summary>
        /// GetPhSistema: Método para obtener un registro de Sistema
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Usuario</returns>
        public async Task<cPh_Sistema> GetPhSistema()
        {
            cPh_Sistema phSistema = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PhSistema/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Sistema>();
                    readJob.Wait();
                    phSistema = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return phSistema;
        }

        /// <summary>
        /// GetPortalConfig: Método para obtener un registro de Configuracion del Portal
        /// </summary>
        /// <returns>Una instancia de la clase cPortal_Config</returns>
        public async Task<cPortal_Config> GetPortalConfig()
        {
            cPortal_Config portalConfig = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PortalConfig/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_Config>();
                    readJob.Wait();
                    portalConfig = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portalConfig;
        }

        /// <summary>
        /// GetPortalMenu: Obtener lista de opciones del menu de Principal 
        /// </summary>
        /// <returns>Lista de menus del sistema</returns>
        public async Task<IEnumerable<cPortal_Menu>> GetPortalMenu()
        {
            IEnumerable<cPortal_Menu>? portalMenu = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PortalMenu/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPortal_Menu>>();
                    readJob.Wait();
                    portalMenu = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                portalMenu = Enumerable.Empty<cPortal_Menu>();
                Console.WriteLine(e);
            }
            return portalMenu;
        }

        /// <summary>
        /// GetPortalMenu: Obtener datos de una opcion de menu 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Menu </returns>
        public async Task<cPortal_Menu> GetPortalMenu(string id)
        {
            cPortal_Menu portalMenu = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PortalMenu/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_Menu>();
                    readJob.Wait();
                    portalMenu = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portalMenu;
        }


        /// <summary>
        /// GetPortalOpcion: Obtener lista de opciones del menu de Portal 
        /// </summary>
        /// <returns>Lista de Opciones del sistema</returns>
        public async Task<IEnumerable<cPortal_Opcion>> GetPortalOpcion()
        {
            IEnumerable<cPortal_Opcion>? portal_Opcion = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PortalOpcion/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPortal_Opcion>>();
                    readJob.Wait();
                    portal_Opcion = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                portal_Opcion = Enumerable.Empty<cPortal_Opcion>();
                Console.WriteLine(e);
            }
            return portal_Opcion;
        }

        /// <summary>
        /// GetPortalOpcion: Obtener datos de una opcion de sistema 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Opcion con los datos de la opción </returns>
        public async Task<cPortal_Opcion> GetPortalOpcion(string id)
        {
            cPortal_Opcion? portal_Opcion = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/PortalOpcion/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_Opcion>();
                    readJob.Wait();
                    portal_Opcion = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portal_Opcion!;
        }

        /// <summary>
        /// getParametroEmail: paramentros de correo electronico para mensajeria 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<cParametroEmail> getParametroEmail(int id)
        {
            cParametroEmail parametro = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/ParametroEmail/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cParametroEmail>();
                    readJob.Wait();
                    parametro = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return parametro;
        }


        /// <summary>
        /// GetPortalOpcion: Obtener lista de opciones del menu de Portal 
        /// </summary>
        /// <returns>Lista de Opciones del sistema</returns>
        public async Task<IEnumerable<cPortal_Rol>> GetPortalRol()
        {
            IEnumerable<cPortal_Rol>? portalRol = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PortalRol/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPortal_Rol>>();
                    readJob.Wait();
                    portalRol = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                portalRol = Enumerable.Empty<cPortal_Rol>();
                Console.WriteLine(e);
            }
            return portalRol;
        }

        /// <summary>
        /// GetPortalOpcion: Obtener datos de una opcion de sistema 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Opcion con los datos de la opción</returns>
        public async Task<cPortal_Rol> GetPortalRol(string id)
        {
            cPortal_Rol portalRol = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PortalRol/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_Rol>();
                    readJob.Wait();
                    portalRol = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portalRol;
        }

        /// <summary>
        /// GetPortalEmpleado: Obtener lista de empleados con acceso al portal de marcas 
        /// </summary>
        /// <returns>Lista de empleados con acceso al portal de marcas</returns>
        public async Task<IEnumerable<cPortal_Empleado>> GetPortalEmpleado()
        {
            IEnumerable<cPortal_Empleado>? portalEmpleado = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PortalEmpleado/";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPortal_Empleado>>();
                    readJob.Wait();
                    portalEmpleado = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                portalEmpleado = Enumerable.Empty<cPortal_Empleado>();
                Console.WriteLine(e);
            }
            return portalEmpleado;
        }

        /// <summary>
        /// GetPortalOpcion: Obtener datos de una opcion de sistema 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Opcion con los datos de la opción</returns>
        public async Task<cPortal_Empleado> GetPortalEmpleado(string id)
        {
            cPortal_Empleado portalEmpleado = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PortalEmpleado/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_Empleado>();
                    readJob.Wait();
                    portalEmpleado = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portalEmpleado;
        }


        /// <summary>
        /// GetPortalDocMarca: Lista de documentos asociados al empleado en una fecha especifica
        /// </summary>
        /// <param name="idnumero">numero de empleado</param>
        /// <param name="fecha">fecha del documento</param>
        /// <returns>Lista de documentos asociados al empleado en una fecha especifica</returns>
        public async Task<IEnumerable<cPortal_DocMarca>> GetPortalDocMarca(string idnumero, string fecha)
        {
            IEnumerable<cPortal_DocMarca>? portalDocMarca = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/PortalDocMarca/{idnumero}/{fecha}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPortal_DocMarca>>();
                    readJob.Wait();
                    portalDocMarca = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                portalDocMarca = Enumerable.Empty<cPortal_DocMarca>();
                Console.WriteLine(e);
            }
            return portalDocMarca;
        }


        /// <summary>
        /// GetPortalDocMarca: Obtiene un documento especifico de Portal DocsMarcas
        /// </summary>
        /// <param name="id">id del registro</param>
        /// <returns>Un documento especifico de Portal DocsMarcas</returns>
        public async Task<cPortal_DocMarca> GetPortalDocMarca(long id)
        {
            cPortal_DocMarca portalDocMarca = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/PortalDocMarca/{id}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPortal_DocMarca>();
                    readJob.Wait();
                    portalDocMarca = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
            }
            return portalDocMarca;
        }


        /// <summary>
        /// GetTemplateFACE: obtiene lista de TemplateFACE (Rostros de colaboradores)
        /// </summary>
        /// <returns>lista de TemplateFACE</returns>
        public async Task<IEnumerable<cTemplateFACE>> GetTemplateFACE()
        {
            IEnumerable<cTemplateFACE>? model = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/TemplateFACE";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cTemplateFACE>>();
                    readJob.Wait();
                    model = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                model = Enumerable.Empty<cTemplateFACE>();
            }
            return model!;
        }

        /// <summary>
        /// GetTemplateFACE: obtiene una lista de registro de TemplateFACE para un empleado especifico
        /// </summary>
        /// <param name="idnumero">id de empleado a recuperar</param>
        /// <returns></returns>
        public async Task<IEnumerable<cTemplateFACE>> GetTemplateFACE(string idnumero)
        {
            IEnumerable<cTemplateFACE>? model = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/TemplateFACE/{idnumero}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cTemplateFACE>>();
                    readJob.Wait();
                    model = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                model = Enumerable.Empty<cTemplateFACE>();
            }
            return model!;
        }

        /// <summary>
        /// GetEmpleadoTotal:  Obtener lista de empleados total
        /// </summary>
        /// <returns>lista de empleados total</returns>
        public async Task<IEnumerable<cEmpleado>> GetEmpleadoTotal()
        {
            IEnumerable<cEmpleado>? empleado = null;
            HttpResponseMessage? result = null;

            try
            {
                var method = "/EmpleadoTotal";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEmpleado>>();
                    readJob.Wait();
                    empleado = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                empleado = Enumerable.Empty<cEmpleado>();

            }
            return empleado;
        }


        /// <summary>
        /// VerificaUsuariosActivosPMW:  Obtener cantidad de licencias activas
        /// </summary>
        /// <returns>Cantidad de licencias activas</returns>
        public async Task<int> VerificaUsuariosActivosPMW()
        {
            int cantidadLicencias = 0;
            HttpResponseMessage? result = null;

            try
            {
                var method = "/VerificaUsuariosActivos";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<int>();
                    readJob.Wait();
                    cantidadLicencias = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);

            }
            return cantidadLicencias;
        }

        public async Task<IEnumerable<cOrganizacionNivel>> GetOrganizacionNivel()
        {
            IEnumerable<cOrganizacionNivel>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/OrganizacionNivel";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cOrganizacionNivel>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cOrganizacionNivel>();

            }
            return modelo!;
        }

        public async Task<cOrganizacionNivel> GetOrganizacionNivel(string id)
        {
            cOrganizacionNivel? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/OrganizacionNivel/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cOrganizacionNivel>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cOrganizacion>> GetOrganizacion()
        {
            IEnumerable<cOrganizacion>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Organizacion";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cOrganizacion>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cOrganizacion>();

            }
            return modelo!;
        }

        public async Task<cOrganizacion> GetOrganizacion(int id)
        {
            cOrganizacion? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Organizacion/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cOrganizacion>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cOrganizacionResponsable>> GetOrganizacionResponsable(int OrganizacionId)
        {
            IEnumerable<cOrganizacionResponsable>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/OrganizacionResponsable/{OrganizacionId}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cOrganizacionResponsable>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cOrganizacionResponsable>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cFlujoAutorizacion>> GetFlujoAutorizacion()
        {
            IEnumerable<cFlujoAutorizacion>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/FlujoAutorizacion";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cFlujoAutorizacion>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cFlujoAutorizacion>();

            }
            return modelo!;
        }

        public async Task<cFlujoAutorizacion> GetFlujoAutorizacion(int id)
        {
            cFlujoAutorizacion? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/FlujoAutorizacion/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cFlujoAutorizacion>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cEstado>> GetEstado()
        {
            IEnumerable<cEstado> estado = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Estado";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEstado>>();
                    readJob.Wait();
                    estado = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return estado;
        }

        public async Task<cEstado> GetEstado(int id)
        {
            cEstado estado = null;
            HttpResponseMessage result = null;
            try
            {

                var method = $"/Estado/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cEstado>();
                    readJob.Wait();
                    estado = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return estado;
        }

        public async Task<IEnumerable<cSolicitudConfiguracion>> GetSolicitudConfiguracion()
        {
            IEnumerable<cSolicitudConfiguracion>? solicitudConfiguracion = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/SolicitudConfiguracion";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitudConfiguracion>>();
                    readJob.Wait();
                    solicitudConfiguracion = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return solicitudConfiguracion!;
        }
        public async Task<cSolicitudConfiguracion> GetSolicitudConfiguracion(string id)
        {
            cSolicitudConfiguracion? solicitudConfiguracion = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudConfiguracion/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cSolicitudConfiguracion>();
                    readJob.Wait();
                    solicitudConfiguracion = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return solicitudConfiguracion!;
        }

        public async Task<IEnumerable<cTipoSolicitud>> GetTipoSolicitud()
        {
            IEnumerable<cTipoSolicitud>? tipoSolicitud = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/TipoSolicitud";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cTipoSolicitud>>();
                    readJob.Wait();
                    tipoSolicitud = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return tipoSolicitud!;
        }

        public async Task<cTipoSolicitud> GetTipoSolicitud(int id)
        {
            cTipoSolicitud? tipoSolicitud = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/TipoSolicitud/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cTipoSolicitud>();
                    readJob.Wait();
                    tipoSolicitud = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return tipoSolicitud!;
        }


        public async Task<IEnumerable<cPh_Grupo>> GetGrupo()
        {
            IEnumerable<cPh_Grupo>? grupo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = "/Grupo";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Grupo>>();
                    readJob.Wait();
                    grupo = readJob.Result;

                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e);
                grupo = Enumerable.Empty<cPh_Grupo>();

            }
            return grupo!;
        }

        public async Task<cPh_Grupo> GetGrupo(int id)
        {
            cPh_Grupo? grupo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Grupo/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Grupo>();
                    readJob.Wait();
                    grupo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return grupo!;
        }

        public async Task<IEnumerable<cOrganizacionBaseResponsable>> GetOrganizacionBaseResponsable(string OrgBaseId)
        {
            IEnumerable<cOrganizacionBaseResponsable>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/OrganizacionBaseResponsable/{OrgBaseId}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cOrganizacionBaseResponsable>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cOrganizacionBaseResponsable>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cOrganizacionBaseResponsable>> GetOrganizacionBaseByJefe(string id)
        {
            IEnumerable<cOrganizacionBaseResponsable>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/OrganizacionBaseByJefe/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cOrganizacionBaseResponsable>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cOrganizacionBaseResponsable>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitud()
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Solicitud";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitud(int estado, int tipoSolicitud, string fechaInicial, string fechaFinal)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudConsulta/{estado}/{tipoSolicitud}/{fechaInicial}/{fechaFinal}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitud(string periodo, int tipoSolicitud)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudConsulta/{periodo}/{tipoSolicitud}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<cSolicitud> GetSolicitud(long id)
        {
            cSolicitud? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Solicitud/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cSolicitud>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitud(string idnumero, string fechaInicial, string fechaFinal, int tipoSolicitud)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/Solicitud/{idnumero}/{fechaInicial}/{fechaFinal}/{tipoSolicitud}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitudPorAprobar(string idnumero)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudPorAprobar/{idnumero}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitudAutorizadaUsuario(string idnumero)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudAprobadaUsuario/{idnumero}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitud>> GetSolicitudTercero(string idnumero, string fechaInicial, string fechaFinal, int tipoSolicitud)
        {
            IEnumerable<cSolicitud>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudTercero/{idnumero}/{fechaInicial}/{fechaFinal}/{tipoSolicitud}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitud>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitud>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<Autorizante>> GetAutorizantesSolicitud(int IdGrupo, int TipoSolicitudId, string IdNumero, long Id)
        {
            IEnumerable<Autorizante>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/AutorizantesSolicitud/{IdGrupo}/{TipoSolicitudId}/{IdNumero}/{Id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<Autorizante>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<Autorizante>();
            }
            return modelo!;
        }

        public async Task<IEnumerable<cEmpleadoJefatura>> GetEmpleadoJefatura()
        {
            IEnumerable<cEmpleadoJefatura> modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/EmpleadoJefatura";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEmpleadoJefatura>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo;
        }

        public async Task<cEmpleadoJefatura> GetEmpleadoJefatura(string id)
        {
            cEmpleadoJefatura? modelo = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/EmpleadoJefatura/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cEmpleadoJefatura>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return modelo!;
        }

        public async Task<IEnumerable<cEmpleadoJefatura>> GetEmpleadoJefatura(string id, bool esJefe)
        {
            IEnumerable<cEmpleadoJefatura>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {
                var method = $"/EmpleadoJefatura/{id}/{esJefe}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cEmpleadoJefatura>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
            }
            return modelo!;
        }

        public async Task<IEnumerable<cConcepto>> GetConcepto()
        {
            IEnumerable<cConcepto>? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/Concepto";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cConcepto>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cPh_CatalogoGenerico>> GetPhCatalogoGenerico(string id)
        {
            IEnumerable<cPh_CatalogoGenerico>? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/PhCatalogoGenerico/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_CatalogoGenerico>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<IEnumerable<cPh_Puesto>> GetPhPuesto()
        {
            IEnumerable<cPh_Puesto>? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/PhPuesto";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Puesto>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }
        public async Task<IEnumerable<cPh_Grupo>> GetPhGrupoByUsuario(string idUsuario)
        {
            IEnumerable<cPh_Grupo>? grupos = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/GrupoByUsuario/{idUsuario}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Grupo>>();
                    readJob.Wait();
                    grupos = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                grupos = Enumerable.Empty<cPh_Grupo>();
                Console.WriteLine(e);
            }
            return grupos!;
        }
        /// <summary>
        /// GetMarcaMovTurno: Método para obtener una lista de Marcas Mov Turnos 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idplanilla">Planilla</param>
        /// <param name="estado">Estado</param>
        /// <param name="fechaInicio">Fecha Inicio</param>
        /// <param name="fechaFinal">Fecha Final</param>
        public async Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno(string idplanilla, string estado, string fechaInicio, string fechaFinal)
        {
            IEnumerable<cMarcaMovTurno>? marcaMovTurno = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaMovTurno/{idplanilla}/{estado}/{fechaInicio}/{fechaFinal}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaMovTurno>>();
                    readJob.Wait();
                    marcaMovTurno = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaMovTurno = Enumerable.Empty<cMarcaMovTurno>();
                Console.WriteLine(e);
            }
            return marcaMovTurno;
        }

        public async Task<IEnumerable<cPh_Login>> GetPhLogin()
        {
            IEnumerable<cPh_Login>? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/PhLogin";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Login>>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<cPh_Login> GetPhLogin(int idUsuario)
        {
            cPh_Login? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/PhLoginById/{idUsuario}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Login>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        public async Task<cPh_Login> GetPhLogin(string idUsuario)
        {
            cPh_Login? modelo = null;
            HttpResponseMessage result = null;

            try
            {
                var method = $"/PhLoginByUsuario/{idUsuario}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cPh_Login>();
                    readJob.Wait();
                    modelo = readJob.Result;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }
        public async Task<IEnumerable<cMarcaDistribucionConcepto>> GetMarcaDistribucionBySup(string idnumero, string idplanilla, string periodo)
        {
            IEnumerable<cMarcaDistribucionConcepto>? marcaDistribucion = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/MarcaDistribucionConcepto/BySup/{idnumero}/{idplanilla}/{periodo}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cMarcaDistribucionConcepto>>();
                    readJob.Wait();
                    marcaDistribucion = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                marcaDistribucion = Enumerable.Empty<cMarcaDistribucionConcepto>();
                Console.WriteLine(e);
            }
            return marcaDistribucion!;
        }

        public async Task<IEnumerable<cPh_Proyecto>> GetProyectoByCCosto(string idccosto)
        {
            IEnumerable<cPh_Proyecto>? phProyectos = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/ProyectoByCCosto/{idccosto}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Proyecto>>();
                    readJob.Wait();
                    phProyectos = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                phProyectos = Enumerable.Empty<cPh_Proyecto>();
                Console.WriteLine(e);
            }
            return phProyectos!;
        }

        public async Task<IEnumerable<cPh_Proyecto>> GetProyecto()
        {
            IEnumerable<cPh_Proyecto>? phProyectos = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/Proyecto";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_Proyecto>>();
                    readJob.Wait();
                    phProyectos = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                phProyectos = Enumerable.Empty<cPh_Proyecto>();
                Console.WriteLine(e);
            }
            return phProyectos!;
        }

        public async Task<IEnumerable<cPh_FaseProyecto>> GetFaseProyecto()
        {
            IEnumerable<cPh_FaseProyecto>? phFaseProyecto = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/FaseProyecto";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_FaseProyecto>>();
                    readJob.Wait();
                    phFaseProyecto = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                phFaseProyecto = Enumerable.Empty<cPh_FaseProyecto>();
                Console.WriteLine(e);
            }
            return phFaseProyecto!;
        }

        public async Task<IEnumerable<cPh_FaseProyecto>> GetFaseProyecto(string idproyecto)
        {
            IEnumerable<cPh_FaseProyecto>? fasesProyecto = null;
            HttpResponseMessage result = null;
            try
            {
                var method = $"/FaseProyecto/{idproyecto}";
                result = await Get(method);

                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cPh_FaseProyecto>>();
                    readJob.Wait();
                    fasesProyecto = readJob.Result;
                }
            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                fasesProyecto = Enumerable.Empty<cPh_FaseProyecto>();
                Console.WriteLine(e);
            }
            return fasesProyecto!;
        }

        public async Task<string?> GetPublicIpAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var uri = new Uri("https://api.ipify.org");
                    HttpResponseMessage response = await client.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        return data.ToString();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return "";
        }

        public async Task<string?> GetServerIpAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var uri = new Uri("https://ipinfo.io/ip");
                    HttpResponseMessage response = await client.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        return data.ToString();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return "";
        }

        public async Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLabor()
        {
            IEnumerable<cSolicitudAuxLabor>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudAuxLabor";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitudAuxLabor>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitudAuxLabor>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLaborByEstado(string compania, string fechainicial, string fechafinal, int estadoId)
        {
            IEnumerable<cSolicitudAuxLabor>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudAuxLabor/ByEstado/{compania}/{fechainicial}/{fechafinal}/{estadoId}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitudAuxLabor>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitudAuxLabor>();

            }
            return modelo!;
        }

        public async Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLaborByResponsable(string compania, string idResponsable, string fechainicial, string fechafinal)
        {
            IEnumerable<cSolicitudAuxLabor>? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudAuxLabor/ByResponsable/{compania}/{idResponsable}/{fechainicial}/{fechafinal}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<IList<cSolicitudAuxLabor>>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);
                modelo = Enumerable.Empty<cSolicitudAuxLabor>();

            }
            return modelo!;
        }

        public async Task<cSolicitudAuxLabor> GetSolicitudAuxLabor(long id)
        {
            cSolicitudAuxLabor? modelo = null;
            HttpResponseMessage? result = null;
            try
            {

                var method = $"/SolicitudAuxLabor/ByEstado/{id}";
                result = await Get(method);
                if (result.IsSuccessStatusCode)
                {
                    var readJob = result.Content.ReadFromJsonAsync<cSolicitudAuxLabor>();
                    readJob.Wait();
                    modelo = readJob.Result!;
                }

            }
            catch (Exception e)
            {
                //devuelve el codigo de error
                Console.WriteLine(e.Message);

            }
            return modelo!;
        }

        
    }


}
