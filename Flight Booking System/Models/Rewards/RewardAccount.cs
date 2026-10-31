
using System.ComponentModel.DataAnnotations;
using Flight_Booking_System.Models.Identity;

namespace Flight_Booking_System.Models.Rewards
{
    public class RewardAccount
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        public int AvailablePoints { get; set; }

        public int TotalEarnedPoints { get; set; }

        public int TotalRedeemedPoints { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public int ReservedPoints { get; set; }
    }
}
