
namespace Flight_Booking_System.ViewModels.Customer
{
    public class BookingConfirmationViewModel
    {
        public int BookingId { get; set; }

        public string BookingReference { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string FlightNumber { get; set; } = string.Empty;

        public string FromAirport { get; set; } = string.Empty;

        public string ToAirport { get; set; } = string.Empty;

        public DateTime DepartureDateTime { get; set; }

        public DateTime ArrivalDateTime { get; set; }

        public List<BookingTicketViewModel> Tickets { get; set; }
            = new List<BookingTicketViewModel>();

        public decimal TotalPrice => Tickets.Sum(t => t.TicketPrice);
    }

    public class BookingTicketViewModel
    {
        public string PassengerName { get; set; } = string.Empty;

        public string SeatNumber { get; set; } = string.Empty;

        public decimal TicketPrice { get; set; }
    }
}
