using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Identity.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        // checked while typing by AccountController.CheckUserName
        [Required]
        [StringLength(20, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use only letters, numbers, dot, dash or underscore.")]
        [Remote(action: "CheckUserName", controller: "Account", areaName: "Identity")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
