

using Microsoft.JSInterop;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Modelo.Utils;


namespace PortalEmpleadoGeotime.Client.Data
{
    public class BrowserService : IBrowserService
    {
        private readonly IJSRuntime _js;

        public BrowserService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<BrowserDimension> GetDimensions()
        {
            return await _js.InvokeAsync<BrowserDimension>("getDimensions");
        }
    }
}
