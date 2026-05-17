using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models.ViewModels
{
    public class ReporteFiltrosViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string TipoReporte { get; set; }
        public string FormaPago { get; set; }
        public int? ProveedorId { get; set; }
        public int? ProductoId { get; set; }
        public int? MesaId { get; set; }
        public int? UsuarioId { get; set; }
    }

    public class ReporteVentaViewModel
    {
        public int VentaId { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaVenta { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteDocumento { get; set; }
        public string TipoPago { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string UsuarioNombre { get; set; }
    }

    public class ReporteVentaDetalleViewModel
    {
        public int VentaId { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaVenta { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteDocumento { get; set; }
        public string ClienteTelefono { get; set; }
        public string TipoPago { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public List<DetalleVentaReporte> Detalles { get; set; }
    }

    public class DetalleVentaReporte
    {
        public string ProductoNombre { get; set; }
        public string ProductoCodigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ReporteProductoVendidoViewModel
    {
        public string Codigo { get; set; }
        public string ProductoNombre { get; set; }
        public decimal CantidadVendida { get; set; }
        public decimal TotalVenta { get; set; }
        public int NumeroVentas { get; set; }
        public decimal PorcentajeParticipacion { get; set; }
    }
}