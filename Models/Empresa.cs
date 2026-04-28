using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Empresa")]
    public class Empresa
    {
        [Key]
        public int EmpresaId { get; set; }

        [Required, MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string Descripcion { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<EmpresaUsuario> EmpresaUsuarios { get; set; }
        public virtual ICollection<Bitacora> Bitacoras { get; set; }
    }
}