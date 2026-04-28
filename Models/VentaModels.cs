using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class VentaModels
    {
        public class Venta
        {
            public int VentaId { get; set; }
            public string NumeroFactura { get; set; }
            public DateTime FechaVenta { get; set; }
            public int? ClienteId { get; set; }
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
            public int UsuarioId { get; set; }
            public string UsuarioNombre { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<DetalleVenta> Detalles { get; set; }
        }

        public class DetalleVenta
        {
            public int DetalleVentaId { get; set; }
            public int VentaId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoCodigo { get; set; }
            public string ProductoNombre { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal Subtotal { get; set; }
            public List<DetalleVentaLote> LotesUtilizados { get; set; }
        }

        public class DetalleVentaLote
        {
            public int DetalleVentaLoteId { get; set; }
            public int DetalleVentaId { get; set; }
            public int LoteId { get; set; }
            public string NumeroLote { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class ProductoVenta
        {
            public int ProductoId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public decimal PrecioVenta { get; set; }
            public decimal StockDisponible { get; set; }
            public List<LoteDisponible> Lotes { get; set; }
        }

        public class LoteDisponible
        {
            public int LoteId { get; set; }
            public string NumeroLote { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public DateTime FechaIngreso { get; set; }
            public DateTime? FechaVencimiento { get; set; }
        }
    }
}