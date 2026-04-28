using System.ComponentModel.DataAnnotations;

namespace LaMediaCancha.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string NuevaPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NuevaPassword")]
        public string ConfirmarPassword { get; set; }
    }
}