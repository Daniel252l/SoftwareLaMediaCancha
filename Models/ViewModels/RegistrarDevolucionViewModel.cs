using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LaMediaCancha.Models;

namespace LaMediaCancha.Models.ViewModels
{
    public class RegistrarDevolucionViewModel
    {
        public int CompraId { get; set; }
        public int OrdenId { get; set; }
        public string NumeroDocumento { get; set; }
        public string NumeroFactura { get; set; }
        public string NumeroFacturaBuscado { get; set; }
        public DateTime FechaCompra { get; set; }
        public string ProveedorNombre { get; set; }
        public string ClienteNombre { get; set; }
        public int DiasMaximos { get; set; }
        public int DiasTranscurridos { get; set; }
        public string Motivo { get; set; }
        public string TipoDevolucion { get; set; }
        public string Observaciones { get; set; }
        public string Tipo { get; set; } // "Proveedor" o "Cliente"
        public string FormaCompensacion { get; set; } // "Efectivo", "Tarjeta", "CreditoCasa", "NotaCredito"
        public List<DevolucionModels.ProductoDevolucion> Productos { get; set; }
        public List<DevolucionModels.ProductoDevolucionCliente> ProductosCliente { get; set; }

        public bool DentroDePlazo => DiasTranscurridos <= DiasMaximos;
        public string MensajePlazo => DentroDePlazo
            ? $"✅ Dentro del plazo. {DiasTranscurridos} de {DiasMaximos} días"
            : $"❌ Fuera de plazo. {DiasTranscurridos} de {DiasMaximos} días";

        public List<SelectListItem> TiposDevolucion => new List<SelectListItem>
        {
            new SelectListItem { Value = "ProductoDefectuoso", Text = "Producto Defectuoso" },
            new SelectListItem { Value = "ProductoSobrante", Text = "Producto Sobrante" },
            new SelectListItem { Value = "ErrorFacturacion", Text = "Error de Facturación" },
            new SelectListItem { Value = "Otro", Text = "Otro" }
        };

        public List<SelectListItem> FormasCompensacion => new List<SelectListItem>
        {
            new SelectListItem { Value = "Efectivo", Text = "Efectivo" },
            new SelectListItem { Value = "Tarjeta", Text = "Tarjeta de Crédito/Débito" },
            new SelectListItem { Value = "CreditoCasa", Text = "Crédito en Casa" },
            new SelectListItem { Value = "NotaCredito", Text = "Nota de Crédito Fiscal" }
        };

        public List<SelectListItem> DestinosStock => new List<SelectListItem>
        {
            new SelectListItem { Value = "Merma", Text = "Merma/Desperdicio (No regresa al inventario)" },
            new SelectListItem { Value = "DevolucionStock", Text = "Devolución a Stock (Regresa al inventario)" }
        };
    }
}