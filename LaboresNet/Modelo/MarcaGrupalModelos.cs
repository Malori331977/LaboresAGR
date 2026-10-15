using System.Text.Json.Serialization;
using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Response;
using com.gsitcr.geotime.Models.Utils;

namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class IdentificacionRostro
    {
        [JsonPropertyName("idnumero")] public string? IdNumero { get; set; }
        [JsonPropertyName("distancia")] public double? Distancia { get; set; }
        [JsonPropertyName("sinRostro")] public bool SinRostro { get; set; }
    }

    public class PruebaVida
    {
        [JsonPropertyName("ok")] public bool Ok { get; set; }
        [JsonPropertyName("motivo")] public string? Motivo { get; set; }
        [JsonPropertyName("foto")] public string? Foto { get; set; }
    }

    public record RegistroSesion(cEmpleado Empleado, string Codigo, string Mensaje, string? Advertencia, string Hora)
    {
        public bool EsOk => Codigo == CodigoMarcaGrupal.OK;
    }

    public class ResumenSesionGrupal
    {
        public int TipoMarca { get; init; }
        public DateTime Inicio { get; init; }
        public DateTime Fin { get; init; }
        public List<cEmpleado> Esperados { get; init; } = new();
        public List<RegistroSesion> Registrados { get; init; } = new();

        public List<cEmpleado> Pendientes =>
            Esperados.Where(e => !Registrados.Any(r => r.Empleado.IdNumero == e.IdNumero)).ToList();
    }

    public class FilaColaborador
    {
        public required cEmpleado Empleado { get; init; }
        public EstadoColaborador Estado { get; init; }
        public string Detalle { get; init; } = "";
        /// <summary>Cantidad de plantillas faciales (TemplateFACE) registradas para el empleado.</summary>
        public int Plantillas { get; init; }
        public bool TieneRostro => Plantillas > 0;
    }
}
