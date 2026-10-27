
namespace Flight_Booking_System.ViewModels.Customer
{
    public class ChooseSeatViewModel
    {
        public int FlightId { get; set; }

        public int ClassId { get; set; }

        public string FlightNumber { get; set; } = string.Empty;

        public string AircraftName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public decimal TicketPrice { get; set; }

        public List<SeatOptionViewModel> Seats { get; set; }
            = new List<SeatOptionViewModel>();
    }


    public class SeatOptionViewModel
    {
        public int SeatId { get; set; }

        public string SeatNumber { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }
    }
}
