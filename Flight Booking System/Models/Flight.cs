namespace Flight_Booking_System.Models
{
    public class Flight
    {
        public int Id { get; set; }

        public int AircraftId { get; set; }
        public Aircraft Aircraft { get; set; } = null!;


        public  bool FlightStatus { get; set; }


        public int AirlineId { get; set; }
        public Airline Airline { get; set; } = null!;

        public int RouteId { get; set; }
        public Route Route { get; set; } = null!;

        public DateTime ArrivalDateTime { get; set; }

        public DateTime DepartureDateTime { get; set; }

        public string FlightNumber { get; set; } = string.Empty;

        public DayOfWeek DayOfWeek { get; set; }
    }
}