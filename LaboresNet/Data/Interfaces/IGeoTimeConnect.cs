using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Request;
using com.gsitcr.geotime.Models.Response;
using PortalEmpleadoGeotime.Client.Modelo;

namespace PortalEmpleadoGeotime.Client.Data.Interfaces
{
    public interface IGeoTimeConnect
    {
        public event Action OnChange;

        /// <summary>
        /// setSchema: Metodo utilizado para cambiar de esquema de base de datos en la api.
        /// </summary>
        /// <param name="compania">Instancia de la clase cPh_Compania que contiene la información del esquema de base de datos</param>
        public void setSchema(cPh_Compania compania);

        /// <summary>
        /// Post: Método para crear o actualizar registros en objetos de la base de datos a traves del método Post
        /// </summary>
        /// <returns>Objeto de la clase EventResponse </returns>
        /// <param name="apiName">Nombre del método de la Api a Ejecutar</param>
        /// <param name="model">Objeto de la clase a enviar en el cuerpo del mensaje</param>

        public Task<EventResponse> Post(string apiName, object model);

        /// <summary>
        /// Put: Método para actualizar registros en objetos de la base de datos a traves del método Put
        /// </summary>
        /// <returns>Objeto de la clase EventResponse </returns>
        /// <param name="apiName">Nombre del método de la Api a Ejecutar</param>
        /// <param name="model">Objeto de la clase a enviar en el cuerpo del mensaje</param>
        public Task<EventResponse> Put(string apiName, object model);

        /// <summary>
        /// Delete: Borar un registro
        /// </summary>
        /// <param name="apiName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<EventResponse> Delete(string apiName, string id);

        public Task<bool> ApiDisponible();

        /// <summary>
        /// GetEmpleado: Método para obtener una lista de empleados 
        /// </summary>
        /// <returns>Lista de cEmpleados</returns>
        public Task<IEnumerable<cEmpleado>> GetEmpleado();

        public Task<IEnumerable<cEmpleado>> GetEmpleadoProgramador(string grupos);

        /// <summary>
        /// GetEmpleado: Método para un empleado específico
        /// </summary>
        /// <returns>Una instancia de la clase cEmpleado</returns>
        /// ///<param name="id">Id del empleado requerido</param>
        public Task<cEmpleado> GetEmpleado(string id);

        /// <summary>
        /// GetEmpleadoByEmail: Método para un empleado específico por email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<cEmpleado> GetEmpleadoByEmail(string email);

        /// <summary>
        /// getPeriodo: Método para obtener una lista de Periodos 
        /// </summary>
        /// <returns>Lista de cPeriodos</returns>
        public Task<IEnumerable<cPh_Periodos>> GetPeriodo();

        /// <summary>
        /// GetEmpleado: Método para un periodo específico
        /// </summary>
        /// <returns>Una instancia de la clase cPeriodo</returns>
        /// ///<param name="id">Id del periodo requerido</param>
        public Task<cPh_Periodos> GetPeriodo(string id);

        /// <summary>
        /// getPeriodo: Método para obtener una lista de Periodos 
        /// </summary>
        /// <returns>Lista de cPeriodos</returns>
        /// <param name="fecha">Fecha del periodo</param>
        /// <param name="vigente">Periodo está vigente </param>
        public Task<IEnumerable<cPh_Periodos>> GetPeriodo(string fecha, string vigente);

        /// <summary>
        /// GetPeriodoVigenteEmpleado: Método para obtener periodo vigente de un empleado 
        /// </summary>
        /// <returns>Una instancia de la clase cPeriodo</returns>
        /// <param name="idnumero">Número del empleado requerido</param>
        /// <param name="fechaPeriodo">Fecha del periodo</param>
        public Task<cPh_Periodos> GetPeriodoVigenteEmpleado(string idnumero, string fechaPeriodo);


        /// <summary>
        /// GetAccionPersonal: Método para una Acciones de Personal 
        /// </summary>
        /// <returns>Una instancia de cAccionPersonal</returns>
        /// <param name="idregistro">Id de acción</param>
        public Task<cAccionPersonal> GetAccionPersonal(long idregistro);
        /// <summary>
        /// GetAccionPersonal: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="FechaInicio">Fecha de Inicio del reporte para obtener la lista de acciones de personal</param>
        /// <param name="FechaFin">Fecha de Final del reporte para obtener la lista de acciones de personal</param>
        public Task<IEnumerable<cAccionPersonal>> GetAccionPersonal(string IdPlanilla, DateTime FechaInicio, DateTime FechaFin);

        /// <summary>
        /// GetAccionPersonal: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="FechaInicio">Fecha de Inicio del reporte para obtener la lista de acciones de personal</param>
        /// <param name="FechaFin">Fecha de Final del reporte para obtener la lista de acciones de personal</param>
        /// <param name="usuario">id de usuario que realizó el registro de la acción</param>
        public Task<IEnumerable<cAccionPersonal>> GetAccionPersonal(string IdPlanilla, DateTime FechaInicio, DateTime FechaFin, string usuario);

        /// <summary>
        /// GetAccionPersonalPorEstado: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="usuario">id de usuario que realizó el registro de la acción</param>
        /// <param name="estado">Estado de la acción de personal</param>
        public Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorEstado(string IdPlanilla, string usuario, char estado);

        /// <summary>
        /// GetAccionPersonalPorPeriodo: Método para obtener una lista de Acciones de Personal por periodo 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="idnumero">id del empleado</param>
        /// <param name="fecha">fecha para determinar periodo vigente</param>
        /// <param name="idplanilla">id de planilla</param>
        public Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorPeriodo(string idnumero, string fecha, string idplanilla);

        /// <summary>
        /// GetAccionPersonalPorEstado: Método para obtener una lista de Acciones de Personal 
        /// </summary>
        /// <returns>Lista de cAccionPersonal</returns>
        /// <param name="IdPlanilla">Código de Planilla</param>
        /// <param name="estado">Estado de la acción de personal</param>
        public Task<IEnumerable<cAccionPersonal>> GetAccionPersonalPorEstado(string IdPlanilla, char estado);

        /// <summary>
        /// getMarcaIn: Método para obtener una lista de Marcas de ingreso, salida y descanso 
        /// </summary>
        /// <returns>Lista de cMarcaIn</returns>
        public Task<IEnumerable<cMarcaIn>> GetMarcaIn();

        /// <summary>
        /// getMarcaIn: Método para obtener una lista de Marcas de ingreso, salida y descanso para un colaborador
        /// </summary>
        /// <returns>Lista de cMarcaIn</returns>
        /// ///<param name="idtarjeta">idtarjeta del empleado requerido</param>
        public Task<IEnumerable<cMarcaIn>> GetMarcaIn(string idtarjeta);

        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una Solicitud de Horas Extra
        /// </summary>
        /// <returns>Una instancia de cMarcaExtraApb</returns>
        /// <param name="idregistro">nuero de registro</param>
        public Task<cMarcaExtraApb> GetMarcaExtraApb(long idregistro);

        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una lista de Solicitudes de Horas Extra
        /// </summary>
        /// <returns>Lista de cMarcaExtraApb</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public Task<IEnumerable<cMarcaExtraApb>> GetMarcaExtraApb(string idnumero, string fechaPeriodo, string idplanilla, bool byPeriodo);

        /// <summary>
        /// GetMarcaExtraApb: Método para obtener una lista de Horas Extra para aprobacion
        /// </summary>
        /// <returns>Lista de cMarcaExtraApb</returns>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idgrupo">id de grupos asociados al empleado </param>
        public Task<IEnumerable<cMarcaExtraApb>> GetMarcaExtraApb(string fechaPeriodo, string idgrupo);

        /// <summary>
        /// GetMarcaMovTurno: Método para obtener todos los datos de la tabla Marcas_Mov_Turnos 
        /// </summary>
        /// <returns>Lista de registros de la clase cMarcaMovTurno</returns>
        public Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno();

        /// <summary>
        /// GetMarcaMovTurno: Método para obtener una lista de Marcas Mov Turnos por empleado 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren los turnos</param>
        public Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno(string idnumero, string fechaPeriodo);

        /// <summary>
        /// GetMarcaMovTurnoByGrupo: Método para obtener una lista de Marcas Mov Turnos por Grupos 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idgrupo">Número de grupos</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren los turnos</param>
        public Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurnoByGrupo(string fechaPeriodo, string idgrupo);

        /// <summary>
        /// GetTurno: Método para obtener una lista de turnos
        /// </summary>
        /// <returns>Lista de cTurno</returns>
        public Task<IEnumerable<cTurno>> GetTurno();

        /// <summary>
        /// GetTurno: Método para obtener un Turno específico
        /// </summary>
        /// <returns>Una instancia de la clase cturno</returns>
        /// ///<param name="id">Id del turno requerido</param>
        public Task<cTurno> GetTurno(int id);

        /// <summary>
        /// GetMarca: Método para obtener una lista de Marcas por empleado y Periodo
        /// </summary>
        /// <returns>Lista de cMarca</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public Task<IEnumerable<cMarca>> GetMarca(string idnumero, string fechaPeriodo);

        /// <summary>
        /// GetMarcaProceso: Método para obtener una lista de Marcas Proceso por Periodo
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public Task<IEnumerable<cMarcaProceso>> GetMarcaProceso(string fechaPeriodo);

        /// <summary>
        /// GetMarca: Método para obtener una lista de Marcas Proceso por empleado y Periodo
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        public Task<IEnumerable<cMarcaProceso>> GetMarcaProceso(string idnumero, string fechaPeriodo);

        /// <summary>
        /// GetMarcaAudit: Método para obtener una lista de Marcas Auditoria por empleado, periodo y planilla
        /// </summary>
        /// <returns>Lista de cMarcaAudit</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public Task<IEnumerable<cMarcaAudit>> GetMarcaAudit(string idnumero, string fechaPeriodo, string idplanilla);

        /// <summary>
        /// GetMarcaDescanso: Método para obtener una lista de Marcas de Descanso por empleado, periodo y planilla
        /// </summary>
        /// <returns>Lista de cMarcaProceso</returns>
        /// <param name="idnumero">Número de Empleado</param>
        /// <param name="fechaPeriodo">Fecha del Periodo para el cual se requieren las marcas</param>
        /// <param name="idplanilla">id de planilla </param>
        public Task<IEnumerable<cMarcaDescanso>> GetMarcaDescanso(string idnumero, string fechaPeriodo, string idplanilla);

        /// <summary>
        /// GetCentroCosto: Método para obtener una lista de Centros de Costo
        /// </summary>
        /// <returns>Lista de cCentroCosto</returns>
        public Task<IEnumerable<cCentroCosto>> GetCentroCosto();

        /// <summary>
        /// GetIncidencia: Método para obtener una lista de Incidencias
        /// </summary>
        /// <returns>Lista de cIncidencia</returns>
        public Task<IEnumerable<cIncidencia>> GetIncidencia();

        /// <summary>
        /// GetIncidencia: Método para obtener una incidencia específica
        /// </summary>
        /// <returns>Una instancia de la clase cIncidencia</returns>
        /// ///<param name="id">Id de incidencia requerido</param>
        public Task<cIncidencia> GetIncidencia(int id);

        /// <summary>
        /// GetPhPlanilla: Método para obtener una lista de Planillas
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<cPh_Planilla>> GetPhPlanilla();

        /// <summary>
        /// GetPhPlanilla: Método para obtener una planilla específica
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Planilla</returns>
        /// <param name="id">Id de planilla requerido</param>
        public Task<cPh_Planilla> GetPhPlanilla(string id);

        /// <summary>
        /// /Obtener lista de Compañias asociadas al usuario
        /// </summary>
        /// <returns>Lista de Companias de Usuario </returns>

        public Task<IEnumerable<cPh_CompaniaUsuario>> GetPhCompaniaUsuario(string idnumero);

        /// <summary>
        /// /Obtener lista de Compañias del sistema
        /// </summary>
        /// <returns>Lista de Companias del sistema </returns>
        public Task<IEnumerable<cPh_Compania>> GetPhCompania();

        /// <summary>
        /// GetMarcasIncidencias: Obtener las Marcas Incidencias para un empleado, planilla y un periodo especifico
        /// </summary>
        /// <param name="idnumero">numero de empleado a buscar</param>
        /// <param name="fecha">fecha para determinar periodo</param>
        /// <param name="idplanilla">id de planilla</param>
        /// <returns>Lista de Marcas Incidencias</returns>
        public Task<IEnumerable<cMarcaIncidencia>> GetMarcaIncidencia(string idnumero, string fecha, string idplanilla);

        /// <summary>
        /// GetMarcaIncidencia: Obtener una Marca Incidencia especifica segun el id indicado en el parámetro
        /// </summary>
        /// <param name="id">numero de marca incidencia</param>
        /// <returns>Una instancia de Marcas Incidencias</returns>
        public Task<cMarcaIncidencia> GetMarcaIncidencia(long id);

        /// <summary>
        /// GetPhUsuario: Método para obtener un registro de Usuario
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Usuario</returns>
        /// <param name="id">Id de empleado requerido</param>
        public Task<cPh_Usuario> GetPhUsuario(string id);
        public Task<cPh_Usuario> GetPhUsuarioById(string id);

        /// <summary>
        /// GetPhSistema: Método para obtener un registro de Sistema
        /// </summary>
        /// <returns>Una instancia de la clase cPh_Usuario</returns>
        public Task<cPh_Sistema> GetPhSistema();

        /// <summary>
        /// GetPortalConfig: Método para obtener un registro de Configuracion del Portal
        /// </summary>
        /// <returns>Una instancia de la clase cPortal_Config</returns>
        public Task<cPortal_Config> GetPortalConfig();

        /// <summary>
        /// GetPortalMenu: Obtener lista de opciones del menu de Principal 
        /// </summary>
        /// <returns>Lista de menus del sistema</returns>
        public Task<IEnumerable<cPortal_Menu>> GetPortalMenu();

        /// <summary>
        /// GetPortalMenu: Obtener datos de una opcion de menu 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Menu </returns>
        public Task<cPortal_Menu> GetPortalMenu(string id);

        /// <summary>
        /// GetPortalOpcion: Obtener lista de opciones del menu de Portal 
        /// </summary>
        /// <returns>Lista de Opciones del sistema</returns>
        public Task<IEnumerable<cPortal_Opcion>> GetPortalOpcion();

        /// <summary>
        /// GetPortalOpcion: Obtener datos de una opcion de sistema 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Opcion con los datos de la opción </returns>
        public Task<cPortal_Opcion> GetPortalOpcion(string id);
        public Task<cParametroEmail> getParametroEmail(int id);

        /// <summary>
        /// GetPortalOpcion: Obtener lista de opciones del menu de Portal 
        /// </summary>
        /// <returns>Lista de Opciones del sistema</returns>
        public Task<IEnumerable<cPortal_Rol>> GetPortalRol();

        /// <summary>
        /// GetPortalOpcion: Obtener datos de una opcion de sistema 
        /// </summary>
        /// <param name="id">id de la opcion</param>
        /// <returns>Instancia de cPortal_Opcion con los datos de la opción </returns>
        public Task<cPortal_Rol> GetPortalRol(string id);

        /// <summary>
        /// GetPortalEmpleado: Método para obtener una lista de empleados con acceso a Marcas Web
        /// </summary>
        /// <returns>Lista de cPortal_Empleado</returns>
        public Task<IEnumerable<cPortal_Empleado>> GetPortalEmpleado();

        /// <summary>
        /// GetTurno: Método para obtener un empleado con acceso a Marcas Web
        /// </summary>
        /// <returns>Una instancia de la clase cPortal_Empleado</returns>
        /// <param name="id">Id del empelado requerido</param>
        public Task<cPortal_Empleado> GetPortalEmpleado(string id);

        /// <summary>
        /// GetPortalDocMarca: Lista de documentos asociados al empleado en una fecha especifica
        /// </summary>
        /// <param name="idnumero">numero de empleado</param>
        /// <param name="fecha">fecha del documento</param>
        /// <returns>Lista de documentos asociados al empleado en una fecha especifica</returns>
        public Task<IEnumerable<cPortal_DocMarca>> GetPortalDocMarca(string idnumero, string fecha);

        /// <summary>
        /// GetPortalDocMarca: Obtiene un documento especifico de Portal DocsMarcas
        /// </summary>
        /// <param name="id">id del registro</param>
        /// <returns>Un documento especifico de Portal DocsMarcas</returns>
        public Task<cPortal_DocMarca> GetPortalDocMarca(long id);

        /// <summary>
        /// GetTemplateFACE: obtiene lista de TemplateFACE (Rostros de colaboradores)
        /// </summary>
        /// <returns>lista de TemplateFACE</returns>
        public Task<IEnumerable<cTemplateFACE>> GetTemplateFACE();

        /// <summary>
        /// GetTemplateFACE: obtiene una lista de registro de TemplateFACE para un empleado especifico
        /// </summary>
        /// <param name="idnumero">id de empleado a recuperar</param>
        /// <returns></returns>
        public Task<IEnumerable<cTemplateFACE>> GetTemplateFACE(string idnumero);

        /// <summary>
        /// GetEmpleadoTotal:  Obtener lista de empleados total
        /// </summary>
        /// <returns>lista de empleados total</returns>
        public Task<IEnumerable<cEmpleado>> GetEmpleadoTotal();

        /// <summary>
        /// VerificaUsuariosActivosPMW:  Verifica cantidad de usuarios habilitados en la base de datos para el portal de marcas web
        /// </summary>
        /// <returns>cantidad de usuarios habilitados para el portal de marcas web</returns>
        public Task<int> VerificaUsuariosActivosPMW();


        /// <summary>
        /// GetOrganizacionNivel: obtener todos los registros de Organizacion Niveles
        /// </summary>
        /// <returns>Lista con todos los registros de Organizacion Niveles</returns>
        public Task<IEnumerable<cOrganizacionNivel>> GetOrganizacionNivel();

        /// <summary>
        /// GetOrganizacionNivel: Obtiene un registro de Organizacion Niveles
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Un registro de Organizacion Niveles segun el id de parametro indicado</returns>
        public Task<cOrganizacionNivel> GetOrganizacionNivel(string id);

        /// <summary>
        /// GetOrganizacion: obtener todos los registros de Organizacion
        /// </summary>
        /// <returns>Lista con todos los registros de Organizacion</returns>
        public Task<IEnumerable<cOrganizacion>> GetOrganizacion();
        /// <summary>
        /// GetOrganizacion: Obtiene un registro de Organizacion 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Un registro de Organizacion segun el id de parametro indicado</returns>
        public Task<cOrganizacion> GetOrganizacion(int id);

        public Task<IEnumerable<cOrganizacionResponsable>> GetOrganizacionResponsable(int OrganizacionId);

        /// <summary>
        /// GetFlujoAutorizacion: obtener todos los registros de flujos de autorizacion
        /// </summary>
        /// <returns>Lista con todos los registros de flujos de autorizacion</returns>
        public Task<IEnumerable<cFlujoAutorizacion>> GetFlujoAutorizacion();
        /// <summary>
        /// GetFlujoAutorizacion: Obtiene un registro de flujo de autorizacion 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Un registro de flujo de autorizacion  segun el id de parametro indicado</returns>
        public Task<cFlujoAutorizacion> GetFlujoAutorizacion(int id);

        public Task<IEnumerable<cEstado>> GetEstado();
        public Task<cEstado> GetEstado(int id);
        public Task<IEnumerable<cSolicitudConfiguracion>> GetSolicitudConfiguracion();
        public Task<cSolicitudConfiguracion> GetSolicitudConfiguracion(string id);
        public Task<IEnumerable<cTipoSolicitud>> GetTipoSolicitud();
        public Task<cTipoSolicitud> GetTipoSolicitud(int id);
        public Task<IEnumerable<cPh_Grupo>> GetGrupo();
        public Task<cPh_Grupo> GetGrupo(int id);

        public Task<IEnumerable<cOrganizacionBaseResponsable>> GetOrganizacionBaseResponsable(string OrgBaseId);


        public Task<IEnumerable<cSolicitud>> GetSolicitud();
        public Task<IEnumerable<cSolicitud>> GetSolicitud(int estado, int tipoSolicitud, string fechaInicial, string fechaFinal);
        public Task<IEnumerable<cSolicitud>> GetSolicitud(string periodo, int tipoSolicitud);

        /// <summary>
        /// GetSolicitudes: Obtiene un registro de una solicitud
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Un registro de solicitud segun el id de parametro indicado</returns>
        public Task<cSolicitud> GetSolicitud(long id);

        /// <summary>
        /// GetSolicitud: obtiene lista de solictudes para un colaborador según el id de número, fecha inicial, fecha final y tipo de solicitud.
        /// </summary>
        /// <param name="idnumero"></param>
        /// <param name="fechaInicial"></param>
        /// <param name="fechaFinal"></param>
        /// <param name="tipoSolicitud"></param>
        /// <returns></returns>
        public Task<IEnumerable<cSolicitud>> GetSolicitud(string idnumero, string fechaInicial, string fechaFinal, int tipoSolicitud);

        //Creado por: Marlon Loria Solano
        //Fecha: 2025-07-25  
        //obtener lista Solicitudes de Accion de Personal por Usuario autorizador Y FLUJO DE AUTORIZACION
        public Task<IEnumerable<cSolicitud>> GetSolicitudPorAprobar(string idnumero);

        /// <summary>
        ///  GetSolicitudAutorizadaUsuario: obtiene lista de autorizantes que han autorizado solicitudes para un usuario específico   
        /// </summary>
        /// <param name="IdNumero"></param>
        /// <returns></returns>
        public Task<IEnumerable<cSolicitud>> GetSolicitudAutorizadaUsuario(string IdNumero);

        /// <summary>
        /// GetSolicitud: obtiene lista de solictudes para un colaborador según el id de número, fecha inicial, fecha final y tipo de solicitud.
        /// </summary>
        /// <param name="idnumero"></param>
        /// <param name="fechaInicial"></param>
        /// <param name="fechaFinal"></param>
        /// <param name="tipoSolicitud"></param>
        /// <returns></returns>
        public Task<IEnumerable<cSolicitud>> GetSolicitudTercero(string idnumero, string fechaInicial, string fechaFinal, int tipoSolicitud);

        /// <summary>
        /// GetAutorizantesSolicitud: obtiene una lista de autorizantes para una solicitud específica
        /// </summary>
        /// <param name="IdGrupo"></param>
        /// <param name="TipoSolicitudId"></param>
        /// <param name="IdNumero"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<IEnumerable<Autorizante>> GetAutorizantesSolicitud(int IdGrupo, int TipoSolicitudId, string IdNumero, long Id);

        

        
        public Task<IEnumerable<cEmpleadoJefatura>> GetEmpleadoJefatura();
        public Task<cEmpleadoJefatura> GetEmpleadoJefatura(string id);
        public Task<IEnumerable<cEmpleadoJefatura>> GetEmpleadoJefatura(string id, bool esJefe);

        public Task<IEnumerable<cConcepto>> GetConcepto();
        public Task<IEnumerable<cPh_CatalogoGenerico>> GetPhCatalogoGenerico(string id);
        public Task<IEnumerable<cPh_Puesto>> GetPhPuesto();
        public Task<IEnumerable<cMarca>> GetMarcaDiariaByGrupo(string grupos, string fecha);
        public Task<IEnumerable<cMarca>> GetMarcaDiariaByGrupoTotal(string grupos, string fecha);
        public Task<IEnumerable<cOrganizacionBaseResponsable>> GetOrganizacionBaseByJefe(string id);
        public Task<IEnumerable<cPh_Grupo>> GetPhGrupoByUsuario(string idUsuario);

        /// <summary>
        /// GetMarcaMovTurno: Método para obtener una lista de Marcas Mov Turnos 
        /// </summary>
        /// <returns>Lista de cMarcaMovTurno</returns>
        /// <param name="idplanilla">Planilla</param>
        /// <param name="estado">Estado</param>
        /// <param name="fechaInicio">Fecha Inicio</param>
        /// <param name="fechaFinal">Fecha Final</param>
        public Task<IEnumerable<cMarcaMovTurno>> GetMarcaMovTurno(string idplanilla, string estado, string fechaInicio, string fechaFinal);

        public Task<IEnumerable<cPh_Login>> GetPhLogin();
        public Task<cPh_Login> GetPhLogin(int id);
        public Task<cPh_Login> GetPhLogin(string id);

        /// <summary>
        /// GetMarcaDistribucionBySup: obtener marcas distribucion de un supervisor
        /// </summary>
        /// <param name="idnumero"></param>
        /// <param name="idplanilla"></param>
        /// <param name="periodo"></param>
        /// <returns></returns>
        public Task<IEnumerable<cMarcaDistribucionConcepto>> GetMarcaDistribucionBySup(string idnumero, string idplanilla, string periodo);
        /// <summary>
        /// getproyectoByCCosto: obtener proyectos asociados a un centro de costo
        /// </summary>
        /// <param name="idccosto"></param>
        /// <returns></returns>
        public Task<IEnumerable<cPh_Proyecto>> GetProyectoByCCosto(string idccosto);

        public Task<IEnumerable<cPh_Proyecto>> GetProyecto();

        /// <summary>
        /// GetFaseProyecto: obtener fases asociadas a un proyecto
        /// </summary>
        /// <param name="idproyecto"></param>
        /// <returns></returns>
        public Task<IEnumerable<cPh_FaseProyecto>> GetFaseProyecto(string idproyecto);
        public  Task<IEnumerable<cPh_FaseProyecto>> GetFaseProyecto();
        public Task<string?> GetPublicIpAsync();
        public Task<string?> GetServerIpAsync();

        public Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLabor();
        public Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLaborByEstado(string compania, string fechainicial, string fechafinal, int estadoId);
        public Task<IEnumerable<cSolicitudAuxLabor>> GetSolicitudAuxLaborByResponsable(string compania, string idResponsable, string fechainicial, string fechafinal);
        public Task<cSolicitudAuxLabor> GetSolicitudAuxLabor(long id);

        // ── Catálogos de labores (Ph_Labores*) ───────────────────────────────
        // Devuelven null cuando la API no respondió con éxito (distinto de lista vacía).

        /// <summary>GetLaboresBloques: lista los bloques de labores.</summary>
        public Task<IEnumerable<cPh_LaboresBloque>?> GetLaboresBloques();

        /// <summary>GetLaboresFincas: lista las fincas; opcionalmente de un bloque.</summary>
        public Task<IEnumerable<cPh_LaboresFinca>?> GetLaboresFincas(string? bloqueId = null);

        /// <summary>GetLaboresLotes: lista los lotes; opcionalmente por bloque y/o finca.</summary>
        public Task<IEnumerable<cPh_LaboresLote>?> GetLaboresLotes(string? bloqueId = null, string? fincaId = null);

        /// <summary>GetLaboresOrdenesTrabajo: lista las órdenes de trabajo; opcionalmente por bloque y/o finca.</summary>
        public Task<IEnumerable<cPh_LaboresOrdenTrabajo>?> GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null);

        /// <summary>GetLaboresActividades: lista las actividades; opcionalmente por bloque, finca y/u orden de trabajo.</summary>
        public Task<IEnumerable<cPh_LaboresActividad>?> GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null);

        /// <summary>GetLaboresUnidades: lista las unidades de medida.</summary>
        public Task<IEnumerable<cPh_LaboresUnidad>?> GetLaboresUnidades();

        /// <summary>GetLaboresRecursos: lista los recursos; opcionalmente por unidad y/o concepto.</summary>
        public Task<IEnumerable<cPh_LaboresRecurso>?> GetLaboresRecursos(string? unidadId = null, int? idConcepto = null);

        /// <summary>GetPhOpciones: opciones generales del sistema (ph_opciones).</summary>
        public Task<cPh_Opciones?> GetPhOpciones();

        /// <summary>PostMarcaGrupal: registra una marca grupal. Lanza excepción ante fallas técnicas.</summary>
        public Task<cMarcaGrupalResultado> PostMarcaGrupal(cMarcaGrupalRequest request);

    }
}
