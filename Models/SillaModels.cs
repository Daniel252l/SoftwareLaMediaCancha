using System;
using System.Collections.Generic;
using static LaMediaCancha.Models.OrdenModels;

namespace LaMediaCancha.Models
{
    public class SillaModels
    {
        public class Silla
        {
            public int SillaId { get; set; }
            public int MesaId { get; set; }
            public int NumeroSilla { get; set; }
            public string Estado { get; set; }
            public bool Activo { get; set; }
            public decimal Total { get; set; }
            public string NombreCliente { get; set; }
            public int? OrdenPersonaId { get; set; }
            public string ItemsPreview { get; set; }
        }

        public class MesaConSillasViewModel
        {
            public int MesaId { get; set; }
            public int NumeroMesa { get; set; }
            public int Capacidad { get; set; }
            public string Ubicacion { get; set; }
            public string Estado { get; set; }
            public List<Silla> Sillas { get; set; }
            public Orden OrdenActiva { get; set; }
            public decimal TotalMonto { get; set; }
        }

        public class SillaOrdenViewModel
        {
            public int SillaId { get; set; }
            public int NumeroSilla { get; set; }
            public string Estado { get; set; }
            public int? OrdenPersonaId { get; set; }
            public string NombreCliente { get; set; }
            public decimal Total { get; set; }
            public List<DetalleOrdenPersona> Detalles { get; set; }
        }
    }
}