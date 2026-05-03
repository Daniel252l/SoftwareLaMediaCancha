using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models
{
    public class OrdenModels
    {
        public class Mesa
        {
            public int MesaId { get; set; }
            public int NumeroMesa { get; set; }
            public int Capacidad { get; set; }
            public string Ubicacion { get; set; }
            public string Estado { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
        }

        public class Orden
        {
            public int OrdenId { get; set; }
            public string NumeroOrden { get; set; }
            public int MesaId { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public DateTime FechaApertura { get; set; }
            public DateTime? FechaCierre { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Descuento { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }
            public string Observaciones { get; set; }
            public int UsuarioId { get; set; }
            public string UsuarioNombre { get; set; }
            public List<DetalleOrden> Detalles { get; set; }
        }

        public class DetalleOrden
        {
            public int DetalleOrdenId { get; set; }
            public int OrdenId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoCodigo { get; set; }
            public string ProductoNombre { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal Subtotal { get; set; }
            public string Nota { get; set; }
        }
    }
}