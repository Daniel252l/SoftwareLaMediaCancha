using System.ComponentModel.DataAnnotations;

namespace LaMediaCancha.Models.ViewModels
{
    public class RecuperarPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}