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

        [MaxLength(11)]
        [MinLength(11)]
        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
    }
}