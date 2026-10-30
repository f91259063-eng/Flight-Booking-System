
namespace Flight_Booking_System.Services.Rewards
{
    public interface IRewardService
    {
        Task<int> GetAvailablePointsAsync(string userId);

        Task<decimal> CalculateDiscountAsync(
            string userId,
            int pointsToRedeem);

        Task<bool> ReservePointsAsync(
            string userId,
            int bookingId,
            int points);

        Task<bool> ConfirmRedemptionAsync(
            string userId,
            int bookingId);

        Task<bool> ReleaseReservedPointsAsync(
            string userId,
            int bookingId);
    }
}
