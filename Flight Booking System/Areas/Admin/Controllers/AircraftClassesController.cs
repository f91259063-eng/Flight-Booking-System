
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

        // ==========================================
        // INDEX
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aircraftClasses =
                await _aircraftClassRepository.GetAllAsync(
                    includes:
                    [
                        x => x.Aircraft!,
                        x => x.Class!
                    ],
                    IsTracking: false);

            return View(aircraftClasses);
        }

        // ==========================================
        // CREATE GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        // ==========================================
        // CREATE POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AircraftClass aircraftClass)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 1. Validate aircraft.
            var aircraft = await _aircraftRepository.GetOneAsync(
                x => x.Id == aircraftClass.AircraftId,
                IsTracking: false);

            if (aircraft == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.AircraftId),
                    "The selected aircraft does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 2. Validate class.
            var selectedClass = await _classRepository.GetOneAsync(
                x => x.Id == aircraftClass.ClassId,
                IsTracking: false);

            if (selectedClass == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "The selected class does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 3. Prevent duplicate classes.
            var duplicateClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.AircraftId == aircraftClass.AircraftId
                      && x.ClassId == aircraftClass.ClassId,
                    IsTracking: false);

            if (duplicateClass != null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "This class has already been added to this aircraft.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 4. Validate seat count.
            if (aircraftClass.NumberOfSeats <= 0)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    "The number of seats must be greater than zero.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 5. Calculate allocated seats.
            var existingClasses =
                await _aircraftClassRepository.GetAllAsync(
                    filter: x => x.AircraftId == aircraftClass.AircraftId,
                    IsTracking: false);

            int allocatedSeats = existingClasses.Sum(
                x => x.NumberOfSeats);

            int remainingSeats = aircraft.Capacity - allocatedSeats;

            // 6. Check aircraft capacity.
            if (aircraftClass.NumberOfSeats > remainingSeats)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    $"This aircraft has only {remainingSeats} unallocated seats remaining.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 7. Insert aircraft class.
            await _aircraftClassRepository.InsertAsync(aircraftClass);

            var result = await _aircraftClassRepository.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the aircraft class.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 8. Generate seats.
            await _seatService.GenerateSeatsAsync(aircraftClass);

            // 9. Activity log.
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

        // ==========================================
        // EDIT GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var aircraftClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.ID == id,
                    includes:
                    [
                        x => x.Aircraft!,
                        x => x.Class!
                    ]);

            if (aircraftClass == null)
                return NotFound();

            await LoadDropdowns();

            return View(aircraftClass);
        }

        // ==========================================
        // EDIT POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            AircraftClass aircraftClass)
        {
            if (id != aircraftClass.ID)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 1. Get existing tracked record.
            var existingAircraftClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.ID == id);

            if (existingAircraftClass == null)
                return NotFound();

            // 2. Validate selected aircraft.
            var aircraft = await _aircraftRepository.GetOneAsync(
                x => x.Id == aircraftClass.AircraftId,
                IsTracking: false);

            if (aircraft == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.AircraftId),
                    "The selected aircraft does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 3. Validate selected class.
            var selectedClass = await _classRepository.GetOneAsync(
                x => x.Id == aircraftClass.ClassId,
                IsTracking: false);

            if (selectedClass == null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "The selected class does not exist.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 4. Prevent duplicate classes on the same aircraft.
            var duplicateClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.ID != id
                      && x.AircraftId == aircraftClass.AircraftId
                      && x.ClassId == aircraftClass.ClassId,
                    IsTracking: false);

            if (duplicateClass != null)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.ClassId),
                    "This class has already been added to this aircraft.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 5. Validate seat count.
            if (aircraftClass.NumberOfSeats <= 0)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    "The number of seats must be greater than zero.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 6. Calculate allocated seats excluding current record.
            var otherClasses =
                await _aircraftClassRepository.GetAllAsync(
                    filter: x =>
                        x.AircraftId == aircraftClass.AircraftId
                        && x.ID != id,
                    IsTracking: false);

            int allocatedSeats = otherClasses.Sum(
                x => x.NumberOfSeats);

            int remainingSeats = aircraft.Capacity - allocatedSeats;

            // 7. Validate aircraft capacity.
            if (aircraftClass.NumberOfSeats > remainingSeats)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    $"This aircraft has only {remainingSeats} seats available " +
                    "for this class.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 8. Synchronize seats safely.
            var seatResult = await _seatService.ResizeSeatsAsync(
                id,
                aircraftClass.NumberOfSeats);

            if (!seatResult.Success)
            {
                ModelState.AddModelError(
                    nameof(aircraftClass.NumberOfSeats),
                    seatResult.Error ?? "Unable to update aircraft seats.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 9. Update the existing tracked entity.
            existingAircraftClass.AircraftId = aircraftClass.AircraftId;
            existingAircraftClass.ClassId = aircraftClass.ClassId;
            existingAircraftClass.NumberOfSeats = aircraftClass.NumberOfSeats;

            _aircraftClassRepository.Update(existingAircraftClass);

            var result = await _aircraftClassRepository.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the aircraft class.");

                await LoadDropdowns();
                return View(aircraftClass);
            }

            // 10. Activity log.
            await _activityLogService.LogAsync(
                "AircraftClassesController",
                "Edit",
                "AircraftClass",
                id.ToString(),
                $"Updated aircraft class '{selectedClass.Name}' " +
                $"for aircraft '{aircraft.Model}'. " +
                $"New seat count: {aircraftClass.NumberOfSeats}."
            );

            TempData["Success"] =
                "Aircraft class was updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var aircraftClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.ID == id,
                    includes:
                    [
                        x => x.Aircraft!,
                        x => x.Class!
                    ],
                    IsTracking: false);

            if (aircraftClass == null)
                return NotFound();

            return View(aircraftClass);
        }

        // ==========================================
        // DELETE POST
        // ==========================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aircraftClass =
                await _aircraftClassRepository.GetOneAsync(
                    x => x.ID == id,
                    includes:
                    [
                        x => x.Aircraft!,
                        x => x.Class!
                    ]);

            if (aircraftClass == null)
                return NotFound();

            string className = aircraftClass.Class?.Name ?? "Unknown";
            string aircraftName =
                aircraftClass.Aircraft?.Model ?? "Unknown";

            // 1. Check tickets and stage seat deletion.
            var seatResult = await _seatService.RemoveSeatsAsync(id);

            if (!seatResult.Success)
            {
                TempData["Error"] = seatResult.Error;
                return RedirectToAction(nameof(Index));
            }

            // 2. Delete aircraft class.
            _aircraftClassRepository.Delete(aircraftClass);

            var result = await _aircraftClassRepository.CommitAsync();

            if (result <= 0)
            {
                TempData["Error"] =
                    "Unable to delete the aircraft class.";

                return RedirectToAction(nameof(Index));
            }

            // 3. Activity log.
            await _activityLogService.LogAsync(
                "AircraftClassesController",
                "Delete",
                "AircraftClass",
                id.ToString(),
                $"Deleted class '{className}' from aircraft '{aircraftName}'."
            );

            TempData["Success"] =
                $"Aircraft class '{className}' was deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // LOAD DROPDOWNS
        // ==========================================

        private async Task LoadDropdowns()
        {
            ViewBag.Aircrafts =
                await _aircraftRepository.GetAllAsync(
                    IsTracking: false);

            ViewBag.Classes =
                await _classRepository.GetAllAsync(
                    IsTracking: false);
        }
    }
}
