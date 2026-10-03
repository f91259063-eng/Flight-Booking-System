namespace Flight_Booking_System.Models
{
    public class Seat
    {


        public int Id { get; set; }

        public string SeatNumber { get; set; }

        public int AircraftClassId { get; set; }

        public AircraftClass AircraftClass { get; set; }

    }
}
