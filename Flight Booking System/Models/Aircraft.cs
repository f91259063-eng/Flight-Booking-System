namespace Flight_Booking_System.Models
{
    public class Aircraft
    {

        public int Id { get; set; }


        public int AirlineId { get; set; }
        public Airline Airline { get; set; }

        public string RegistrationNumber { get; set; }
        public string Model { get; set; }
        public int Capacity { get; set; }


    }
}
