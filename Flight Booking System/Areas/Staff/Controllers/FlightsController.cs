using Flight_Booking_System.Data;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Staff.Controllers
{
    // Read only: the staff can see the flights and how many seats are still free
    [Area("Staff")]
    [Authorize(Roles = AppRoles.StaffRoles)]
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FlightsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? from, string? to, DateTime? date)
        {
            // only active flights that did not leave yet
            var query = _db.Flights
                .AsNoTracking()
                .Where(f => f.FlightStatus && f.DepartureDateTime >= DateTime.Now);

            if (!string.IsNullOrWhiteSpace(from))
            {
                from = from.Trim();
                query = query.Where(f => f.Route!.FromAirport.Name.Contains(from));
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                to = to.Trim();
                query = query.Where(f => f.Route!.ToAirport.Name.Contains(to));
            }

            if (date.HasValue)
            {
                var day = date.Value.Date;
                query = query.Where(f => f.DepartureDateTime.Date == day);
            }

            var flights = await query
                .OrderBy(f => f.DepartureDateTime)
                .Take(100)
                .Select(f => new StaffFlightRow
                {
                    FlightId = f.Id,
                    FlightNumber = f.FlightNumber,
                    Airline = f.Airline!.Name,
                    From = f.Route!.FromAirport.Name,
                    To = f.Route!.ToAirport.Name,
                    Departure = f.DepartureDateTime,
                    Arrival = f.ArrivalDateTime,
                    TotalSeats = _db.AircraftClasses
                        .Where(ac => ac.AircraftId == f.AircraftId)
                        .Sum(ac => ac.NumberOfSeats),
                    BookedSeats = _db.Tickets.Count(t => t.FlightId == f.Id)
                })
                .ToListAsync();

            return View(new StaffFlightSearchViewModel
            {
                From = from,
                To = to,
                Date = date,
                Flights = flights
            });
        }
    }
}
