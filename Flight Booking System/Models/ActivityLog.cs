using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flight_Booking_System.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public  Models.Identity.ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Control { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityName { get; set; }

        public string? EntityId { get; set; }

        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}