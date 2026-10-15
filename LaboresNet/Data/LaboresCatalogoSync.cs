using com.gsitcr.geotime.Models;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using PortalEmpleadoGeotime.Client.Data.Interfaces;

namespace PortalEmpleadoGeotime.Client.Data
{
    /// <summary>
    /// Descarga los catálogos de labores (Ph_Labores*) de la API de Geotime y los guarda en la base local.
    /// Es todo o nada: si algún catálogo no responde, no se modifica nada local.
    /// </summary>
    public static class LaboresCatalogoSync
    {
        /// <returns>true si descargó y guardó los 7 catálogos; false si alguno falló.</returns>
        public static async Task<bool> SincronizarAsync(IGeoTimeConnect geo, MarcasDBLocal db)
        {
            var tBloques = geo.GetLaboresBloques();
            var tFincas = geo.GetLaboresFincas();
            var tLotes = geo.GetLaboresLotes();
            var tOrdenes = geo.GetLaboresOrdenesTrabajo();
            var tActividades = geo.GetLaboresActividades();
            var tUnidades = geo.GetLaboresUnidades();
            var tRecursos = geo.GetLaboresRecursos();

            await Task.WhenAll(tBloques, tFincas, tLotes, tOrdenes, tActividades, tUnidades, tRecursos);

            var bloques = tBloques.Result;
            var fincas = tFincas.Result;
            var lotes = tLotes.Result;
            var ordenes = tOrdenes.Result;
            var actividades = tActividades.Result;
            var unidades = tUnidades.Result;
            var recursos = tRecursos.Result;

            // null = la API falló; una lista vacía es una respuesta válida.
            if (bloques is null || fincas is null || lotes is null || ordenes is null
                || actividades is null || unidades is null || recursos is null)
                return false;

            await db.SaveLaboresCatalogosAsync(bloques, fincas, lotes, ordenes, actividades, unidades, recursos);
            return true;
        }
    }
}
