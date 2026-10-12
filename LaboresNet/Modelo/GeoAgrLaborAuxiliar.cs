using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PortalEmpleadoGeotime.Client.Modelo
{

    [Table("GEO_AGR_LABOR_AUXILIAR")]
    public class GeoAgrLaborAuxiliar
    {

        [Column("COMPANIA")]
        public string Compania { get; set; }

        [Column("ID_EMPLEADO")]
        public string IdEmpleado { get; set; }

        [Column("FECHA")]
        public DateTime Fecha { get; set; }

        [Column("COMENTARIOS")]
        public string? Comentarios { get; set; }

        [Column("ESTADOID")]
        public int EstadoId { get; set; }

        public long IdRegistro { get; set; } = 0;

        public IEnumerable<GeoAgrLaborAuxiliarDet>? GeoAgrLaborAuxiliarDet { get; set; }

    }


}
