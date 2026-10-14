using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Models
{
    public class Airline
    {


        public int Id { get; set; }
        public string Name { get; set; }
        public string AirlineCode { get; set; }


        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(11, MinimumLength = 11,
            ErrorMessage = "رقم الهاتف يجب أن يتكون من 11 رقمًا")]
        [RegularExpression(@"^[0-9]{11}$",
            ErrorMessage = "رقم الهاتف يجب أن يحتوي على أرقام فقط")]
        public string Phone { get; set; }

        public string Email { get; set; }
        public ICollection<AircraftClass> AircraftClasses { get; set; } = new List<AircraftClass>();
    }
}
