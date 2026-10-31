
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AirlineController : Controller
    {
        private readonly IRepository<Airline> _repositoryairline;
        private readonly IActivityLogService _activityLogService;

        public AirlineController(
            IRepository<Airline> repository,
            IActivityLogService activityLogService)
        {
            _repositoryairline = repository;
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var airlines = await _repositoryairline.GetAllAsync();

            return View(airlines);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airline airline)
        {
            if (!ModelState.IsValid)
            {
                return View(airline);
            }

            await _repositoryairline.InsertAsync(airline);

            var result = await _repositoryairline.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "AirlineController",
                    "Create",
                    "Airline",
                    airline.Id.ToString(),
                    $"Created airline: {airline.Name}"
                );

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(
                "",
                "Unable to create the airline."
            );

            return View(airline);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var airline = await _repositoryairline
                .GetOneAsync(x => x.Id == id);

            if (airline == null)
            {
                return NotFound();
            }

            return View(airline);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Airline airline)
        {
            if (id != airline.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(airline);
            }

            var existingAirline = await _repositoryairline
                .GetOneAsync(x => x.Id == id);

            if (existingAirline == null)
            {
                return NotFound();
            }

            existingAirline.Name = airline.Name;
            existingAirline.AirlineCode = airline.AirlineCode;
            existingAirline.Email = airline.Email;
            existingAirline.Phone = airline.Phone;

            var result = await _repositoryairline.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "AirlineController",
                    "Edit",
                    "Airline",
                    existingAirline.Id.ToString(),
                    $"Updated airline: {existingAirline.Name}"
                );

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(
                "",
                "Unable to update the airline."
            );

            return View(airline);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var airline = await _repositoryairline
                .GetOneAsync(x => x.Id == id);

            if (airline == null)
            {
                return NotFound();
            }

            return View(airline);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var airline = await _repositoryairline
                .GetOneAsync(x => x.Id == id);

            if (airline == null)
            {
                return NotFound();
            }

            var airlineId = airline.Id.ToString();
            var airlineName = airline.Name;

            _repositoryairline.Delete(airline);

            var result = await _repositoryairline.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "AirlineController",
                    "Delete",
                    "Airline",
                    airlineId,
                    $"Deleted airline: {airlineName}"
                );

                return RedirectToAction(nameof(Index));
            }

            return BadRequest(
                "Unable to delete the airline. It may be referenced by another record."
            );
        }
    }
}
