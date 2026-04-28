using System.ComponentModel.DataAnnotations;

namespace LaMediaCancha.Models.ViewModels
{
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [Display(Name = "Contraseña Actual")]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener mínimo 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "La contraseña debe tener al menos una mayúscula, una minúscula, un número y un símbolo")]
        [Display(Name = "Nueva Contraseña")]
        public string NuevaPassword { get; set; }

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña")]
        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmarPassword { get; set; }
    }
}