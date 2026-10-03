using Flight_Booking_System.Models;

public class Ticket
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    public int FlightId { get; set; }
    public Flight Flight { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;
    
}