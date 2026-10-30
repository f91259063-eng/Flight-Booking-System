
using Flight_Booking_System.Data;
using Flight_Booking_System.Models.Rewards;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Flight_Booking_System.Services.Rewards
{
    public class RewardService : IRewardService
    {
        private readonly ApplicationDbContext _context;

        public RewardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 100 points = 50 EGP
        private const int PointsPerDiscount = 100;
        private const decimal DiscountPerPoints = 50m;

        // =====================================
        // Get Available Points
        // =====================================

        public async Task<int> GetAvailablePointsAsync(string userId)
        {
            var account = await _context.RewardAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return account?.AvailablePoints ?? 0;
        }

        // =====================================
        // Calculate Discount
        // =====================================

        public async Task<decimal> CalculateDiscountAsync(
            string userId,
            int pointsToRedeem)
        {
            if (pointsToRedeem < PointsPerDiscount ||
                pointsToRedeem % PointsPerDiscount != 0)
            {
                return 0;
            }

            int availablePoints =
                await GetAvailablePointsAsync(userId);

            if (pointsToRedeem > availablePoints)
                return 0;

            return (pointsToRedeem / PointsPerDiscount)
                   * DiscountPerPoints;
        }

        // =====================================
        // Reserve Points
        // =====================================

        public async Task<bool> ReservePointsAsync(
            string userId,
            int bookingId,
            int points)
        {
            if (points < PointsPerDiscount ||
                points % PointsPerDiscount != 0)
            {
                return false;
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                string reserveKey = $"BOOKING-{bookingId}-RESERVE";
                string redeemKey = $"BOOKING-{bookingId}-REDEEM";
                string releaseKey = $"BOOKING-{bookingId}-RELEASE";

                // Verify booking ownership.
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookingId &&
                        x.UserId == userId);

                if (booking == null ||
                    booking.PointsRedeemed != points)
                {
                    return false;
                }

                // Prevent duplicate reservation.
                var existingReserve =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == reserveKey);

                if (existingReserve != null)
                {
                    return existingReserve.UserId == userId &&
                           existingReserve.Points == points;
                }

                bool alreadyRedeemed =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == redeemKey);

                bool alreadyReleased =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == releaseKey);

                if (alreadyRedeemed || alreadyReleased)
                    return false;

                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (account == null ||
                    account.AvailablePoints < points)
                {
                    return false;
                }

                // Move points from available to reserved.
                account.AvailablePoints -= points;
                account.ReservedPoints += points;

                _context.RewardTransactions.Add(
                    new RewardTransaction
                    {
                        UserId = userId,
                        BookingId = bookingId,
                        TransactionType = "Reserve",
                        Points = points,
                        Description = "Points reserved for booking",
                        ReferenceKey = reserveKey,
                        CreatedAt = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================
        // Confirm Redemption
        // =====================================

        public async Task<bool> ConfirmRedemptionAsync(
            string userId,
            int bookingId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                string reserveKey = $"BOOKING-{bookingId}-RESERVE";
                string redeemKey = $"BOOKING-{bookingId}-REDEEM";
                string releaseKey = $"BOOKING-{bookingId}-RELEASE";

                // Verify booking ownership and confirmation.
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookingId &&
                        x.UserId == userId);

                if (booking == null ||
                    booking.Status != "Confirmed")
                {
                    return false;
                }

                // Verify successful payment.
                bool paymentSucceeded =
                    await _context.Payments.AnyAsync(x =>
                        x.BookingId == bookingId &&
                        x.PaymentStatus);

                if (!paymentSucceeded)
                    return false;

                // Check whether redemption was already confirmed.
                var existingRedeem =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == redeemKey);

                if (existingRedeem != null)
                {
                    return existingRedeem.UserId == userId &&
                           existingRedeem.Points == booking.PointsRedeemed;
                }

                // A released reservation cannot be redeemed.
                bool alreadyReleased =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == releaseKey);

                if (alreadyReleased)
                    return false;

                // Get original reservation.
                var reserve =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == reserveKey &&
                            x.UserId == userId);

                if (reserve == null ||
                    reserve.Points != booking.PointsRedeemed ||
                    reserve.Points <= 0)
                {
                    return false;
                }

                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (account == null ||
                    account.ReservedPoints < reserve.Points)
                {
                    return false;
                }

                // Confirm redemption.
                account.ReservedPoints -= reserve.Points;
                account.TotalRedeemedPoints += reserve.Points;

                _context.RewardTransactions.Add(
                    new RewardTransaction
                    {
                        UserId = userId,
                        BookingId = bookingId,
                        TransactionType = "Redeem",
                        Points = reserve.Points,
                        Description = "Points redeemed after successful payment",
                        ReferenceKey = redeemKey,
                        CreatedAt = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================
        // Release Reserved Points
        // =====================================

        public async Task<bool> ReleaseReservedPointsAsync(
            string userId,
            int bookingId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                string reserveKey = $"BOOKING-{bookingId}-RESERVE";
                string redeemKey = $"BOOKING-{bookingId}-REDEEM";
                string releaseKey = $"BOOKING-{bookingId}-RELEASE";

                bool alreadyRedeemed =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == redeemKey);

                if (alreadyRedeemed)
                    return false;

                var existingRelease =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == releaseKey);

                if (existingRelease != null)
                {
                    return existingRelease.UserId == userId;
                }

                var reserve =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == reserveKey &&
                            x.UserId == userId);

                if (reserve == null)
                    return false;

                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (account == null ||
                    account.ReservedPoints < reserve.Points)
                {
                    return false;
                }

                // Return reserved points to available balance.
                account.ReservedPoints -= reserve.Points;
                account.AvailablePoints += reserve.Points;

                _context.RewardTransactions.Add(
                    new RewardTransaction
                    {
                        UserId = userId,
                        BookingId = bookingId,
                        TransactionType = "Release",
                        Points = reserve.Points,
                        Description = "Reserved points returned",
                        ReferenceKey = releaseKey,
                        CreatedAt = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
