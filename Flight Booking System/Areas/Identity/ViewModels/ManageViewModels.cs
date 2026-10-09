using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Areas.Identity.ViewModels
{
    public class ProfileViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Middle name")]
        public string? MiddleName { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        // Display only
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class TwoFactorStatusViewModel
    {
        public bool Is2faEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }

    public class EnableAuthenticatorViewModel
    {
        [Required, StringLength(7, MinimumLength = 6)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;

        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
    }
}
