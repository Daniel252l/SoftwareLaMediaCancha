using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Menu_Usuario")]
    public class MenuUsuario
    {
        [Key]
        public int MenuUsuarioId { get; set; }

        public int MenuId { get; set; }
        public int UsuarioId { get; set; }

        public bool PuedeVer { get; set; } = false;
        public bool PuedeCrear { get; set; } = false;
        public bool PuedeEditar { get; set; } = false;
        public bool PuedeEliminar { get; set; } = false;

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}