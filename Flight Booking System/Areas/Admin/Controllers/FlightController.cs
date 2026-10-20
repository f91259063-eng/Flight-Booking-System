
using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FlightController : Controller
    {
        private readonly IRepository<Flight> _repository;
        private readonly ApplicationDbContext _context;

        public FlightController(
            IRepository<Flight> repository,
            ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        private async Task LoadDropdowns(
            int? aircraftId = null,
            int? airlineId = null,
            int? routeId = null)
        {
            // Aircrafts Dropdown
            var aircrafts = await _context.Aircrafts
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Aircrafts = new SelectList(
                aircrafts.Select(a => new
                {
                    a.Id,
                    DisplayName =
                        $"{a.RegistrationNumber} - {a.Model} - السعة: {a.Capacity}"
                }),
                "Id",
                "DisplayName",
                aircraftId
            );

            // Airlines Dropdown
            var airlines = await _context.Airlines
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Airlines = new SelectList(
                airlines,
                "Id",
                "Name",
                airlineId
            );

            // Routes Dropdown
            var routes = await _context.Routes
                .Include(r => r.FromAirport)
                .Include(r => r.ToAirport)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Routes = new SelectList(
                routes.Select(r => new
                {
                    r.Id,
                    DisplayName =
                        $"مطار {r.FromAirport?.Name ?? "غير محدد"} " +
                        $"← مطار {r.ToAirport?.Name ?? "غير محدد"} " +
                        $"- المسافة: {r.Distance}"
                }),
                "Id",
                "DisplayName",
                routeId
            );
        }

        // GET: Admin/Flight
        public async Task<IActionResult> Index()
        {
            var flights = await _context.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Airline)
                .Include(f => f.Route)
                    .ThenInclude(r => r.FromAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ToAirport)
                .AsNoTracking()
                .ToListAsync();

            return View(flights);
        }

        // GET: Admin/Flight/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        // POST: Admin/Flight/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Flight flight)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    flight.AircraftId,
                    flight.AirlineId,
                    flight.RouteId
                );

                return View(flight);
            }

            await _repository.InsertAsync(flight);
            await _repository.CommitAsync();

            TempData["Success"] = "Flight created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Flight/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var flight = await _context.Flights
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
                return NotFound();

            await LoadDropdowns(
                flight.AircraftId,
                flight.AirlineId,
                flight.RouteId
            );

            return View(flight);
        }

        // POST: Admin/Flight/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Flight flight)
        {
            if (id != flight.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    flight.AircraftId,
                    flight.AirlineId,
                    flight.RouteId
                );

                return View(flight);
            }

            var existingFlight = await _repository.GetOneAsync(
                f => f.Id == id
            );

            if (existingFlight == null)
                return NotFound();

            existingFlight.AircraftId = flight.AircraftId;
            existingFlight.AirlineId = flight.AirlineId;
            existingFlight.RouteId = flight.RouteId;
            existingFlight.FlightNumber = flight.FlightNumber;
            existingFlight.FlightStatus = flight.FlightStatus;
            existingFlight.DepartureDateTime = flight.DepartureDateTime;
            existingFlight.ArrivalDateTime = flight.ArrivalDateTime;
            existingFlight.DayOfWeek = flight.DayOfWeek;

            _repository.Update(existingFlight);
            await _repository.CommitAsync();

            TempData["Success"] = "Flight updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Flight/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Airline)
                .Include(f => f.Route)
                    .ThenInclude(r => r.FromAirport)
                .Include(f => f.Route)
                    .ThenInclude(r => r.ToAirport)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
                return NotFound();

            return View(flight);
        }

        // POST: Admin/Flight/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flight = await _repository.GetOneAsync(
                f => f.Id == id
            );

            if (flight == null)
                return NotFound();

            _repository.Delete(flight);
            await _repository.CommitAsync();

            TempData["Success"] = "Flight deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
