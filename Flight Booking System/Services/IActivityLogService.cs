
namespace Flight_Booking_System.ModelsServices
{
    public interface IActivityLogService
    {
        Task LogAsync(
            string control,
            string action,
            string? entityName = null,
            string? entityId = null,
            string? description = null);
    }
}
