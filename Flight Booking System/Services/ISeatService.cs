using Flight_Booking_System.Models;

namespace Flight_Booking_System.Services
{
    public interface ISeatService
   
    {


        Task GenerateSeatsAsync(AircraftClass aircraftClass);

    }


}

