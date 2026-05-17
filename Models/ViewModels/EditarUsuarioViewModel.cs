using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace LaMediaCancha.Models.ViewModels
{
    public class EditarUsuarioViewModel
    {
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        public int RolId { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        [MaxLength(150)]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Correo no válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Usuario Activo")]
        public bool Activo { get; set; }

        [Display(Name = "Forzar cambio de contraseña")]
        public bool EsPasswordTemporal { get; set; }

        [Display(Name = "Nueva Contraseña")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        [DataType(DataType.Password)]
        public string NuevaContrasena { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}