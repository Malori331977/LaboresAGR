using com.gsitcr.geotime.Models;

namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class MarcaDistribucionConceptoExt:cMarcaDistribucionConcepto
    {
        public string DescCentroCosto { get; set; } = "";
        public string DescProyecto { get; set; } = "";
        public string DescFase { get; set; } = "";
        public DateOnly FechaDO { get; set; }
        public TimeOnly HoraInicioTO { get; set; }
        public TimeOnly HoraFinTO { get; set; }
    }
}
