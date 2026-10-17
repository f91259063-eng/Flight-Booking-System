
using Flight_Booking_System.Models;

namespace Flight_Booking_System.ViewModels
{
    public class AircraftSeatsViewModel
    {
        public int Id { get; set; }

        public string RegistrationNumber { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string AirlineName { get; set; } = string.Empty;

        public List<AircraftClassSeatsViewModel> Classes { get; set; } = new();
    }

    public class AircraftClassSeatsViewModel
    {
        public int AircraftClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public int NumberOfSeats { get; set; }

        public List<Seat> Seats { get; set; } = new();
    }
}