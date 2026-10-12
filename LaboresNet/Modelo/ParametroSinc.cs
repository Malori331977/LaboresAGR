using System.ComponentModel.DataAnnotations;
using SQLite;
namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class ParametroSinc
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string UltimaSincronización { get; set; }
    }
}
