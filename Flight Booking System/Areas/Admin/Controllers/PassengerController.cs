
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PassengerController : Controller
    {
        private readonly IRepository<Passenger> _repositoryPassenger;
        private readonly IActivityLogService _activityLogService;

        public PassengerController(
            IRepository<Passenger> repository,
            IActivityLogService activityLogService)
        {
            _repositoryPassenger = repository;
            _activityLogService = activityLogService;
        }

        // GET: Admin/Passenger
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var passengers = await _repositoryPassenger.GetAllAsync();

            return View(passengers);
        }

        // GET: Admin/Passenger/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Passenger/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Passenger passenger)
        {
            if (!ModelState.IsValid)
                return View(passenger);

            await _repositoryPassenger.InsertAsync(passenger);

            var result = await _repositoryPassenger.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to save passenger information.");

                return View(passenger);
            }

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Create),
                nameof(Passenger),
                passenger.Id.ToString(),
                $"Created passenger: {passenger.FirstName} {passenger.LastName}."
            );

            TempData["Success"] = "Passenger created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Passenger/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            return View(passenger);
        }

        // POST: Admin/Passenger/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Passenger passenger)
        {
            if (id != passenger.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(passenger);

            var existingPassenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (existingPassenger == null)
                return NotFound();

            existingPassenger.FirstName = passenger.FirstName;
            existingPassenger.MiddleName = passenger.MiddleName;
            existingPassenger.LastName = passenger.LastName;
            existingPassenger.PassportNumber = passenger.PassportNumber;
            existingPassenger.NationalID = passenger.NationalID;
            existingPassenger.Phone = passenger.Phone;
            existingPassenger.Email = passenger.Email;
            existingPassenger.DateOfBirth = passenger.DateOfBirth;

            var result = await _repositoryPassenger.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update passenger information.");

                return View(passenger);
            }

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Edit),
                nameof(Passenger),
                existingPassenger.Id.ToString(),
                $"Updated passenger: {existingPassenger.FirstName} {existingPassenger.LastName}."
            );

            TempData["Success"] = "Passenger updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Passenger/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            return View(passenger);
        }

        // POST: Admin/Passenger/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var passenger = await _repositoryPassenger
                .GetOneAsync(x => x.Id == id);

            if (passenger == null)
                return NotFound();

            var passengerId = passenger.Id.ToString();

            var passengerName =
                $"{passenger.FirstName} {passenger.LastName}";

            _repositoryPassenger.Delete(passenger);

            var result = await _repositoryPassenger.CommitAsync();

            if (result <= 0)
            {
                await _activityLogService.LogAsync(
                    nameof(PassengerController),
                    nameof(Delete),
                    nameof(Passenger),
                    passengerId,
                    $"Failed to delete passenger: {passengerName}."
                );

                TempData["Error"] =
                    "Unable to delete the passenger. The passenger may be linked to existing records.";

                return RedirectToAction(nameof(Index));
            }

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Delete),
                nameof(Passenger),
                passengerId,
                $"Deleted passenger: {passengerName}."
            );

            TempData["Success"] = "Passenger deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
