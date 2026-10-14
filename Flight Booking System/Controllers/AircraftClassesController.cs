
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Controllers
{
    public class AircraftClassesController : Controller
    {
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly ISeatService _seatService;

        public AircraftClassesController(
            IRepository<AircraftClass> aircraftClassRepository,
            ISeatService seatService)
        {
            _aircraftClassRepository = aircraftClassRepository;
            _seatService = seatService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AircraftClass aircraftClass)
        {
            if (!ModelState.IsValid)
            {
                return View(aircraftClass);
            }

            // 1. Save AircraftClass
            _aircraftClassRepository.InsertAsync(aircraftClass);
            _aircraftClassRepository.CommitAsync();

            // 2. Generate seats automatically
            await _seatService.GenerateSeatsAsync(aircraftClass);

            // 3. Return to Index
            return RedirectToAction(nameof(Index));
        }
    }
}
