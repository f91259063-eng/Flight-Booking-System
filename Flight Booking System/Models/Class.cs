using System.ComponentModel.DataAnnotations;

namespace Flight_Booking_System.Models
{
    public class Class
    {


        public int Id { get; set; }
        [Required(ErrorMessage = "اسم درجة السفر مطلوب")]
        [Display(Name = "درجة السفر")]
        public string Name { get; set; }
        public ICollection<AircraftClass> AircraftClasses { get; set; }
     = new List<AircraftClass>();
    }
}
