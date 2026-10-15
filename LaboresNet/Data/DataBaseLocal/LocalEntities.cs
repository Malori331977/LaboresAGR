using BlazorBootstrap;
using System;
using System.ComponentModel.DataAnnotations;
using SQLite;

namespace PortalEmpleadoGeotime.Client.Data.DataBaseLocal
{
    [Table("cEmpleado")]
    public class cEmpleadoLocal
    {
        [PrimaryKey]
        public string IdNumero { get; set; }
        public string? IdPlanilla { get; set; }
        public String? Nombre { get; set; }
        public String? Tarjeta { get; set; }
        public String? Identificacion { get; set; }
        public String? IdDepartamento { get; set; }
        public string? Estado { get; set; }
        public String? IdCCosto { get; set; }
        public DateTime? Fecha_Ingreso { get; set; }
        public String? Email { get; set; }
        public int? IdGrupo { get; set; }
        public int? IdHorario { get; set; }
        public int? IdAgrupamiento { get; set; }
        public string? Tipo_Marca { get; set; }
        public string? global_clave { get; set; }
        public DateTime? Fecha_Salida { get; set; }
        public string? global_code { get; set; }
        public DateTime? fecha_act_code { get; set; }
        public byte[]? foto { get; set; }
        public string? exporta { get; set; }
        public string? ubicacion { get; set; }
        public string? rubro1 { get; set; }
        public string? rubro2 { get; set; }
        public string? rubro3 { get; set; }
        public string? rubro4 { get; set; }
        public string? rubro5 { get; set; }
        public string? rubro6 { get; set; }
        public string? rubro7 { get; set; }
        public string? rubro8 { get; set; }
        public string? rubro9 { get; set; }
        public string? rubro10 { get; set; }
        public string? rubro11 { get; set; }
        public string? rubro12 { get; set; }
        public string? rubro13 { get; set; }
        public string? rubro14 { get; set; }
        public string? rubro15 { get; set; }
        public string? rubro16 { get; set; }
        public string? rubro17 { get; set; }
        public string? rubro18 { get; set; }
        public string? rubro19 { get; set; }
        public string? rubro20 { get; set; }
        public string? rubro21 { get; set; }
        public string? rubro22 { get; set; }
        public string? rubro23 { get; set; }
        public string? rubro24 { get; set; }
        public string? rubro25 { get; set; }
        public DateTime? inicio_rol { get; set; }
        public string? web_pass { get; set; }
        public int? id_transfo_conc { get; set; }
        public string? widioma { get; set; }
        public string? def_cc { get; set; }
        public string? def_py { get; set; }
        public string? def_fase { get; set; }
        public string? puesto { get; set; }

        public string? TCompensacionAprobado { get; set; }
        public string? TCompensacionUtilizado { get; set; }
        public string? Sexo { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    [Table("cMarcaIn")]
    public class cMarcaInLocal
    {
        [PrimaryKey, AutoIncrement]
        public int? Id { get; set; }

        public string? idtarjeta { get; set; }
        public DateTime? fecha { get; set; }
        public string? hora { get; set; }
        public string? idterminal { get; set; }
        public int? tipo { get; set; }
        public string? idplanilla { get; set; }
        public string? idnumero { get; set; }
        public string? estado { get; set; }
        public DateTime? fecha_reg { get; set; }
        public string? long_reg { get; set; }
        public string? lat_reg { get; set; }
        public byte[]? imagen_reg { get; set; }
        public string? gps_cell { get; set; }
        public string? host { get; set; }
        public string? dir_ip { get; set; }
    }

    [Table("ParametroSinc")]
    public class ParametroSincLocal
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string? UltimaSincronización { get; set; }
    }

    [Table("cPh_Periodos")]
    public class cPh_PeriodosLocal
    {
        [PrimaryKey]
        public string idperiodo { get; set; }
        public string? tipo_planilla { get; set; }
        public DateTime inicio { get; set; }
        public DateTime fin { get; set; }
        public string? estado { get; set; }
        public DateTime? inicio_proy { get; set; }
        public DateTime? fin_proy { get; set; }
        public string? periodo_proy { get; set; }
        public int? dia_inicio { get; set; }
    }

    [Table("cPh_Planilla")]
    public class cPh_PlanillaLocal
    {
        [PrimaryKey]
        public string idplanilla { get; set; }
        public string planilla { get; set; }
        public string? tipo_planilla { get; set; }
        public string? nom_conector { get; set; }
        public int? nivel_aprob_ext { get; set; }
        public string? tipo_adic { get; set; }
        public string? agrup_salida { get; set; }
        public string? ext_det { get; set; }
        public string? ext_per_ant { get; set; }
        public string? est_nomina { get; set; }
        public string? tipo_dist { get; set; }
        public string? auto_proceso { get; set; }
        public int? dia_inicio { get; set; }
        public string? proyecta { get; set; }
        public string? m_desc { get; set; }
        public string? c_adic { get; set; }
        public string? c_inci { get; set; }
        public string? c_ext { get; set; }


    }

    [Table("cPh_Compania")]
    public class cPh_CompaniaLocal
    {
        [PrimaryKey]
        public string IDCOMP { get; set; }
        public string COMPANIA { get; set; }
        public string? NOM_CONECTOR { get; set; }
        public string? STRING_SQL { get; set; }
        public string? STRING_SQL_ERP { get; set; }
        public string? PAIS { get; set; }
        public string? AUTO_PROCESO { get; set; }
        public string? REMOTE_ERPSERVICE { get; set; }
        public string? MAIL_SERVER { get; set; }
        public string? MAIL_USER { get; set; }
        public string? MAIL_PASSWORD { get; set; }
        public int? MAIL_PORT { get; set; }
        public string? MAIL_AUTH { get; set; }
        public string? MAIL_SSL { get; set; }
        public string? HORA_SUP { get; set; }
        public string? HORA_EMP { get; set; }
        public string? SUPERVISOR_ACUM { get; set; }
        public string? MAIL_TLS { get; set; }
        public string? HORA_CALC { get; set; }
        public string? IN_MARCAS { get; set; }
        public int ZONAHORARIA { get; set; }

        public string? APICLIENTID { get; set; }
        public string? APIUSER { get; set; }
        public string? APIPASSWORD { get; set; }
        public string? APIDATABASE { get; set; } = "";
        public string? APIURL { get; set; } = "";
        public string? APIDATABASEERP { get; set; } = "";
        public string? APISCHEMAERP { get; set; } = "";
        public string? APIURLERP { get; set; } = "";
        public string? SINCAUTODEPTO { get; set; } = "N";
        public string? SINCAUTOCCOSTO { get; set; } = "N";
        public string? SINCAUTOPUESTO { get; set; } = "N";
        public string? SINCAUTONOMINA { get; set; } = "N";
        public string? SINCAUTOEMPLEADO { get; set; } = "N";

    }

    [Table("cPortal_Config")]
    public class cPortal_ConfigLocal
    {
        [PrimaryKey]
        public string? IDAPLICACION { get; set; }

        public string IDVERSION { get; set; }
        public string IDLICENCIA { get; set; }
        public string COMPANIA { get; set; }
        public string BASEDATOS { get; set; }
        public bool PERMANENTE { get; set; }
        public bool ACTIVA { get; set; }
        public string REGSITROLIC { get; set; }
        public bool USORESTRINGIDO { get; set; }
        public bool USARECONOCIMIENTOFACIAL { get; set; }
        public bool USARGEOLOCALIZACION { get; set; }
        public bool LOCALIZACIONREQ { get; set; }
        public bool AUTOREGISTROROSTRO { get; set; }
        public bool VERLOGMARCAS { get; set; }
        public string? MAPAPIKEY { get; set; }
        public decimal FACEDIST { get; set; }
        public decimal FACETEXT { get; set; }
        public string ORGANIZACIONBASEID { get; set; }
        public int CANTMAXPLANTILLAS { get; set; }
        public bool VERTARIFA { get; set; }
        public DateTime FECHAULTMODIFICA { get; set; }
        public string IDUSUARIOMODIFICA { get; set; }
        public string TIPOIDENTIFICADOR { get; set; }
        public bool USAPROYECTOS { get; set; }

    }

    [Table("NavItemLocal")]
    public class NavItemLocal
    {
        [PrimaryKey, AutoIncrement]
        public int Index { get; set; }
        public string Id { get; set; }
        public string Usuario { get; set; }
        public string Href { get; set; }
        public int IconName { get; set; }
        public string Text { get; set; }
        public string ParentId { get; set; }
        public int IconColor { get; set; }
        public int Level { get; set; }
        public int Sequence { get; set; }
    }

    [Table("cMarcaDistribucionConcepto")]
    public class cMarcaDistribucionConceptoLocal
    {
        [PrimaryKey, AutoIncrement]
        public long IDREGISTRO { get; set; }
        public string IDPLANILLA { get; set; }
        public string IDNUMERO { get; set; }
        public DateTime FECHA { get; set; }
        public string IDCCOSTO { get; set; }
        public string? PROYECTO { get; set; }
        public string? FASE { get; set; }
        public decimal? CANTIDAD { get; set; }
        public string? INICIO { get; set; }
        public string? FIN { get; set; }
        public string ESTADO { get; set; }
        public int IDDIST { get; set; }
        public DateTime? FECHA_DIST { get; set; }
        public string? LON_REG { get; set; }
        public string? LAT_REG { get; set; }
        public string? COMENTARIO { get; set; }
        public long? idsolicitud { get; set; }
        public string IdUsuario { get; set; }
    }

    [Table("cPh_Proyecto")]
    public class cPh_ProyectoLocal
    {
        public string PROYECTO { get; set; }
        public string DESCRIPCION { get; set; }
        public string? CENTRO_COSTO { get; set; }
    }

    [Table("cPh_FaseProyecto")]
    public class cPh_FaseProyectoLocal
    {
        public string PROYECTO { get; set; }
        public string FASE { get; set; }
        public string? NOMBRE { get; set; }
        public string DESCRIPCION { get; set; }
        public string ACEPTA_DATOS { get; set; }
    }

    [Table("cCentroCosto")]
    public class cCentroCostoLocal
    {
        [PrimaryKey]
        public String? IdCCosto { get; set; }
        public String? Descripcion { get; set; }
        public string? Distribuye { get; set; }
        public string? Alias_CCosto { get; set; }
    }
    [Table("cPh_Usuario")]
    public class cPh_UsuarioLocal
    {
        [PrimaryKey]
        public int IDUSUARIO { get; set; }
        public string? CODIGO { get; set; }
        public string? PLANILLAS { get; set; }
        public int? NIVEL { get; set; }
        public string? GRUPOS { get; set; }
        public string? ESTADO { get; set; }
        public string? FPERIODO { get; set; }
        public string? FPLANILLA { get; set; }
        public string? TURNOS { get; set; }
        public string ORDEN_EMP { get; set; }
        public string TIPO_EDT { get; set; }
        public string? FILT_PRGT { get; set; }
        public int? NIVEL_APROB_EXT { get; set; }
        public int? FCANT_MESES { get; set; }
        public DateTime? FFECHA_EVALUAR { get; set; }
        public string? PT_AGRUP { get; set; }
    }

    [Table("cTemplateFACE")]
    public class cTemplateFACELocal
    {
        public string IDNUMERO { get; set; }
        public int INDEXID { get; set; }
        public byte[] FACETEMPLATE { get; set; }
    }

    [Table("ParametroApi")]
    public class cParametroApiLocal
    {
        [PrimaryKey]
        public int Id { get; set; } = 0;
        public string ApiGeoTimeUrl { get; set; } = "";
        public string ApiGeotimeDataBase { get; set; } = "";
        public string ApiGeotimeSchema { get; set; } = "";
        public string ApiClientId { get; set; } = "";
        public string ApiUser { get; set; } = "";
        public string ApiPassword { get; set; } = "";
        public string ApiAuxiliarUrl { get; set; } = "";
        public string ApiAuxiliarSchema { get; set; } = "";
    }

    // ─── Catálogos de labores (Ph_Labores*) ─────────────────────────────────────
    // Las PKs son compuestas; las tablas se crean con SQL raw en MarcasDBLocal.

    [Table("LaboresBloque")]
    public class LaboresBloqueLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresFinca")]
    public class LaboresFincaLocal
    {
        public string BloqueId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresLote")]
    public class LaboresLoteLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresOrdenTrabajo")]
    public class LaboresOrdenTrabajoLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresActividad")]
    public class LaboresActividadLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string OrdenTrabajoId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresUnidad")]
    public class LaboresUnidadLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresRecurso")]
    public class LaboresRecursoLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
        public int IdConcepto { get; set; }
        public string UnidadId { get; set; } = "";
    }

    // ─── TicoFrut local entities ────────────────────────────────────────────────
    // Las PKs son compuestas; las tablas se crean con SQL raw en MarcasDBLocal.

    [Table("GeoAgrLaborAuxiliar")]
    public class GeoAgrLaborAuxiliarLocal
    {
        /// <summary>Identificador único de registro (parte de la PK compuesta).</summary>
        public long      IdRegistro  { get; set; }
        public string?   Compania    { get; set; }
        public string?   IdEmpleado  { get; set; }
        public DateTime  Fecha       { get; set; }
        public string?   Comentarios { get; set; }
        public int EstadoId { get; set; } = 0;
    }

    [Table("GeoAgrLaborAuxiliarDet")]
    public class GeoAgrLaborAuxiliarDetLocal
    {
        /// <summary>FK → GeoAgrLaborAuxiliar.IdRegistro; parte de la PK compuesta.</summary>
        public long      IdRegistro           { get; set; }
        public string?   Compania             { get; set; }
        public string?   IdEmpleado           { get; set; }
        public DateTime  Fecha                { get; set; }
        public int       Item                 { get; set; }
        public string?   Bloque               { get; set; }
        public string?   Finca                { get; set; }
        public string?   Lote                 { get; set; }
        public string?   Ot                   { get; set; }
        public string?   Labor                { get; set; }
        public string?  Recurso               { get; set; }
        public decimal?  CantidadRendimiento { get; set; }
        public string?   Unidad               { get; set; }
        public decimal?  HorasAprobadas       { get; set; }
        public int EstadoId { get; set; } = 0;
        /// <summary>Empleado al que se asignó este ítem; parte de la PK compuesta.</summary>
        public string?   IdEmpleadoAsignado   { get; set; }
    }

    [Table("FiltroUsuario")]
    public class FiltroUsuarioLocal
    {
        /// <summary>IdNumero del empleado autenticado.</summary>
        [PrimaryKey]
        public string IdUsuario  { get; set; } = "";
        public string Pais       { get; set; } = "";
        public string Compania   { get; set; } = "";
    }

    [Table("cEstado")]
    public class EstadoLocal
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Descripcion { get; set; } = "";
    }

    [Table("cPh_Login")]
    public class cPhLoginLocal
    {
        [PrimaryKey]
        public int idusuario { get; set; }
        public string usuario { get; set; }
        public string clave { get; set; }
        public string descripcion { get; set; }
        public int? privilegio { get; set; }
        public int? grupo_base { get; set; }
        public long? idsesion { get; set; }
        public DateTime? ultimo_login { get; set; }
        public string? fcomp { get; set; }
        public string? fplani { get; set; }
        public string? fperiodo { get; set; }
        public string? grupos { get; set; }
        public int? proceso { get; set; }
        public DateTime? ultimo_estado { get; set; }
        public string? companias { get; set; }
        public string? planillas { get; set; }
        public string? idioma { get; set; }
        public string? stat_msg { get; set; }
        public string? turnos { get; set; }
        public string? usa_wusuario { get; set; }
        public string estado { get; set; }
        public string? EMAIL { get; set; }
        public string? OMITE_LIC { get; set; }
        public string? ACTIVO { get; set; }
        public string? GLOBAL_CLAVE { get; set; }
        public string? ROLUSUARIO { get; set; }
    }

}
