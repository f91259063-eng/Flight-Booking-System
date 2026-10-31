using Microsoft.AspNetCore.Authorization;
﻿

using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CityController : Controller
    {
        private readonly IRepository<city> _repositorycity;
        private readonly IActivityLogService _activityLogService;

        public CityController(
            IRepository<city> repositorycity,
            IActivityLogService activityLogService)
        {
            _repositorycity = repositorycity;
            _activityLogService = activityLogService;
        }

        // GET: Admin/City
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cities = await _repositorycity.GetAllAsync();

            return View(cities);
        }

        // GET: Admin/City/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/City/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(city city)
        {
            if (!ModelState.IsValid)
            {
                return View(city);
            }

            await _repositorycity.InsertAsync(city);
            var result = await _repositorycity.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "City",
                    "Create",
                    "City",
                    city.Id.ToString(),
                    $"Created city: {city.Name}"
                );

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(
                "",
                "Unable to save the city."
            );

            return View(city);
        }

        // GET: Admin/City/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/City/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, city city)
        {
            if (id != city.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(city);
            }

            var existingCity = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (existingCity == null)
            {
                return NotFound();
            }

            existingCity.Name = city.Name;

            var result = await _repositorycity.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "City",
                    "Edit",
                    "City",
                    existingCity.Id.ToString(),
                    $"Updated city: {existingCity.Name}"
                );

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(
                "",
                "Unable to update the city."
            );

            return View(city);
        }

        // GET: Admin/City/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Admin/City/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var city = await _repositorycity
                .GetOneAsync(x => x.Id == id);

            if (city == null)
            {
                return NotFound();
            }

            // Keep the details before deleting the entity.
            var cityId = city.Id.ToString();
            var cityName = city.Name;

            _repositorycity.Delete(city);

            var result = await _repositorycity.CommitAsync();

            if (result > 0)
            {
                await _activityLogService.LogAsync(
                    "City",
                    "Delete",
                    "City",
                    cityId,
                    $"Deleted city: {cityName}"
                );

                return RedirectToAction(nameof(Index));
            }

            return BadRequest(
                "Unable to delete the city. It may be referenced by another record."
            );
        }
    }
}
