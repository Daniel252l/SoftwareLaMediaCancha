using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class RecetaModels
    {
        public class Receta
        {
            public int RecetaId { get; set; }
            public int ProductoTerminadoId { get; set; }
            public string ProductoTerminadoNombre { get; set; }
            public string NombreReceta { get; set; }
            public decimal Rendimiento { get; set; }
            public string Instrucciones { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public List<RecetaDetalle> Detalles { get; set; }
        }

        public class RecetaDetalle
        {
            public int RecetaDetalleId { get; set; }
            public int RecetaId { get; set; }
            public int ProductoCompraId { get; set; }
            public string ProductoCompraNombre { get; set; }
            public decimal CantidadNecesaria { get; set; }
            public int UnidadMedidaId { get; set; }
            public string UnidadMedidaNombre { get; set; }
            public string UnidadMedidaAbreviatura { get; set; }
            public int StockDisponible { get; set; }
            public bool StockSuficiente { get; set; }
        }

        public class VerificacionStockViewModel
        {
            public int ProductoId { get; set; }
            public string ProductoNombre { get; set; }
            public decimal CantidadSolicitada { get; set; }
            public bool HayStock { get; set; }
            public string Mensaje { get; set; }
            public bool EsProductoSimple { get; set; }
            public List<RecetaDetalleVerificacion> Detalles { get; set; }
        }

        public class RecetaDetalleVerificacion
        {
            public int ProductoCompraId { get; set; }
            public string ProductoCompraNombre { get; set; }
            public decimal CantidadNecesaria { get; set; }
            public decimal CantidadTotal { get; set; }
            public string UnidadMedida { get; set; }
            public int StockDisponible { get; set; }
            public bool Suficiente { get; set; }
            public List<LoteSeleccionado> LotesUtilizados { get; set; }
        }

        public class LoteSeleccionado
        {
            public int LoteId { get; set; }
            public string CodigoLote { get; set; }
            public DateTime FechaVencimiento { get; set; }
            public int CantidadUsada { get; set; }
            public int CantidadDisponible { get; set; }
            public string NumeroLote { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
        }
    }
}