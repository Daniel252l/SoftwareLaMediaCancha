using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models.ViewModels
{
    public class BuscarVentaViewModel
    {
        public string NumeroFactura { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
    }
}