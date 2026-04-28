using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace LaMediaCancha.Models.ViewModels
{
    public class RegistrarCompraViewModel
    {
        public int ProveedorId { get; set; }
        public int TipoCompraId { get; set; }
        public int TipoPagoId { get; set; }
        public string NumeroDocumento { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Observaciones { get; set; }
        public decimal CostoEnvio { get; set; }  // ← NUEVO: Costo de envío


        public List<SelectListItem> Proveedores { get; set; }
        public List<SelectListItem> TiposCompra { get; set; }
        public List<SelectListItem> TiposPago { get; set; }
        public List<ProductoCompraItem> Productos { get; set; }
    }

    public class ProductoCompraItem
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
    }
}