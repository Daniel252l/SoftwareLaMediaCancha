using System;

namespace LaMediaCancha.Models
{
    public class LoteViewModel
    {
        public int LoteId { get; set; }
        public string NumeroLoteInterno { get; set; }
        public string NumeroLoteProveedor { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal CantidadInicial { get; set; }
        public decimal CantidadActual { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Estado { get; set; }
        public string NumeroCompra { get; set; }
        public string ProveedorNombre { get; set; }
        public int? DiasParaVencer { get; set; }

        // Propiedades adicionales para vista Próximos a Vencer
        public string ProductoNombre { get; set; }
        public string ProductoCodigo { get; set; }
    }
}