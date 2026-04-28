using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class FacturaModels
    {
        public class EncabezadoFactura
        {
            public int FacturaId { get; set; }
            public string NumeroFactura { get; set; }
            public string NumeroDocumento { get; set; }
            public DateTime FechaEmision { get; set; }
            public int ClienteId { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteDocumento { get; set; }
            public string ClienteTelefono { get; set; }
            public string TipoPago { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Descuento { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }  // Vigente, Anulada
            public string Observaciones { get; set; }
            public DateTime FechaCreacion { get; set; }
            public DateTime? FechaAnulacion { get; set; }
            public string MotivoAnulacion { get; set; }
            public int? NotaCreditoId { get; set; }
            public string UsuarioAnulacion { get; set; }
            public List<DetalleFactura> Detalles { get; set; }
        }

        public class DetalleFactura
        {
            public int DetalleFacturaId { get; set; }
            public int FacturaId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoCodigo { get; set; }
            public string ProductoNombre { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class NotaCredito
        {
            public int NotaCreditoId { get; set; }
            public int FacturaOriginalId { get; set; }
            public string NumeroNotaCredito { get; set; }
            public DateTime FechaEmision { get; set; }
            public decimal MontoTotal { get; set; }
            public string Motivo { get; set; }
            public string Estado { get; set; }
            public DateTime FechaCreacion { get; set; }
            public int UsuarioId { get; set; }
            public string UsuarioNombre { get; set; }
        }

        public class AnularFacturaRequest
        {
            public int FacturaId { get; set; }
            public string MotivoAnulacion { get; set; }
            public int UsuarioId { get; set; }
        }
    }
}