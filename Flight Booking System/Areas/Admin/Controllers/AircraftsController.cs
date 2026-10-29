using System.Linq.Expressions;

using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AircraftController : Controller
    {
        private readonly IRepository<Aircraft> _repositoryAircraft;
        private readonly IRepository<Airline> _repositoryAirline;
        private readonly IRepository<AircraftClass> _repositoryAircraftClass;
        private readonly IRepository<Seat> _repositorySeat;
        private readonly IActivityLogService _activityLogService;

        public AircraftController(
            IRepository<Aircraft> repositoryAircraft,
            IRepository<Airline> repositoryAirline,
            IRepository<AircraftClass> repositoryAircraftClass,
            IRepository<Seat> repositorySeat,
            IActivityLogService activityLogService)
        {
            _repositoryAircraft = repositoryAircraft;
            _repositoryAirline = repositoryAirline;
            _repositoryAircraftClass = repositoryAircraftClass;
            _repositorySeat = repositorySeat;
            _activityLogService = activityLogService;
        }

        // GET: Admin/Aircraft
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aircrafts = await _repositoryAircraft.GetAllAsync(
                includes: new Expression<Func<Aircraft, object>>[]
                {
                    x => x.Airline!
                });

            return View(aircrafts);
        }

        // GET: Admin/Aircraft/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var aircraft = await _repositoryAircraft.GetOneAsync(
                x => x.Id == id,
                new Expression<Func<Aircraft, object>>[]
                {
                    x => x.Airline!
                });

            if (aircraft == null)
                return NotFound();

            var aircraftClasses = await _repositoryAircraftClass.GetAllAsync(
                x => x.AircraftId == id,
                new Expression<Func<AircraftClass, object>>[]
                {
                    x => x.Class!
                });

            var model = new AircraftSeatsViewModel
            {
                Id = aircraft.Id,
                RegistrationNumber = aircraft.RegistrationNumber,
                Model = aircraft.Model,
                Capacity = aircraft.Capacity,
                AirlineName = aircraft.Airline?.Name ?? "Not specified"
            };

            foreach (var aircraftClass in aircraftClasses)
            {
                var seats = await _repositorySeat.GetAllAsync(
                    x => x.AircraftClassId == aircraftClass.ID);

                model.Classes.Add(new AircraftClassSeatsViewModel
                {
                    AircraftClassId = aircraftClass.ID,
                    ClassName = aircraftClass.Class?.Name ?? "Not specified",
                    NumberOfSeats = aircraftClass.NumberOfSeats,
                    Seats = seats
                        .OrderBy(s => s.SeatNumber)
                        .ToList()
                });
            }

            return View(model);
        }

        // GET: Admin/Aircraft/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
            return View();
        }

        // POST: Admin/Aircraft/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aircraft aircraft)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
                return View(aircraft);
            }

            await _repositoryAircraft.InsertAsync(aircraft);

            var result = await _repositoryAircraft.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the aircraft.");

                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
                return View(aircraft);
            }

            await _activityLogService.LogAsync(
                "AircraftController",
                "Create",
                "Aircraft",
                aircraft.Id.ToString(),
                $"Created aircraft: {aircraft.Model}, " +
                $"Registration: {aircraft.RegistrationNumber}, " +
                $"Capacity: {aircraft.Capacity}."
            );

            TempData["Success"] = "Aircraft created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Aircraft/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var aircraft = await _repositoryAircraft.GetOneAsync(
                x => x.Id == id);

            if (aircraft == null)
                return NotFound();

            ViewBag.Airlines = await _repositoryAirline.GetAllAsync();

            return View(aircraft);
        }

        // POST: Admin/Aircraft/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Aircraft aircraft)
        {
            if (id != aircraft.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
                return View(aircraft);
            }

            var existingAircraft = await _repositoryAircraft.GetOneAsync(
                x => x.Id == id);

            if (existingAircraft == null)
                return NotFound();

            // Prevent capacity from becoming smaller than allocated seats.
            var aircraftClasses = await _repositoryAircraftClass.GetAllAsync(
                x => x.AircraftId == id);

            var allocatedSeats = aircraftClasses.Sum(x => x.NumberOfSeats);

            if (aircraft.Capacity < allocatedSeats)
            {
                ModelState.AddModelError(
                    nameof(aircraft.Capacity),
                    $"Capacity cannot be less than the {allocatedSeats} " +
                    "seats already allocated to this aircraft.");

                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
                return View(aircraft);
            }

            existingAircraft.AirlineId = aircraft.AirlineId;
            existingAircraft.RegistrationNumber = aircraft.RegistrationNumber;
            existingAircraft.Model = aircraft.Model;
            existingAircraft.Capacity = aircraft.Capacity;

            var result = await _repositoryAircraft.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the aircraft.");

                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();
                return View(aircraft);
            }

            await _activityLogService.LogAsync(
                "AircraftController",
                "Edit",
                "Aircraft",
                existingAircraft.Id.ToString(),
                $"Updated aircraft: {existingAircraft.Model}, " +
                $"Registration: {existingAircraft.RegistrationNumber}, " +
                $"Capacity: {existingAircraft.Capacity}."
            );

            TempData["Success"] = "Aircraft updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Aircraft/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var aircraft = await _repositoryAircraft.GetOneAsync(
                x => x.Id == id);

            if (aircraft == null)
                return NotFound();

            return View(aircraft);
        }

        // POST: Admin/Aircraft/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aircraft = await _repositoryAircraft.GetOneAsync(
                x => x.Id == id);

            if (aircraft == null)
                return NotFound();

            // Save details before deletion for the audit log.
            var aircraftId = aircraft.Id.ToString();
            var aircraftModel = aircraft.Model;
            var registrationNumber = aircraft.RegistrationNumber;

            try
            {
                _repositoryAircraft.Delete(aircraft);

                var result = await _repositoryAircraft.CommitAsync();

                if (result <= 0)
                {
                    TempData["Error"] =
                        "Unable to delete the aircraft. It may be referenced by other records.";

                    return RedirectToAction(nameof(Index));
                }

                await _activityLogService.LogAsync(
                    "AircraftController",
                    "Delete",
                    "Aircraft",
                    aircraftId,
                    $"Deleted aircraft: {aircraftModel}, " +
                    $"Registration: {registrationNumber}."
                );

                TempData["Success"] = "Aircraft deleted successfully.";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                TempData["Error"] =
                    "Unable to delete the aircraft because it is referenced by other records.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
