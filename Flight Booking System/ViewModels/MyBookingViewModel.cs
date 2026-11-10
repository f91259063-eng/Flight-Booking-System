
namespace Flight_Booking_System.ViewModels.Customer
{
    public class MyBookingViewModel
    {
        public int BookingId { get; set; }

        public string BookingReference { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int NumberOfTickets { get; set; }

        public decimal TotalPrice { get; set; }

        public string FlightNumber { get; set; } = string.Empty;

        public string DepartureAirport { get; set; } = string.Empty;

        public string ArrivalAirport { get; set; } = string.Empty;

        public DateTime DepartureDateTime { get; set; }

        public DateTime ArrivalDateTime { get; set; }

        // Payment Information
        public bool PaymentStatus { get; set; }

        public decimal PaymentAmount { get; set; }

        public bool CanPay =>
            Status == "Pending" &&
            !PaymentStatus &&
            PaymentAmount > 0;
    }
}
