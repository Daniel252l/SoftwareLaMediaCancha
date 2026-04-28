using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        public int RolId { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; }

        [Required, MaxLength(150)]
        public string NombreCompleto { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; }

        [Required, MaxLength(256)]
        public string Salt { get; set; }

        [Required, MaxLength(512)]
        public string PasswordHash { get; set; }

        [MaxLength(10)]
        public string SoundexPassword { get; set; }

        [MaxLength(100)]
        public string NumerosSimbolosPassword { get; set; }

        public int LongitudPassword { get; set; }

        public bool EsPasswordTemporal { get; set; } = true;
        public DateTime? FechaPasswordTemporal { get; set; }

        public bool Activo { get; set; } = true;
        public bool Bloqueado { get; set; } = false;
        public DateTime? FechaBloqueo { get; set; }
        public int IntentosFallidos { get; set; } = 0;
        public DateTime? FechaUltimoAcceso { get; set; }

        [MaxLength(200)]
        public string TokenRecuperacion { get; set; }
        public DateTime? ExpiracionToken { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public virtual ICollection<EmpresaUsuario> EmpresaUsuarios { get; set; }
        public virtual ICollection<MenuUsuario> MenuUsuarios { get; set; }
        public virtual ICollection<Bitacora> Bitacoras { get; set; }
    }
}