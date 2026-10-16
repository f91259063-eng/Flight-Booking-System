


using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AircraftController : Controller
    {
        private readonly IRepository<Aircraft> _repositoryAircraft;
        private readonly IRepository<Airline> _repositoryAirline;

        public AircraftController(
            IRepository<Aircraft> repositoryAircraft,
            IRepository<Airline> repositoryAirline)
        {
            _repositoryAircraft = repositoryAircraft;
            _repositoryAirline = repositoryAirline;
        }

        // GET: Admin/Aircraft
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aircrafts = await _repositoryAircraft.GetAllAsync();

            return View(aircrafts);
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

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "تعذر حفظ بيانات الطائرة."
                );

                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();

                return View(aircraft);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Aircraft/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var aircraft = await _repositoryAircraft
                .GetOneAsync(x => x.Id == id);

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

            var existingAircraft = await _repositoryAircraft
                .GetOneAsync(x => x.Id == id);

            if (existingAircraft == null)
                return NotFound();

            existingAircraft.AirlineId = aircraft.AirlineId;
            existingAircraft.RegistrationNumber = aircraft.RegistrationNumber;
            existingAircraft.Model = aircraft.Model;
            existingAircraft.Capacity = aircraft.Capacity;

            var result = await _repositoryAircraft.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "تعذر تحديث بيانات الطائرة."
                );

                ViewBag.Airlines = await _repositoryAirline.GetAllAsync();

                return View(aircraft);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Aircraft/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var aircraft = await _repositoryAircraft
                .GetOneAsync(x => x.Id == id);

            if (aircraft == null)
                return NotFound();

            return View(aircraft);
        }

        // POST: Admin/Aircraft/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aircraft = await _repositoryAircraft
                .GetOneAsync(x => x.Id == id);

            if (aircraft == null)
                return NotFound();

            _repositoryAircraft.Delete(aircraft);

            var result = await _repositoryAircraft.CommitAsync();

            if (result < 0)
            {
                return BadRequest(
                    "تعذر حذف الطائرة، قد تكون مرتبطة بسجلات أخرى."
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
