
using Flight_Booking_System.Data;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Controllers
{
    [Area("Customer")]
    [Authorize]

    public class MyBookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyBookingsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var bookings = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var result = new List<MyBookingViewModel>();

            foreach (var booking in bookings)
            {
                var tickets = await _db.Tickets
                    .AsNoTracking()
                    .Where(t => t.BookingId == booking.Id)
                    .ToListAsync();

                var firstTicket = tickets.FirstOrDefault();

                var payment = await _db.Payments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.BookingId == booking.Id);

                string flightNumber = "N/A";
                string departureAirport = "Unknown Airport";
                string arrivalAirport = "Unknown Airport";

                DateTime departureDateTime = default;
                DateTime arrivalDateTime = default;

                if (firstTicket != null)
                {
                    var flight = await _db.Flights
                        .AsNoTracking()
                        .Include(f => f.Route)
                        .FirstOrDefaultAsync(
                            f => f.Id == firstTicket.FlightId);

                    if (flight != null)
                    {
                        flightNumber = flight.FlightNumber;
                        departureDateTime = flight.DepartureDateTime;
                        arrivalDateTime = flight.ArrivalDateTime;

                        if (flight.Route != null)
                        {
                            var fromAirport = await _db.AirPorts
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    a => a.Id == flight.Route.FromAirportId);

                            var toAirport = await _db.AirPorts
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    a => a.Id == flight.Route.ToAirportId);

                            departureAirport =
                                fromAirport?.Name ?? "Unknown Airport";

                            arrivalAirport =
                                toAirport?.Name ?? "Unknown Airport";
                        }
                    }
                }

                result.Add(new MyBookingViewModel
                {
                    BookingId = booking.Id,
                    BookingReference = booking.BookingReference,
                    BookingDate = booking.BookingDate,
                    Status = booking.Status,
                    NumberOfTickets = booking.NumberOfTickets,

                    TotalPrice = tickets.Sum(t => t.TicketPrice),

                    FlightNumber = flightNumber,
                    DepartureAirport = departureAirport,
                    ArrivalAirport = arrivalAirport,

                    DepartureDateTime = departureDateTime,
                    ArrivalDateTime = arrivalDateTime,

                    PaymentStatus = payment?.PaymentStatus ?? false,
                    PaymentAmount = payment?.Amount ?? 0m
                });
            }

            return View(result);
        }
    }
}
