
using Flight_Booking_System.Models.Identity;

namespace Flight_Booking_System.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser ApplicationUser { get; set; } = null!;

        public DateTime BookingDate { get; set; }

        public string BookingReference { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int NumberOfTickets { get; set; }

        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();

        // Reward Points
        public int PointsRedeemed { get; set; } = 0;

        public decimal DiscountAmount { get; set; } = 0m;
    }
}
