using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace LaMediaCancha.Models.ViewModels
{
    public class CrearUsuarioViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre Completo")]
        [MaxLength(150)]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo no válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Selecciona un rol")]
        [Display(Name = "Rol")]
        public int RolId { get; set; }

        // Lista para el dropdown de roles
        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}