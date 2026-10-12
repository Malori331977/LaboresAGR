
namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class Autorizante
    {
        public string IdNumero { get; set; }
        public string? Nombre { get; set; }
        public string IdNivelAutorizacion { get; set; }
        public string DescNivelAutorizacion { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
    }
}
