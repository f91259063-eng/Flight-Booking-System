namespace Flight_Booking_System.Models
{
    public class Route
    {
        public int Id { get; set; }

        public int FromAirportId { get; set; }
        public AirPort FromAirport { get; set; } = null!;

        public int ToAirportId { get; set; }
        public AirPort ToAirport { get; set; } = null!;

        public double Distance { get; set; }
    }
}
