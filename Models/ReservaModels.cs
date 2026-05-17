using System;
using System.Collections.Generic;

namespace LaMediaCancha.Models
{
    public class ReservaModels
    {
        public class Reserva
        {
            public int ReservaId { get; set; }
            public string CodigoReserva { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public string ClienteEmail { get; set; }
            public DateTime FechaReserva { get; set; }
            public TimeSpan HoraReserva { get; set; }
            public int NumeroPersonas { get; set; }
            public int? MesaAsignadaId { get; set; }
            public int? MesaNumero { get; set; }
            public string Observaciones { get; set; }
            public string Estado { get; set; } // Pendiente, Confirmada, Cancelada, Completada
            public DateTime FechaCreacion { get; set; }
            public string UsuarioNombre { get; set; }
        }
    }
}