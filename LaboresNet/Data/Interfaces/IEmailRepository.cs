

using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Response;

namespace PortalEmpleadoGeotime.Client.Data.Interfaces
{
    public interface IEmailRepository
    {
        public Task<EventResponse> EnviaCorreoCambioPassword(cEmpleado empleado, string codigo);
    }
}
