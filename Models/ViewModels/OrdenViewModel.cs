using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models.ViewModels
{
    public class OrdenViewModel
    {
        public int MesaId { get; set; }
        public int NumeroMesa { get; set; }
        public string Ubicacion { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteTelefono { get; set; }
        public string Observaciones { get; set; }
        public List<OrdenProductoViewModel> Productos { get; set; }
    }

    public class OrdenProductoViewModel
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Nota { get; set; }
    }

    public class OrdenCuentaViewModel
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public int MesaId { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Observaciones { get; set; }
        public List<OrdenDetalleViewModel> Detalles { get; set; }
    }

    public class OrdenDetalleViewModel
    {
        public string ProductoNombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string Nota { get; set; }
    }
        public class OrdenTomaPedidoViewModel
        {
            public int MesaId { get; set; }
            public int NumeroMesa { get; set; }
            public string Ubicacion { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public string Observaciones { get; set; }
            public List<OrdenProductoViewModel> Productos { get; set; }
        }
    
}