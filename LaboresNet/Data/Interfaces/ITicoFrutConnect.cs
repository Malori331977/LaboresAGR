using com.gsitcr.geotime.Models.Response;
using PortalEmpleadoGeotime.Client.Modelo;
using TicoFrutLibs.Models;

namespace LaboresNet.Data.Interfaces
{
    public interface ITicoFrutConnect
    {
        public Task<EventResponse> Post(string apiName, object model);
        public Task<EventResponse> Put(string apiName, object model);
        public Task<EventResponse> Delete(string apiName, string id);
        public Task<EventResponse> Delete(string apiName, string id, string nivel);
        public Task<bool> ApiDisponible();

        /// <summary>
        /// GetGeoAgrFinca: Obtiene la lista completa de fincas
        /// </summary>
        Task<IEnumerable<GeoAgrFinca>> GetGeoAgrFinca();

        /// <summary>
        /// GetGeoAgrFinca: Obtiene la lista de fincas filtrada por compañía y país
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="pais">Código del país</param>
        Task<IEnumerable<GeoAgrFinca>> GetGeoAgrFinca(string compania, string pais);

        /// <summary>
        /// GetGeoAgrFinca: Obtiene una finca específica
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="pais">Código del país</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        Task<GeoAgrFinca?> GetGeoAgrFinca(string compania, string pais, string bloque, string finca);

        /// <summary>
        /// GetGeoAgrLote: Obtiene la lista completa de lotes
        /// </summary>
        Task<IEnumerable<GeoAgrLote>> GetGeoAgrLote();

        /// <summary>
        /// GetGeoAgrLote: Obtiene la lista de lotes filtrada por compañía, bloque y finca
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        Task<IEnumerable<GeoAgrLote>> GetGeoAgrLote(string compania, string bloque, string finca);

        /// <summary>
        /// GetGeoAgrLote: Obtiene un lote específico
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        /// <param name="lote">Código del lote</param>
        Task<GeoAgrLote?> GetGeoAgrLote(string compania, string bloque, string finca, string lote);

        /// <summary>
        /// GetGeoAgrOt: Obtiene la lista completa de Órdenes de Trabajo
        /// </summary>
        Task<IEnumerable<GeoAgrOt>> GetGeoAgrOt();

        /// <summary>
        /// GetGeoAgrOt: Obtiene la lista de OT filtrada por compañía, bloque y finca
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        Task<IEnumerable<GeoAgrOt>> GetGeoAgrOt(string compania, string bloque, string finca);

        /// <summary>
        /// GetGeoAgrOt: Obtiene una Orden de Trabajo específica
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        /// <param name="ot">Código de la OT</param>
        Task<GeoAgrOt?> GetGeoAgrOt(string compania, string bloque, string finca, string ot);

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista completa de labores de OT
        /// </summary>
        Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor();

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista de labores filtrada por compañía, bloque y finca
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor(string compania, string bloque, string finca);

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene la lista de labores filtrada por compañía, bloque, finca y OT
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        /// <param name="ot">Código de la OT</param>
        Task<IEnumerable<GeoAgrOtLabor>> GetGeoAgrOtLabor(string compania, string bloque, string finca, string ot);

        /// <summary>
        /// GetGeoAgrOtLabor: Obtiene una labor de OT específica
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="bloque">Código del bloque</param>
        /// <param name="finca">Código de la finca</param>
        /// <param name="ot">Código de la OT</param>
        /// <param name="labor">Código de la labor</param>
        Task<GeoAgrOtLabor?> GetGeoAgrOtLabor(string compania, string bloque, string finca, string ot, string labor);

        /// <summary>
        /// GetGeoAgrRecursoConcepto: Obtiene la lista completa de recursos/conceptos
        /// </summary>
        Task<IEnumerable<GeoAgrRecursoConcepto>> GetGeoAgrRecursoConcepto();

        /// <summary>
        /// GetGeoAgrRecursoConcepto: Obtiene un recurso/concepto específico
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="recurso">Código del recurso</param>
        Task<GeoAgrRecursoConcepto?> GetGeoAgrRecursoConcepto(string compania, string recurso);

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene la lista completa de labores auxiliares
        /// </summary>
        Task<IEnumerable<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliar();

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene la lista de labores auxiliares por compañía y empleado
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="idEmpleado">Id del empleado</param>
        Task<IEnumerable<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliar(string compania, string idEmpleado);

        /// <summary>
        /// GetGeoAgrLaborAuxiliar: Obtiene el encabezado de labor auxiliar para un empleado y fecha específicos
        /// </summary>
        /// <param name="compania">Código de la compañía</param>
        /// <param name="idEmpleado">Id del empleado</param>
        /// <param name="fecha">Fecha de la labor</param>
        Task<GeoAgrLaborAuxiliar?> GetGeoAgrLaborAuxiliar(string compania, string idEmpleado, DateTime fecha);
    }
}
