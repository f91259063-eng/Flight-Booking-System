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

        // GET: AircraftClasses
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aircraftClasses =
                await _aircraftClassRepository.GetAllAsync();

            return View(aircraftClasses);
        }

        // GET: AircraftClasses/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: AircraftClasses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AircraftClass aircraftClass)
        {
            if (!ModelState.IsValid)
            {
                return View(aircraftClass);
            }

            await _aircraftClassRepository.InsertAsync(aircraftClass);

            await _aircraftClassRepository.CommitAsync();

            await _seatService.GenerateSeatsAsync(aircraftClass);

            return RedirectToAction(nameof(Index));
        }
    }
}