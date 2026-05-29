using System;
using System.Collections.Generic;

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
            public List<Silla> Sillas { get; set; }
            public decimal TotalMonto { get; set; }
        }

        public class Orden
        {
            public int OrdenId { get; set; }
            public string NumeroOrden { get; set; }
            public int MesaId { get; set; }
            public int NumeroMesa { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public DateTime FechaApertura { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }
            public string Observaciones { get; set; }
            public int UsuarioId { get; set; }
            public string UsuarioNombre { get; set; }
            public int? OrdenPersonaId { get; set; }
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
            public decimal Subtotal { get; set; }
            public string Nota { get; set; }
            public bool EsDeCombo { get; set; }
            public int? ComboId { get; set; }
        }

        public class DetalleOrdenPersona
        {
            public int DetalleOrdenPersonaId { get; set; }
            public int OrdenPersonaId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public bool EsDeCombo { get; set; }
            public int? ComboId { get; set; }
            public string Nota { get; set; }
        }

        public class OrdenPersona
        {
            public int OrdenPersonaId { get; set; }
            public int OrdenId { get; set; }
            public int? SillaId { get; set; }
            public int NumeroSilla { get; set; }
            public string NombreCliente { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Total { get; set; }
            public bool Pagado { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<DetalleOrdenPersona> Detalles { get; set; }
        }

        public class Combo
        {
            public int ComboId { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioCombo { get; set; }
            public decimal PrecioRegularTotal { get; set; }
            public List<ComboDetalle> Productos { get; set; }
        }

        public class ComboDetalle
        {
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public decimal PrecioIndividual { get; set; }
            public int CantidadIncluida { get; set; }
        }

        public class Silla
        {
            public int SillaId { get; set; }
            public int MesaId { get; set; }
            public int NumeroSilla { get; set; }
            public string Estado { get; set; }
            public bool Activo { get; set; }
            public decimal Total { get; set; }
            public string NombreCliente { get; set; }
            public int? OrdenPersonaId { get; set; }
            public bool EsTemporal { get; set; }
        }
    }
}