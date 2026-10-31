using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Flight_Booking_System.ViewModels
{
    public class UserEditViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        // the format is checked in the controller (old accounts may use an email as username)
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Email confirmed")]
        public bool EmailConfirmed { get; set; }

        // leave it empty to keep the current password
        [DataType(DataType.Password)]
        [Display(Name = "New password (optional)")]
        public string? NewPassword { get; set; }

        public List<string> SelectedRoles { get; set; } = new();

        // ---- shown on the page, not read from the form ----
        [BindNever, ValidateNever]
        public List<string> AllRoles { get; set; } = new();

        [BindNever, ValidateNever]
        public bool IsLocked { get; set; }

        [BindNever, ValidateNever]
        public bool TwoFactorEnabled { get; set; }

        [BindNever, ValidateNever]
        public bool IsCurrentUser { get; set; }

        [BindNever, ValidateNever]
        public int BookingsCount { get; set; }
    }
}
