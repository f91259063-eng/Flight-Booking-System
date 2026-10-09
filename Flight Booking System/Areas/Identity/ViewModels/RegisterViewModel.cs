using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Areas.Identity.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        // string? => optional. (A non-nullable string is treated as [Required] by MVC.)
        [StringLength(50)]
        [Display(Name = "Middle name")]
        public string? MiddleName { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
