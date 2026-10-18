namespace Flight_Booking_System.Models
{
    public class Seat
    {
        public int Id { get; set; }

        // رقم المقعد داخل AircraftClass
        // مثال: 001, 002, 003
        public string SeatNumber { get; set; } = string.Empty;

        // العلاقة مع AircraftClass
        public int AircraftClassId { get; set; }

        public AircraftClass AircraftClass { get; set; } = null!;
    }
}