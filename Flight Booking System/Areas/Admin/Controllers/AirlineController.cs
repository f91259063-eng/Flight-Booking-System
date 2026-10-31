using Microsoft.AspNetCore.Authorization;

using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AirlineController : Controller
    {
        private readonly IRepository<Airline> _repositoryairline;

        public AirlineController(IRepository<Airline> repository)
        {
            _repositoryairline = repository;
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
            if (ModelState.IsValid)
            {
                await _repositoryairline.InsertAsync(airline);
                await _repositoryairline.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

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

            await _repositoryairline.CommitAsync();

            return RedirectToAction(nameof(Index));
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

            _repositoryairline.Delete(airline);
            await _repositoryairline.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
