using BlazorBootstrap;


namespace PortalEmpleadoGeotime.Client.Data.Interfaces
{
    public interface IGridExtensions
    {
        public Task<IEnumerable<FilterOperatorInfo>> GridFiltersTranslationProvider();
    }
}
