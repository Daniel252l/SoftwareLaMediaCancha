using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Menu_Rol")]
    public class MenuRol
    {
        [Key]
        public int MenuRolId { get; set; }

        public int MenuId { get; set; }
        public int RolId { get; set; }

        public bool PuedeVer { get; set; } = false;
        public bool PuedeCrear { get; set; } = false;
        public bool PuedeEditar { get; set; } = false;
        public bool PuedeEliminar { get; set; } = false;

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; }
    }
}