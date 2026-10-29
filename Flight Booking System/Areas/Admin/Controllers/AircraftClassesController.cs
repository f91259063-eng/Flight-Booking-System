
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Controllers
{
    [Area("Admin")]
    public class AircraftClassesController : Controller
    {
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly IRepository<Aircraft> _aircraftRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly ISeatService _seatService;
        private readonly IActivityLogService _activityLogService;

        public AircraftClassesController(
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Aircraft> aircraftRepository,
            IRepository<Class> classRepository,
            ISeatService seatService,
            IActivityLogService activityLogService)
        {
            _aircraftClassRepository = aircraftClassRepository;
            _aircraftRepository = aircraftRepository;
            _classRepository = classRepository;
            _seatService = seatService;
            _activityLogService = activityLogService;
        }

        // GET: Admin/AircraftClasses
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aircraftClasses =
                await _aircraftClassRepository.GetAllAsync(
                    includes:
                    [
                        x => x.Aircraft!,
                        x => x.Class!
                    ]);

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

            // 1. Verify that the aircraft exists.
            var aircraft = await _aircraftRepository.GetOneAsync(
                x => x.Id == aircraftClass.AircraftId);

            if (aircraft == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.AircraftId),
                    "The selected aircraft does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 2. Verify that the class exists.
            var selectedClass = await _classRepository.GetOneAsync(
                x => x.Id == aircraftClass.ClassId);

            if (selectedClass == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "The selected class does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 3. Prevent duplicate classes on the same aircraft.
            var duplicateClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.AircraftId == aircraftClass.AircraftId
                      && x.ClassId == aircraftClass.ClassId);

            if (duplicateClass != null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "This class has already been added to this aircraft.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 4. Validate the seat count.
            if (aircraftClass.NumberOfSeats <= 0)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    "The number of seats must be greater than zero.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 5. Get all classes already assigned to this aircraft.
            var existingClasses =
                await _aircraftClassRepository.GetAllAsync(
                    filter: x => x.AircraftId == aircraftClass.AircraftId);

            var allocatedSeats = existingClasses.Sum(x => x.NumberOfSeats);

            var remainingSeats = aircraft.Capacity - allocatedSeats;

            // 6. Do not allow the aircraft capacity to be exceeded.
            if (aircraftClass.NumberOfSeats > remainingSeats)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    $"This aircraft has only {remainingSeats} unallocated seats remaining. " +
                    $"Its total capacity is {aircraft.Capacity} seats, " +
                    $"and {allocatedSeats} seats have already been allocated.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 7. Save the aircraft class.
            await _aircraftClassRepository.InsertAsync(aircraftClass);

            var result =
                await _aircraftClassRepository.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the aircraft class.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 8. Generate seats for this aircraft class.
            await _seatService.GenerateSeatsAsync(aircraftClass);

            // 9. Record the successful operation.
            await _activityLogService.LogAsync(
                "AircraftClassesController",
                "Create",
                "AircraftClass",
                aircraftClass.ID.ToString(),
                $"Added class '{selectedClass.Name}' to aircraft " +
                $"'{aircraft.Model}' with {aircraftClass.NumberOfSeats} seats. " +
                $"Allocated seats: {allocatedSeats + aircraftClass.NumberOfSeats} " +
                $"of {aircraft.Capacity}."
            );

            TempData["Success"] =
                $"Class '{selectedClass.Name}' was added successfully. " +
                $"{remainingSeats - aircraftClass.NumberOfSeats} seats remain unallocated.";

            return RedirectToAction(nameof(Index));
        }

        // Load dropdown lists
        private async Task LoadDropdowns()
        {
            ViewBag.Aircrafts =
                await _aircraftRepository.GetAllAsync();

            ViewBag.Classes =
                await _classRepository.GetAllAsync();
        }
    }
}
