
namespace Flight_Booking_System.ViewModels.Customer
{
    public class PassengerBookingViewModel
    {
        public int FlightId { get; set; }

        public int ClassId { get; set; }

        public decimal TicketPrice { get; set; }

        // Reward Points
        public int AvailablePoints { get; set; }

        public int PointsToRedeem { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalPrice { get; set; }

        // Existing passengers
        public List<PassengerOptionViewModel> ExistingPassengers { get; set; }
            = new List<PassengerOptionViewModel>();

        // Selected passengers and seats
        public List<PassengerSeatViewModel> Passengers { get; set; }
            = new List<PassengerSeatViewModel>();
    }

    public class PassengerSeatViewModel
    {
        public int SeatId { get; set; }

        public string SeatNumber { get; set; } = string.Empty;

        public int? SelectedPassengerId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string MiddleName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PassportNumber { get; set; } = string.Empty;

        public string NationalID { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
    }

    public class PassengerOptionViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PassportNumber { get; set; } = string.Empty;

        public string NationalID { get; set; } = string.Empty;
    }
}
