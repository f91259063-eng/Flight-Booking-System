using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Areas.Identity.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResendConfirmationViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorLoginViewModel
    {
        [Required, StringLength(7, MinimumLength = 6)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Remember this device")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RecoveryCodeLoginViewModel
    {
        [Required]
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    // One generic "message" page (check your email, email confirmed, password changed, ...)
    public class MessageViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = true;
        public string? LinkText { get; set; }
        public string? LinkUrl { get; set; }

        // Only filled in Development, so you can test without an SMTP server
        public string? DevLink { get; set; }
    }
}
