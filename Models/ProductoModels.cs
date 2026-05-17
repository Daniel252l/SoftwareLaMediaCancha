using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
            public int? EstanteId { get; set; }
            public int? ColorId { get; set; }
            public int? TallaId { get; set; }
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
        }

        // ==================== INVENTARIO DE VENTA ====================
        public class InventarioProducto
        {
            public int ProductoId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Departamento { get; set; }
            public string SubDepartamento { get; set; }
            public string Presentacion { get; set; }
            public int ExistenciaActual { get; set; }
            public int StockMinimo { get; set; }
            public int StockMaximo { get; set; }
            public decimal PorcentajeStock => StockMaximo > 0 ? (ExistenciaActual * 100m / StockMaximo) : 0;
            public string EstadoStock
            {
                get
                {
                    if (ExistenciaActual <= StockMinimo) return "CRÍTICO";
                    if (ExistenciaActual <= StockMaximo * 0.2m) return "BAJO";
                    if (ExistenciaActual >= StockMaximo * 0.8m) return "ALTO";
                    return "NORMAL";
                }
            }
            public string ColorEstado
            {
                get
                {
                    if (ExistenciaActual <= StockMinimo) return "danger";
                    if (ExistenciaActual <= StockMaximo * 0.2m) return "warning";
                    if (ExistenciaActual >= StockMaximo * 0.8m) return "info";
                    return "success";
                }
            }
        }

        // ==================== PRODUCTOS DE COMPRA (MATERIAS PRIMAS) ====================
        public class ProductoCompra
        {
            public int ProductoCompraId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public string UnidadMedida { get; set; }
            public decimal PrecioCompra { get; set; }
            public decimal StockActual { get; set; }
            public decimal StockMinimo { get; set; }
            public int? CategoriaId { get; set; }
            public string Categoria { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<LoteCompra> Lotes { get; set; }  // ← Agregar esta propiedad
        }

        // ==================== LOTES DE COMPRA ====================
        public class LoteCompra
        {
            public int LoteCompraId { get; set; }
            public int ProductoCompraId { get; set; }
            public int ProveedorId { get; set; }
            public string NumeroLote { get; set; }
            public decimal CantidadInicial { get; set; }
            public decimal CantidadActual { get; set; }
            public decimal PrecioUnitario { get; set; }
            public DateTime FechaIngreso { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool Activo { get; set; }
            public string ProveedorNombre { get; set; }
            public string ProductoNombre { get; set; }
        }

        // ==================== INVENTARIO DE COMPRA ====================
        public class InventarioCompra
        {
            public int ProductoCompraId { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string UnidadMedida { get; set; }
            public decimal PrecioCompra { get; set; }
            public decimal StockActual { get; set; }
            public decimal StockMinimo { get; set; }
            public string Categoria { get; set; }
            public bool Activo { get; set; }
            public string EstadoStock { get; set; }
        }
    }
}