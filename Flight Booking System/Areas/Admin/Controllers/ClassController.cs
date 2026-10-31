using Microsoft.AspNetCore.Authorization;

using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;

using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ClassController : Controller
    {
        private readonly IRepository<Class> _repositoryclass;
        private readonly IActivityLogService _activityLogService;

        public ClassController(
            IRepository<Class> repository,
            IActivityLogService activityLogService)
        {
            _repositoryclass = repository;
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var classes = await _repositoryclass.GetAllAsync();

            return View(classes);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Class classModel)
        {
            if (!ModelState.IsValid)
                return View(classModel);

            await _repositoryclass.InsertAsync(classModel);

            var result = await _repositoryclass.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to save the class.");

                return View(classModel);
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Class",
                "Create",
                "Class",
                classModel.Id.ToString(),
                $"Created class: {classModel.Name}"
            );

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var classModel = await _repositoryclass
                .GetOneAsync(x => x.Id == id);

            if (classModel == null)
                return NotFound();

            return View(classModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Class classModel)
        {
            if (id != classModel.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(classModel);

            var existingClass = await _repositoryclass
                .GetOneAsync(x => x.Id == id);

            if (existingClass == null)
                return NotFound();

            existingClass.Name = classModel.Name;

            var result = await _repositoryclass.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to update the class.");

                return View(classModel);
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Class",
                "Edit",
                "Class",
                existingClass.Id.ToString(),
                $"Updated class: {existingClass.Name}"
            );

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var classModel = await _repositoryclass
                .GetOneAsync(x => x.Id == id);

            if (classModel == null)
                return NotFound();

            return View(classModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classModel = await _repositoryclass
                .GetOneAsync(x => x.Id == id);

            if (classModel == null)
                return NotFound();

            _repositoryclass.Delete(classModel);

            var result = await _repositoryclass.CommitAsync();

            if (result < 0)
            {
                return BadRequest(
                    "Unable to delete the class. It may be referenced by another record.");
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Class",
                "Delete",
                "Class",
                classModel.Id.ToString(),
                $"Deleted class: {classModel.Name}"
            );

            return RedirectToAction(nameof(Index));
        }
    }
}