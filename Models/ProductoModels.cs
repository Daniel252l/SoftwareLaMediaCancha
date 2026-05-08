using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models
{
    public class ProductoModels
    {
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

        public class InventarioProducto  // ← Mover DENTRO de ProductoModels
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
    }
}