using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models
{
    public class DevolucionModels
    {
        public class EncabezadoDevolucion
        {
            public int DevolucionId { get; set; }
            public int CompraId { get; set; }
            public string NumeroDocCompra { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoNombre { get; set; }  // ← Agregar esta propiedad
            public DateTime FechaCompraRef { get; set; }
            public bool TeniaProductosEnOferta { get; set; }
            public DateTime FechaDevolucion { get; set; }
            public string Motivo { get; set; }
            public string TipoDevolucion { get; set; }
            public decimal MontoTotal { get; set; }
            public string Estado { get; set; }
            public string Observaciones { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public DateTime? FechaModificacion { get; set; }
            public List<DetalleDevolucion> Detalles { get; set; }  // ← Agregar esta propiedad
        }

        public class DetalleDevolucion
        {
            public int DetalleDevolucionId { get; set; }
            public int DevolucionId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }  // ← Agregar esta propiedad
            public decimal Cantidad { get; set; }
            public decimal PrecioReferencia { get; set; }
            public decimal Subtotal { get; set; }
            public string MotivoDetalle { get; set; }
            public bool EstabaEnOferta { get; set; }
            public decimal? PrecioOfertaRef { get; set; }
        }

        public class ProductoDevolucion
        {
            public int ProductoId { get; set; }
            public string NombreProducto { get; set; }
            public string CodigoProducto { get; set; }
            public decimal CantidadComprada { get; set; }
            public decimal CantidadYaDevuelta { get; set; }
            public decimal CantidadDisponible => CantidadComprada - CantidadYaDevuelta;
            public decimal CantidadADevolver { get; set; }
            public decimal PrecioUnitario { get; set; }
            public bool EstaEnOferta { get; set; }
            public decimal? PrecioOferta { get; set; }
            public string Presentacion { get; set; }
        }

        public class RegistrarDevolucionRequest
        {
            public int CompraId { get; set; }
            public int EmpleadoId { get; set; }
            public string Motivo { get; set; }
            public string TipoDevolucion { get; set; }
            public string Observaciones { get; set; }
            public List<ProductoDevolucionItem> Productos { get; set; }
        }

        public class ProductoDevolucionItem
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
        }
    }
}
