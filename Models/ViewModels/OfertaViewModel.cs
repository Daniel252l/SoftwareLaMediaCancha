using System;

namespace LaMediaCancha.Models.ViewModels
{
    public class OfertaViewModel
    {
        public int OfertaId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public decimal PrecioOriginal { get; set; }
        public decimal PrecioOferta { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DiasRestantes { get; set; }
        public bool EsVigente
        {
            get
            {
                return DateTime.Now >= FechaInicio && DateTime.Now <= FechaFin;
            }
        }
    }
}