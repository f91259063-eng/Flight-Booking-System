namespace Flight_Booking_System.Models
{
    public class Setting
    {
        public int Id { get; set; }

        // سعر الكيلومتر
        public decimal PricePerKm { get; set; }

        // تاريخ آخر تحديث للسعر
        public DateTime LastUpdated { get; set; }
    }
}