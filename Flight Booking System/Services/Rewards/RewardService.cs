
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
        // Ensure Reward Account
        // =====================================

        public async Task EnsureAccountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException(
                    "User ID is required.", nameof(userId));

            bool accountExists = await _context.RewardAccounts
                .AnyAsync(x => x.UserId == userId);

            if (accountExists)
                return;

            bool userExists = await _context.Users
                .AnyAsync(x => x.Id == userId);

            if (!userExists)
                throw new InvalidOperationException(
                    "User does not exist.");

            var account = new RewardAccount
            {
                UserId = userId,
                AvailablePoints = 0,
                ReservedPoints = 0,
                TotalEarnedPoints = 0,
                TotalRedeemedPoints = 0
            };

            _context.RewardAccounts.Add(account);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(account).State = EntityState.Detached;

                bool createdByAnotherRequest =
                    await _context.RewardAccounts
                        .AnyAsync(x => x.UserId == userId);

                if (!createdByAnotherRequest)
                    throw;
            }
        }

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

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookingId &&
                        x.UserId == userId);

                if (booking == null ||
                    booking.PointsRedeemed != points)
                {
                    return false;
                }

                bool alreadyRedeemed =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == redeemKey);

                bool alreadyReleased =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == releaseKey);

                if (alreadyRedeemed || alreadyReleased)
                    return false;

                var existingReserve =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == reserveKey);

                if (existingReserve != null)
                {
                    return existingReserve.UserId == userId &&
                           existingReserve.Points == points;
                }

                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (account == null ||
                    account.AvailablePoints < points)
                {
                    return false;
                }

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

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookingId &&
                        x.UserId == userId);

                if (booking == null ||
                    booking.Status != "Confirmed" ||
                    booking.PointsRedeemed <= 0)
                {
                    return false;
                }

                bool paymentSucceeded =
                    await _context.Payments.AnyAsync(x =>
                        x.BookingId == bookingId &&
                        x.PaymentStatus);

                if (!paymentSucceeded)
                    return false;

                var existingRedeem =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == redeemKey);

                if (existingRedeem != null)
                {
                    return existingRedeem.UserId == userId &&
                           existingRedeem.Points == booking.PointsRedeemed;
                }

                bool alreadyReleased =
                    await _context.RewardTransactions.AnyAsync(
                        x => x.ReferenceKey == releaseKey);

                if (alreadyReleased)
                    return false;

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
                    return existingRelease.UserId == userId;

                var reserve =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == reserveKey &&
                            x.UserId == userId);

                if (reserve == null || reserve.Points <= 0)
                    return false;

                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (account == null ||
                    account.ReservedPoints < reserve.Points)
                {
                    return false;
                }

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

        // =====================================
        // Earn Points After Completed Flight
        // =====================================

        public async Task<bool> EarnPointsAsync(
            string userId,
            int bookingId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                string earnKey = $"BOOKING-{bookingId}-EARN";

                // Get confirmed booking
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookingId &&
                        x.UserId == userId &&
                        x.Status == "Confirmed");

                if (booking == null)
                    return false;

                // Get successful payment
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(x =>
                        x.BookingId == bookingId &&
                        x.PaymentStatus);

                if (payment == null || payment.Amount <= 0)
                    return false;

                // Get ticket
                var ticket = await _context.Tickets
                    .FirstOrDefaultAsync(x =>
                        x.BookingId == bookingId);

                if (ticket == null)
                    return false;

                // Check flight status
                var flight = await _context.Flights
                    .FirstOrDefaultAsync(x =>
                        x.Id == ticket.FlightId);

                if (flight == null ||
                    flight.FlightStatus !=
                    Flight_Booking_System.Models.ENUM.FlightStatus.Completed)
                {
                    return false;
                }

                // Prevent duplicate earning
                var existingEarn =
                    await _context.RewardTransactions
                        .FirstOrDefaultAsync(x =>
                            x.ReferenceKey == earnKey);

                if (existingEarn != null)
                {
                    return existingEarn.UserId == userId &&
                           existingEarn.BookingId == bookingId;
                }

                // 1 point for every 100 EGP paid
                int earnedPoints =
                    (int)Math.Floor(payment.Amount / 100m);

                if (earnedPoints <= 0)
                    return false;

                // Get or create Reward Account
                var account = await _context.RewardAccounts
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId);

                if (account == null)
                {
                    account = new RewardAccount
                    {
                        UserId = userId,
                        AvailablePoints = 0,
                        ReservedPoints = 0,
                        TotalEarnedPoints = 0,
                        TotalRedeemedPoints = 0
                    };

                    _context.RewardAccounts.Add(account);
                }

                // Add points
                account.AvailablePoints += earnedPoints;
                account.TotalEarnedPoints += earnedPoints;

                // Record transaction
                _context.RewardTransactions.Add(
                    new RewardTransaction
                    {
                        UserId = userId,
                        BookingId = bookingId,
                        TransactionType = "Earn",
                        Points = earnedPoints,
                        Description =
                            $"Earned {earnedPoints} points after completed flight",
                        ReferenceKey = earnKey,
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
