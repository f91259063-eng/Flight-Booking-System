namespace Flight_Booking_System.ViewModels
{
    public class TicketHistoryItem
    {
        public string BookingReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Departure { get; set; }
    }

    public class CustomerHistoryViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public List<TicketHistoryItem> Tickets { get; set; } = new();
    }

    public class CustomerSearchViewModel
    {
        public string? Query { get; set; }
        public List<CustomerHistoryViewModel> Results { get; set; } = new();
    }

    // one flight in the staff search
    public class StaffFlightRow
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }

        public int AvailableSeats => Math.Max(TotalSeats - BookedSeats, 0);
    }

    public class StaffFlightSearchViewModel
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public DateTime? Date { get; set; }
        public List<StaffFlightRow> Flights { get; set; } = new();
    }
}
