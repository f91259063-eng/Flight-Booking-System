using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Models
{
    public class Passenger
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string MiddleName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PassportNumber { get; set; } = string.Empty;

        public string NationalID { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(11, MinimumLength = 11,
              ErrorMessage = "رقم الهاتف يجب أن يتكون من 11 رقمًا")]
        [RegularExpression(@"^[0-9]{11}$",
              ErrorMessage = "رقم الهاتف يجب أن يحتوي على أرقام فقط")]
        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
    }
}