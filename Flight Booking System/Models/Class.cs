namespace Flight_Booking_System.Models
{
    public class Class
    {


        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<AircraftClass> AircraftClasses { get; set; }
     = new List<AircraftClass>();
    }
}
