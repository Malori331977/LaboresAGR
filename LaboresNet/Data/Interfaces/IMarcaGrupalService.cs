using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Response;

namespace PortalEmpleadoGeotime.Client.Data.Interfaces
{
    public record UbicacionSesion(string? Latitud, string? Longitud, string? DirIp);

    public interface IMarcaGrupalService
    {
        /// <summary>Registra una marca grupal en línea; ante falla técnica aplica las reglas localmente y la deja pendiente.</summary>
        Task<cMarcaGrupalResultado> Registrar(cEmpleado empleado, int tipo, UbicacionSesion ubicacion, byte[]? foto);

        /// <summary>Envía a /MarcaGrupal las marcas grupales pendientes. Las rechazadas quedan con estado 'R'.</summary>
        Task<(int Enviadas, int Rechazadas)> SincronizarPendientes();

        /// <summary>Causa de la ultima falla tecnica contra /MarcaGrupal (null si la ultima llamada fue bien).</summary>
        string? UltimoError { get; }

        /// <summary>Hora actual en la zona horaria de la compañía (misma que usan las marcas grupales).</summary>
        DateTime Ahora();
    }
}
