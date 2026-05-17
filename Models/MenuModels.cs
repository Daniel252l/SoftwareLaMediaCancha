using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class MenuModels
    {
        public class ProductoMenu
        {
            public int ProductoId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioVenta { get; set; }
            public bool Activo { get; set; }
            public string Departamento { get; set; }
            public string SubDepartamento { get; set; }
            public string Presentacion { get; set; }
            public bool EnOferta { get; set; }
            public decimal? DescuentoPorcentaje { get; set; }
            public decimal? PrecioOferta { get; set; }
            public DateTime? FechaInicioOferta { get; set; }
            public DateTime? FechaFinOferta { get; set; }
            public string TipoItem { get; set; } // "Producto" o "Combo"
            public int DiasRestantes => FechaFinOferta.HasValue ? (int)(FechaFinOferta.Value.Date - DateTime.Now.Date).TotalDays : 0;

            // Propiedades específicas para combos
            public List<ComboDetalleMenu> ProductosCombo { get; set; }
        }

        public class ComboDetalleMenu
        {
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public int CantidadIncluida { get; set; }
            public decimal PrecioIndividual { get; set; }
        }
    }
}