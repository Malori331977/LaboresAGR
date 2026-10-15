using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;


namespace PortalEmpleadoGeotime.Client.Modelo
{

    [Table("GEO_AGR_LABOR_AUXILIAR_DET")]
    public class GeoAgrLaborAuxiliarDet
    {
        [Column("ID_EMPLEADO")]
        public string IdEmpleado { get; set; }

        [Column("FECHA")]
        public DateTime Fecha { get; set; }

        [Column("ITEM")]
        public int Item { get; set; }

        [Column("BLOQUE")]
        public string? Bloque { get; set; }

        [Column("FINCA")]
        public string? Finca { get; set; }

        [Column("LOTE")]
        public string? Lote { get; set; }

        [Column("OT")]
        public string? Ot { get; set; }

        [Column("LABOR")]
        public string? Labor { get; set; }

        [Column("RECURSO")]
        public string? Recurso { get; set; }

        [Column("CANTIDAD_RENDIMIENTO")]
        public decimal? CantidadRendimiento { get; set; }

        [Column("UNIDAD")]
        public string? Unidad { get; set; }

        [Column("HORAS_APROBADAS")]
        public decimal? HorasAprobadas { get; set; }

        [Column("ESTADOID")]
        public int EstadoId { get; set; } = 0;

        /// <summary>Empleado al que se asignó este ítem (uno por cada empleado seleccionado al crear la labor). Sin columna en Oracle.</summary>
        public string? IdEmpleadoAsignado { get; set; }

        public long IdRegistro { get; set; } = 0;

        public GeoAgrLaborAuxiliar? GeoAgrLaborAuxiliar { get; set; }

    }


}
