using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Models
{
    public class Airline
    {


        public int Id { get; set; }
        public string Name { get; set; }
        public string AirlineCode { get; set; }



        [MaxLength(11)]
        [MinLength(11)]
        public string  Phone { get; set; }
        public string Email { get; set; }
        public ICollection<AircraftClass> AircraftClasses { get; set; }
     = new List<AircraftClass>();





    }
}
