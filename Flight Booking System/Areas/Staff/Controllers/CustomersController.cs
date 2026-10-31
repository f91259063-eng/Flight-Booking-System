using Flight_Booking_System.Data;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Staff.Controllers
{
    // Read only: staff can look at a customer's history but can't change anything here
    [Area("Staff")]
    [Authorize(Roles = AppRoles.StaffRoles)]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomersController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Search(string? q)
        {
            var model = new CustomerSearchViewModel { Query = q };

            if (string.IsNullOrWhiteSpace(q))
                return View(model);

            q = q.Trim();

            var passengers = await _db.Passengers
                .AsNoTracking()
                .Where(p => p.Phone.Contains(q) ||
                            p.NationalID.Contains(q) ||
                            p.Email.Contains(q) ||
                            p.FirstName.Contains(q) ||
                            p.LastName.Contains(q))
                .OrderBy(p => p.FirstName)
                .Take(20)
                .ToListAsync();

            var ids = passengers.Select(p => p.Id).ToList();

            var tickets = await _db.Tickets
                .AsNoTracking()
                .Where(t => ids.Contains(t.PassengerId))
                .Select(t => new
                {
                    t.PassengerId,
                    t.Booking.BookingReference,
                    t.Booking.Status,
                    t.Booking.BookingDate,
                    t.Flight.FlightNumber,
                    From = t.Flight.Route.FromAirport.Name,
                    To = t.Flight.Route.ToAirport.Name,
                    t.Flight.DepartureDateTime
                })
                .ToListAsync();

            // the receptionist sees the id numbers hidden, the agent and the admin see them all
            var seeFullIds = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.BookingAgent);

            foreach (var p in passengers)
            {
                model.Results.Add(new CustomerHistoryViewModel
                {
                    FullName = $"{p.FirstName} {p.MiddleName} {p.LastName}".Replace("  ", " ").Trim(),
                    Phone = p.Phone,
                    Email = p.Email,
                    NationalId = seeFullIds ? p.NationalID : Hide(p.NationalID),
                    PassportNumber = seeFullIds ? p.PassportNumber : Hide(p.PassportNumber),
                    Tickets = tickets
                        .Where(t => t.PassengerId == p.Id)
                        .OrderByDescending(t => t.DepartureDateTime)
                        .Select(t => new TicketHistoryItem
                        {
                            BookingReference = t.BookingReference,
                            Status = t.Status,
                            BookingDate = t.BookingDate,
                            FlightNumber = t.FlightNumber,
                            From = t.From,
                            To = t.To,
                            Departure = t.DepartureDateTime
                        })
                        .ToList()
                });
            }

            return View(model);
        }

        // 12345678901234 -> **********1234
        private static string Hide(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length <= 4) return new string('*', value.Length);

            return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
        }
    }
}
