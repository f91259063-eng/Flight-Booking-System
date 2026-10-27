
namespace Flight_Booking_System.ViewModels.Customer
{
    public class AvailableFlightViewModel
    {
        public int FlightId { get; set; }

        public string FlightNumber { get; set; } = string.Empty;

        public string AirlineName { get; set; } = string.Empty;

        public string AircraftName { get; set; } = string.Empty;

        public string DepartureAirport { get; set; } = string.Empty;

        public string ArrivalAirport { get; set; } = string.Empty;

        public double Distance { get; set; }

        public DateTime DepartureDateTime { get; set; }

        public DateTime ArrivalDateTime { get; set; }

        public bool FlightStatus { get; set; }
    }
}
