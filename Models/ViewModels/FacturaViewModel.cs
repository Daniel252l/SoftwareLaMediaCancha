using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace LaMediaCancha.Models.ViewModels
{
    public class FacturaViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime FechaEmision { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteDocumento { get; set; }
        public string ClienteTelefono { get; set; }
        public string TipoPago { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public string MotivoAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public int? NotaCreditoId { get; set; }
        public string NumeroNotaCredito { get; set; }
        public List<DetalleFacturaViewModel> Detalles { get; set; }
    }

    public class DetalleFacturaViewModel
    {
        public int ProductoId { get; set; }
        public string ProductoCodigo { get; set; }
        public string ProductoNombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class BuscarFacturaViewModel
    {
        public string NumeroFactura { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
        public List<SelectListItem> Estados { get; set; }
    }

    public class AnularFacturaViewModel
    {
        public int FacturaId { get; set; }
        public string NumeroFactura { get; set; }
        public string ClienteNombre { get; set; }
        public decimal Total { get; set; }
        public string MotivoAnulacion { get; set; }
        public List<SelectListItem> MotivosAnulacion { get; set; }
    }
}