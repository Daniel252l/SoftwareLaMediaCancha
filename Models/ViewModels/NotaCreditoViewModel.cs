using System;

namespace LaMediaCancha.Models.ViewModels
{
    public class NotaCreditoViewModel
    {
        public int NotaCreditoId { get; set; }
        public string NumeroNotaCredito { get; set; }
        public DateTime FechaEmision { get; set; }
        public decimal MontoTotal { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string UsuarioNombre { get; set; }
        public string FacturaOriginalNumero { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteDocumento { get; set; }
    }
}