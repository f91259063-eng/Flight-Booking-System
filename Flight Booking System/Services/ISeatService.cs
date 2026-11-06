
using Flight_Booking_System.Models;

namespace Flight_Booking_System.Services
{
    public interface ISeatService
    {
        Task GenerateSeatsAsync(AircraftClass aircraftClass);

        Task<(bool Success, string? Error)> ResizeSeatsAsync(
            int aircraftClassId,
            int targetSeatCount);

        Task<(bool Success, string? Error)> RemoveSeatsAsync(
            int aircraftClassId);
    }
}
