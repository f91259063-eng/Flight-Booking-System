using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Areas.Identity.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username or email")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
