using Microsoft.AspNetCore.Authorization;
﻿

using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.ENUM;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services.Rewards;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FlightController : Controller
    {
        private readonly IRepository<Flight> _repository;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogService;
        private readonly IRewardService _rewardService;

        public FlightController(
            IRepository<Flight> repository,
            ApplicationDbContext context,
            IActivityLogService activityLogService,
            IRewardService rewardService)
        {
            _repository = repository;
            _context = context;
            _activityLogService = activityLogService;
            _rewardService = rewardService;
        }

        // =====================================
        // Load Dropdowns
        // =====================================

        private async Task LoadDropdowns(
            int? aircraftId = null,
            int? airlineId = null,
            int? routeId = null)
        {
            var aircrafts = await _context.Aircrafts
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Aircrafts = new SelectList(
                aircrafts.Select(a => new
                {
                    a.Id,
                    DisplayName =
                        $"{a.RegistrationNumber} - {a.Model} - Capacity: {a.Capacity}"
                }),
                "Id",
                "DisplayName",
                aircraftId
            );

            var airlines = await _context.Airlines
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Airlines = new SelectList(
                airlines,
                "Id",
                "Name",
                airlineId
            );

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
                        $"{r.FromAirport?.Name ?? "Undefined"} " +
                        "→ " +
                        $"{r.ToAirport?.Name ?? "Undefined"} " +
                        $"- Distance: {r.Distance}"
                }),
                "Id",
                "DisplayName",
                routeId
            );
        }

        // =====================================
        // Index
        // =====================================

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

        // =====================================
        // Create
        // =====================================

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

            var result = await _repository.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    nameof(FlightController),
                    nameof(Create),
                    nameof(Flight),
                    flight.Id.ToString(),
                    $"Created flight {flight.FlightNumber}. " +
                    $"Status: {flight.FlightStatus}"
                );

                TempData["Success"] =
                    "Flight created successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Failed to create flight.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // Edit
        // =====================================

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

            // Save old values
            var oldStatus = existingFlight.FlightStatus;
            var oldFlightNumber = existingFlight.FlightNumber;

            // Update flight
            existingFlight.AircraftId = flight.AircraftId;
            existingFlight.AirlineId = flight.AirlineId;
            existingFlight.RouteId = flight.RouteId;
            existingFlight.FlightNumber = flight.FlightNumber;
            existingFlight.FlightStatus = flight.FlightStatus;
            existingFlight.DepartureDateTime =
                flight.DepartureDateTime;
            existingFlight.ArrivalDateTime =
                flight.ArrivalDateTime;
            existingFlight.DayOfWeek = flight.DayOfWeek;

            _repository.Update(existingFlight);

            var result = await _repository.CommitAsync();

            if (result <= 0)
            {
                TempData["Error"] =
                    "Failed to update flight.";

                return RedirectToAction(nameof(Index));
            }

            // Activity Log
            await _activityLogService.LogAsync(
                nameof(FlightController),
                nameof(Edit),
                nameof(Flight),
                existingFlight.Id.ToString(),
                $"Updated flight {oldFlightNumber}. " +
                $"Status changed from {oldStatus} " +
                $"to {existingFlight.FlightStatus}."
            );

            TempData["Success"] =
                "Flight updated successfully.";

            // =====================================
            // Reward Points
            // =====================================

            // Earn points only when status changes
            // from another status to Completed.

            if (oldStatus != FlightStatus.Completed &&
                existingFlight.FlightStatus == FlightStatus.Completed)
            {
                // Get bookings connected to this flight
                var bookingIds = await _context.Tickets
                    .AsNoTracking()
                    .Where(t => t.FlightId == existingFlight.Id)
                    .Select(t => t.BookingId)
                    .Distinct()
                    .ToListAsync();

                // Only confirmed bookings
                var bookings = await _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        bookingIds.Contains(b.Id) &&
                        b.Status == "Confirmed")
                    .Select(b => new
                    {
                        b.Id,
                        b.UserId
                    })
                    .ToListAsync();

                int earnedCount = 0;
                int failedCount = 0;

                foreach (var booking in bookings)
                {
                    try
                    {
                        bool earned =
                            await _rewardService.EarnPointsAsync(
                                booking.UserId,
                                booking.Id
                            );

                        if (earned)
                            earnedCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                }

                // Update result message
                TempData["Success"] =
                    $"Flight completed successfully. " +
                    $"Reward points awarded to {earnedCount} booking(s).";

                if (failedCount > 0)
                {
                    TempData["Warning"] =
                        $"Reward processing failed for {failedCount} booking(s).";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // Delete
        // =====================================

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

            // Save information before deleting
            var flightNumber = flight.FlightNumber;
            var flightStatus = flight.FlightStatus;

            _repository.Delete(flight);

            var result = await _repository.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    nameof(FlightController),
                    "Delete",
                    nameof(Flight),
                    id.ToString(),
                    $"Deleted flight {flightNumber}. " +
                    $"Previous status: {flightStatus}"
                );

                TempData["Success"] =
                    "Flight deleted successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Failed to delete flight.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
