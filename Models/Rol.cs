using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Roles")]
    public class Rol
    {
        [Key]
        public int RolId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        [MaxLength(200)]
        public string Descripcion { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<Usuario> Usuarios { get; set; }
        public virtual ICollection<MenuRol> MenuRoles { get; set; }
    }
}