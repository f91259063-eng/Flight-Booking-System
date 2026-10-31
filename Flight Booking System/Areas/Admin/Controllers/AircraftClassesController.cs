using Microsoft.AspNetCore.Authorization;
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AircraftClassesController : Controller
    {
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly IRepository<Aircraft> _aircraftRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly ISeatService _seatService;

        public AircraftClassesController(
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Aircraft> aircraftRepository,
            IRepository<Class> classRepository,
            ISeatService seatService)
        {
            _aircraftClassRepository = aircraftClassRepository;
            _aircraftRepository = aircraftRepository;
            _classRepository = classRepository;
            _seatService = seatService;
        }

        // GET: Admin/AircraftClasses

        public async Task<IActionResult> Index()
        {
            var aircraftClasses =
                await _aircraftClassRepository.GetAllAsync(
                    includes:
                    [
                        x => x.Aircraft!,
                x => x.Class!
                    ]
                );

            return View(aircraftClasses);
        }
        // GET: Admin/AircraftClasses/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // POST: Admin/AircraftClasses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AircraftClass aircraftClass)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();

                return View(aircraftClass);
            }

            await _aircraftClassRepository.InsertAsync(aircraftClass);

            var result =
                await _aircraftClassRepository.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "تعذر حفظ فئة الطائرة."
                );

                await LoadDropdowns();

                return View(aircraftClass);
            }

            await _seatService.GenerateSeatsAsync(aircraftClass);

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.Aircrafts =
                await _aircraftRepository.GetAllAsync();

            ViewBag.Classes =
                await _classRepository.GetAllAsync();
        }
    }
}