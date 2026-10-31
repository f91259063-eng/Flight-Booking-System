using Flight_Booking_System.Models.Identity;

namespace Flight_Booking_System.Models.Rewards
{
    public class RewardTransaction
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public int? BookingId { get; set; }

        public Booking? Booking { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public int Points { get; set; }

        public string Description { get; set; } = string.Empty;

        public string ReferenceKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}