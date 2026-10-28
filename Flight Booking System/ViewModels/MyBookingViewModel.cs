namespace Flight_Booking_System.ViewModels.Customer
{
    public class MyBookingViewModel
    {
        public int BookingId { get; set; }

        public string BookingReference { get; set; }

        public DateTime BookingDate { get; set; }

        public string Status { get; set; }

        public int NumberOfTickets { get; set; }

        public decimal TotalPrice { get; set; }

        public string FlightNumber { get; set; }

        public string DepartureAirport { get; set; }

        public string ArrivalAirport { get; set; }

        public DateTime DepartureDateTime { get; set; }

        public DateTime ArrivalDateTime { get; set; }
    }
}