using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaMediaCancha.Models.ViewModels
{
    public class ResumenEstadisticasViewModel
    {
        public int TotalOrdenes { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal PromedioVenta { get; set; }
        public int MesasAtendidas { get; set; }
        public int TotalProductosVendidos { get; set; }
    }
}