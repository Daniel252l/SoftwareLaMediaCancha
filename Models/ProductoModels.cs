using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class ProductoModels
    {
        // ==================== PRODUCTOS DE VENTA ====================
        public class Producto
        {
            public int ProductoId { get; set; }
            public int? SubDepartamentoId { get; set; }
            public int? PresentacionId { get; set; }
            public int? MarcaId { get; set; }
            public int? CategoriaProductoId { get; set; }
            public int? TipoProductoId { get; set; }
            public string Codigo { get; set; }
            public string CodigoBarras { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public decimal? PrecioCompra { get; set; }
            public decimal PrecioVenta { get; set; }
            public bool? EstaEnOferta { get; set; }
            public decimal? PrecioOferta { get; set; }
            public DateTime? FechaInicioOferta { get; set; }
            public DateTime? FechaFinOferta { get; set; }
            public bool Activo { get; set; }
            public DateTime? FechaCreacion { get; set; }
            public DateTime? FechaModificacion { get; set; }
            public List<LoteProductoVenta> Lotes { get; set; }
        }

        // ==================== PRODUCTOS DE COMPRA (MATERIAS PRIMAS) ====================
        // ==================== PRODUCTOS DE COMPRA (MATERIAS PRIMAS) ====================
        public class ProductoCompra
        {
            public int ProductoCompraId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public string UnidadMedida { get; set; }
            public decimal PrecioCompra { get; set; }
            public int StockActual { get; set; }      // ← Cambiado de decimal a int
            public int StockMinimo { get; set; }      // ← Cambiado de decimal a int
            public int? CategoriaId { get; set; }
            public string Categoria { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<LoteCompra> Lotes { get; set; }
            public int LotesActivos { get; set; }
        }
        // ==================== LOTES DE COMPRA ====================
        public class LoteCompra
        {
            public int LoteCompraId { get; set; }
            public int ProductoCompraId { get; set; }
            public int ProveedorId { get; set; }
            public string NumeroLote { get; set; }
            public int CantidadInicial { get; set; }   // ← Cambiado de decimal a int
            public int CantidadActual { get; set; }    // ← Cambiado de decimal a int
            public decimal PrecioUnitario { get; set; }
            public decimal CostoCompra { get; set; }
            public DateTime FechaIngreso { get; set; }
            public DateTime? FechaFabricacion { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool Activo { get; set; }
            public string ProveedorNombre { get; set; }
            public string ProductoNombre { get; set; }
            public bool EstaVencido { get; set; }
            public int DiasRestantes => FechaVencimiento.HasValue ? (int)(FechaVencimiento.Value.Date - DateTime.Now.Date).TotalDays : 999;
        }

        // ==================== LOTES DE PRODUCTOS DE VENTA ====================
        public class LoteProductoVenta
        {
            public int LoteProductoVentaId { get; set; }
            public int ProductoId { get; set; }
            public int ProveedorId { get; set; }
            public string NumeroLote { get; set; }
            public int CantidadInicial { get; set; }
            public int CantidadActual { get; set; }
            public decimal PrecioCompra { get; set; }
            public decimal PrecioVenta { get; set; }
            public DateTime? FechaFabricacion { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public DateTime FechaIngreso { get; set; }
            public bool Activo { get; set; }
            public string ProveedorNombre { get; set; }
            public int DiasRestantes => FechaVencimiento.HasValue ? (int)(FechaVencimiento.Value.Date - DateTime.Now.Date).TotalDays : 999;
            public bool EstaVencido => FechaVencimiento.HasValue && FechaVencimiento.Value.Date < DateTime.Now.Date;
        }

        // ==================== INVENTARIO DE COMPRA ====================
        public class InventarioCompra
        {
            public int ProductoCompraId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string UnidadMedida { get; set; }
            public decimal PrecioCompra { get; set; }
            public int StockActual { get; set; }
            public int StockMinimo { get; set; }
            public string Categoria { get; set; }
            public bool Activo { get; set; }
            public string EstadoStock { get; set; }
            public int LotesActivos { get; set; }
        }

        // ==================== INVENTARIO GENERAL ====================
        public class InventarioGeneral
        {
            public int Id { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string UnidadMedida { get; set; }
            public decimal PrecioCompra { get; set; }
            public int StockActual { get; set; }
            public int StockMinimoAlerta { get; set; }
            public string Categoria { get; set; }
            public string TipoProducto { get; set; }
            public bool Activo { get; set; }
            public int LotesActivos { get; set; }
            public string EstadoStock => StockActual <= StockMinimoAlerta ? "Bajo" :
                                         StockActual <= StockMinimoAlerta * 1.5m ? "Alerta" : "Normal";
        }

        // ==================== MOVIMIENTOS DE INVENTARIO ====================
        public class MovimientoInventario
        {
            public int MovimientoId { get; set; }
            public int ProductoCompraId { get; set; }
            public string ProductoNombre { get; set; }
            public int? LoteCompraId { get; set; }
            public string NumeroLote { get; set; }
            public string TipoMovimiento { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public string Motivo { get; set; }
            public int? ReferenciaId { get; set; }
            public DateTime FechaMovimiento { get; set; }
            public string UsuarioNombre { get; set; }
            public string ProveedorNombre { get; set; }
        }
    }
}