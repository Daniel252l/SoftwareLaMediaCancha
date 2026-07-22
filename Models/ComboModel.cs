using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LaMediaCancha.Models
{
    public class ComboModels
    {
        public class Combo
        {
            public int ComboId { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioCombo { get; set; }
            public decimal PrecioRegularTotal { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<ComboDetalle> Productos { get; set; }
        }

        public class ComboDetalle
        {
            public int ComboDetalleId { get; set; }
            public int ComboId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }
            public decimal PrecioIndividual { get; set; }
            public int CantidadIncluida { get; set; }
        }

        public class ComboViewModel
        {
            public int ComboId { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioCombo { get; set; }
            public decimal PrecioRegularTotal { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<ComboDetalleViewModel> Productos { get; set; }
            public decimal Ahorro => PrecioRegularTotal - PrecioCombo;
            public int TotalProductos { get; set; }
        }

        public class ComboDetalleViewModel
        {
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }
            public decimal PrecioIndividual { get; set; }
            public int CantidadIncluida { get; set; }
            public decimal Subtotal => CantidadIncluida * PrecioIndividual;
        }

        public class CrearComboViewModel
        {
            public int ComboId { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioCombo { get; set; }
            public bool Activo { get; set; }
            public List<ComboProductoItem> Productos { get; set; }
            public List<SelectListItem> ProductosDisponibles { get; set; }
        }

        public class ComboProductoItem
        {
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }
            public decimal PrecioVenta { get; set; }
            public int Cantidad { get; set; }
        }
    }
}