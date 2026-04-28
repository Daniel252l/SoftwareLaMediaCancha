using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models.ViewModels
{
    public class VentaViewModel
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
        public string Observaciones { get; set; }
        public List<CarritoItem> Carrito { get; set; }
    }

    public class CarritoItem
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public List<LoteSeleccionado> LotesSeleccionados { get; set; }
    }

    public class LoteSeleccionado
    {
        public int LoteId { get; set; }
        public string NumeroLote { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}