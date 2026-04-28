using System;
using System.ComponentModel.DataAnnotations;

namespace LaMediaCancha.Models
{
    public class Bitacora
    {
        public int BitacoraId { get; set; }
        public int? UsuarioId { get; set; }

        [MaxLength(100)]
        public string UsuarioNombre { get; set; }

        [Required, MaxLength(100)]
        public string Accion { get; set; }

        [MaxLength(100)]
        public string Tabla { get; set; }

        public string Detalle { get; set; }

        public DateTime Fecha { get; set; }
    }
}