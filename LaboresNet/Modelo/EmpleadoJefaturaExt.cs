
using com.gsitcr.geotime.Models;

namespace PortalEmpleadoGeotime.Client.Modelo
{
	public class EmpleadoJefaturaExt:cEmpleadoJefatura
	{
		public string? NombreEmpleado {  get; set; }
		public string? NombreResponsable { get; set; }
		public string? NombreSuplente{ get; set; }
		public string? Departamento { get; set; }
		public char? Activo { get; set; }
		public string? Grupo { get; set; }

	}
}
