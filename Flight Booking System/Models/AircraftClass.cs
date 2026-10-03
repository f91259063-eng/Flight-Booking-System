namespace Flight_Booking_System.Models
{
    public class AircraftClass
    {

        public int ID { get; set; }

        public int AircraftId { get; set; }
        public Aircraft Aircraft { get; set; } = null!;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public int NumberOfSeats { get; set; }

    






}
}
