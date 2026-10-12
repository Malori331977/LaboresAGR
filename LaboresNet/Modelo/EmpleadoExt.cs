using com.gsitcr.geotime.Models;

namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class EmpleadoExt : cEmpleado
    {
        public string? DescDepartamento { get; set; }
        public string? DescCentroCosto { get; set; }
        public string? DescPlanilla { get; set; }
        public string? DescGrupo { get; set; }
        public bool ExportarBool { get; set; }
        public bool EstadoBool { get; set; }
        public DateOnly InicioRolDO { get; set; }
        public bool TieneRostro { get; set;}
        public int CantPlantillasRegistradas { get; set; }

        public string entrada { get; set; } = "";
        public string salida { get; set; } = "";
        public string descanso { get; set; } = "";
        public string Filtro { get; set; } = "";
    }
}
