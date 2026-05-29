using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class DevolucionModels
    {
        // ==================== ENCABEZADO DE DEVOLUCIÓN ====================
        public class EncabezadoDevolucion
        {
            public int DevolucionId { get; set; }
            public int? CompraId { get; set; }
            public int? OrdenId { get; set; }
            public int? VentaId { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoNombre { get; set; }
            public int? ProveedorId { get; set; }
            public string ProveedorNombre { get; set; }
            public string ClienteNombre { get; set; }
            public string NumeroDocCompra { get; set; }
            public string NumeroDevolucion { get; set; }
            public DateTime FechaCompraRef { get; set; }
            public DateTime FechaDevolucion { get; set; }
            public string Motivo { get; set; }
            public string TipoDevolucion { get; set; }
            public decimal MontoTotal { get; set; }
            public string Estado { get; set; }
            public string Observaciones { get; set; }
            public bool TeniaProductosEnOferta { get; set; }
            public string Tipo { get; set; } // "Proveedor" o "Cliente"
            public string FormaCompensacion { get; set; } // "Efectivo", "Tarjeta", "CreditoCasa", "NotaCredito"
            public string NumeroNotaCredito { get; set; }
            public int? AutorizadoPor { get; set; }
            public string AutorizadoPorNombre { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public DateTime? FechaModificacion { get; set; }
            public List<DetalleDevolucion> Detalles { get; set; }
        }

        // ==================== DETALLE DE DEVOLUCIÓN ====================
        public class DetalleDevolucion
        {
            public int DetalleDevolucionId { get; set; }
            public int DevolucionId { get; set; }
            public int? DetalleOrdenId { get; set; }
            public int? LoteCompraId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public string ProductoCodigo { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioReferencia { get; set; }
            public decimal Subtotal { get; set; }
            public string MotivoDetalle { get; set; }
            public bool EstabaEnOferta { get; set; }
            public decimal? PrecioOfertaRef { get; set; }
            public string Tipo { get; set; } // "Proveedor" o "Cliente"
            public string DestinoStock { get; set; } // "Merma" o "DevolucionStock"
            public bool Autorizado { get; set; }
        }

        // ==================== PRODUCTO PARA DEVOLUCIÓN (VIEW MODEL) ====================
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
            public int? LoteCompraId { get; set; }
            public string NumeroLote { get; set; }
        }

        // ==================== PRODUCTO PARA DEVOLUCIÓN DE CLIENTE ====================
        public class ProductoDevolucionCliente
        {
            public int DetalleOrdenId { get; set; }
            public int ProductoId { get; set; }
            public string ProductoCodigo { get; set; }
            public string ProductoNombre { get; set; }
            public decimal CantidadVendida { get; set; }
            public decimal CantidadYaDevuelta { get; set; }
            public decimal CantidadDisponible { get; set; }
            public decimal CantidadADevolver { get; set; }
            public decimal PrecioUnitario { get; set; }
            public bool EstabaEnOferta { get; set; }
            public decimal? PrecioOferta { get; set; }
            public bool RequiereAutorizacion { get; set; }
            public string DestinoStock { get; set; }
        }

        // ==================== SOLICITUD DE REGISTRO DE DEVOLUCIÓN ====================
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
            public int? LoteCompraId { get; set; }
        }

        // ==================== SOLICITUD DE REGISTRO DE DEVOLUCIÓN DE CLIENTE ====================
        public class RegistrarDevolucionClienteRequest
        {
            public int? OrdenId { get; set; }
            public int? VentaId { get; set; }
            public int EmpleadoId { get; set; }
            public int? AutorizadoPor { get; set; }
            public string Motivo { get; set; }
            public string TipoDevolucion { get; set; }
            public string FormaCompensacion { get; set; }
            public string Observaciones { get; set; }
            public List<DevolucionClienteItem> Productos { get; set; }
        }

        public class DevolucionClienteItem
        {
            public int DetalleOrdenId { get; set; }
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public string DestinoStock { get; set; }
            public bool Autorizado { get; set; }
        }

        // ==================== AUTORIZACIÓN ====================
        public class AutorizacionRequest
        {
            public int DevolucionId { get; set; }
            public int EmpleadoId { get; set; }
            public string Motivo { get; set; }
        }
    }
}