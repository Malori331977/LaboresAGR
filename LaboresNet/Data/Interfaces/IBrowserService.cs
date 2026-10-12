
using PortalEmpleadoGeotime.Client.Modelo.Utils;

namespace PortalEmpleadoGeotime.Client.Data.Interfaces
{
    public interface IBrowserService
    {
        public Task<BrowserDimension> GetDimensions();
    }
}
