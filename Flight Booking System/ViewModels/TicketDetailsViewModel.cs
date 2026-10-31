
namespace Flight_Booking_System.ViewModels.Customer
{
    public class TicketDetailsViewModel
    {
        public int TicketId { get; set; }

        public string BookingReference { get; set; } = string.Empty;

        public string BookingStatus { get; set; } = string.Empty;

        public string PassengerName { get; set; } = string.Empty;

        public string FlightNumber { get; set; } = string.Empty;

        public string DepartureAirport { get; set; } = string.Empty;

        public string ArrivalAirport { get; set; } = string.Empty;

        public DateTime DepartureDateTime { get; set; }

        public string SeatNumber { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public decimal TicketPrice { get; set; }
    }
}
