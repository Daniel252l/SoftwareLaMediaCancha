using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models
{
    public class CompraModels
    {
        public class EncabezadoCompra
        {
            public int CompraId { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoNombre { get; set; }
            public int ProveedorId { get; set; }
            public string ProveedorNombre { get; set; }
            public int TipoCompraId { get; set; }
            public string TipoCompraNombre { get; set; }
            public int TipoPagoId { get; set; }
            public string TipoPagoNombre { get; set; }
            public string NumeroDocumento { get; set; }
            public string NumeroFactura { get; set; }
            public DateTime FechaCompra { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public decimal Subtotal { get; set; }      // ← Asegurar que existe
            public decimal Impuesto { get; set; }      // ← Asegurar que existe
            public decimal Descuento { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }
            public string Observaciones { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public DateTime? FechaModificacion { get; set; }
            public List<DetalleCompra> Detalles { get; set; }
        }

        public class DetalleCompra
        {
            public int DetalleCompraId { get; set; }
            public int CompraId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }
            public decimal Cantidad { get; set; }
            public decimal CantidadDevuelta { get; set; }
            public decimal CantidadDisponible => Cantidad - CantidadDevuelta;
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal Subtotal { get; set; }
            public bool EstabaEnOferta { get; set; }
            public decimal? PrecioOferta { get; set; }
        }

        public class RegistrarCompraRequest
        {
            public int EmpleadoId { get; set; }
            public int ProveedorId { get; set; }
            public int TipoCompraId { get; set; }
            public int TipoPagoId { get; set; }
            public string NumeroDocumento { get; set; }
            public string NumeroFactura { get; set; }
            public DateTime? FechaCompra { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public string Observaciones { get; set; }
            public List<DetalleCompraItem> Detalles { get; set; }
        }

        public class DetalleCompraItem
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
        }
    }
}