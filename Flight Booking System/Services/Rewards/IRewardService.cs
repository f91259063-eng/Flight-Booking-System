
namespace Flight_Booking_System.Services.Rewards
{
    public interface IRewardService
    {
        // Create Reward Account automatically if it doesn't exist
        Task EnsureAccountAsync(string userId);

        // Get available reward points
        Task<int> GetAvailablePointsAsync(string userId);

        // Calculate discount
        Task<decimal> CalculateDiscountAsync(
            string userId,
            int pointsToRedeem);

        // Reserve points for booking
        Task<bool> ReservePointsAsync(
            string userId,
            int bookingId,
            int points);

        // Confirm redeemed points after payment
        Task<bool> ConfirmRedemptionAsync(
            string userId,
            int bookingId);

        // Return reserved points
        Task<bool> ReleaseReservedPointsAsync(
            string userId,
            int bookingId);

        // Earn points after completed flight
        Task<bool> EarnPointsAsync(
            string userId,
            int bookingId);
    }
}
