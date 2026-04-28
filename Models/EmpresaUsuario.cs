using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Empresa_Usuario")]
    public class EmpresaUsuario
    {
        [Key]
        public int EmpresaUsuarioId { get; set; }

        public int EmpresaId { get; set; }
        public int UsuarioId { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [ForeignKey("EmpresaId")]
        public virtual Empresa Empresa { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}