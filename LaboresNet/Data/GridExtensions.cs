using BlazorBootstrap;
using PortalEmpleadoGeotime.Client.Data.Interfaces;


namespace PortalEmpleadoGeotime.Client.Data
{
    public class GridExtensions:IGridExtensions
    {
        public async Task<IEnumerable<FilterOperatorInfo>> GridFiltersTranslationProvider()
        {
            var filtersTranslation = new List<FilterOperatorInfo>();

            // number/date/boolean
            filtersTranslation.Add(new("=", "Igual que", FilterOperator.Equals));
            filtersTranslation.Add(new("!=", "Diferente de", FilterOperator.NotEquals));
            // number/date
            filtersTranslation.Add(new("<", "Menor que", FilterOperator.LessThan));
            filtersTranslation.Add(new("<=", "Menor o igual que", FilterOperator.LessThanOrEquals));
            filtersTranslation.Add(new(">", "Mayor que", FilterOperator.GreaterThan));
            filtersTranslation.Add(new(">=", "Mayor o igual que", FilterOperator.GreaterThanOrEquals));
            // string
            filtersTranslation.Add(new("*a*", "Contiene", FilterOperator.Contains));
            filtersTranslation.Add(new("a**", "Inicia con", FilterOperator.StartsWith));
            filtersTranslation.Add(new("**a", "Termina con", FilterOperator.EndsWith));
            filtersTranslation.Add(new("=", "Igual que", FilterOperator.Equals));
            // common
            filtersTranslation.Add(new("x", "Limpiar", FilterOperator.Clear));

            return await Task.FromResult(filtersTranslation);
        }
    }
}
