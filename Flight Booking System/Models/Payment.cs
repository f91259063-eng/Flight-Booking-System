using Flight_Booking_System.Models.ENUM;

namespace Flight_Booking_System.Models
{
    public class Payment
    {


        public int Id { get; set; }

        public DateTime PaymentDate { get; set; }

        public PaymentMethod PaymentMethod { set; get; }

        public decimal Amount { get; set; }

        public bool PaymentStatus { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; }


    }
}
