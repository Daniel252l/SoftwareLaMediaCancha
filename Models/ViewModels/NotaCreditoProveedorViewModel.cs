using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models.ViewModels
{
    public class NotaCreditoProveedorViewModel
    {
        public int NotaCreditoProveedorId { get; set; }
        public int CompraId { get; set; }
        public string NumeroNCProveedor { get; set; }
        public DateTime FechaEmision { get; set; }
        public decimal MontoTotal { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string DocumentoRuta { get; set; }
        public string DocumentoNombre { get; set; }
        public string UsuarioNombre { get; set; }
        public string NumeroFacturaCompra { get; set; }
    }
}