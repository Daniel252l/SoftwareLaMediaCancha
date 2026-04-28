using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaMediaCancha.Models
{
    [Table("Menus")]
    public class Menu
    {
        [Key]
        public int MenuId { get; set; }

        public int? MenuPadreId { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(50)]
        public string Icono { get; set; }

        [MaxLength(200)]
        public string Ruta { get; set; }

        public int Orden { get; set; }
        public bool Activo { get; set; } = true;

        [ForeignKey("MenuPadreId")]
        public virtual Menu MenuPadre { get; set; }

        public virtual ICollection<Menu> SubMenus { get; set; }
        public virtual ICollection<MenuRol> MenuRoles { get; set; }
        public virtual ICollection<MenuUsuario> MenuUsuarios { get; set; }
    }
}