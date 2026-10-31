using Microsoft.AspNetCore.Authorization;
﻿
using System.Linq.Expressions;

using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Flight_Booking_System.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AirportsController : Controller
    {
        private readonly IRepository<AirPort> _airportRepository;
        private readonly IRepository<city> _cityRepository;
        private readonly AirportCoordinateService _coordinateService;
        private readonly IActivityLogService _activityLogService;

        public AirportsController(
            IRepository<AirPort> airportRepository,
            IRepository<city> cityRepository,
            AirportCoordinateService coordinateService,
            IActivityLogService activityLogService)
        {
            _airportRepository = airportRepository;
            _cityRepository = cityRepository;
            _coordinateService = coordinateService;
            _activityLogService = activityLogService;
        }

        // Display all airports
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var airports = await _airportRepository.GetAllAsync(
                includes: new Expression<Func<AirPort, object>>[]
                {
                    a => a.City
                });

            return View(airports);
        }

        // Display Create page
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCitiesAsync();
            return View();
        }

        // Create airport and retrieve its coordinates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AirPort airport,
            CancellationToken cancellationToken)
        {
            ModelState.Remove(nameof(airport.Latitude));
            ModelState.Remove(nameof(airport.Longitude));

            airport.Latitude = null;
            airport.Longitude = null;

            if (!ModelState.IsValid)
            {
                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            var city = await _cityRepository.GetOneAsync(
                c => c.Id == airport.cityId);

            if (city == null)
            {
                ModelState.AddModelError(
                    nameof(airport.cityId),
                    "The selected city does not exist.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            try
            {
                var coordinates =
                    await _coordinateService.GetCoordinatesAsync(
                        airport.Name,
                        city.Name,
                        cancellationToken);

                if (coordinates == null)
                {
                    ModelState.AddModelError(
                        nameof(airport.Name),
                        "Confirmed coordinates could not be found. " +
                        "Please check the airport name and city.");

                    await LoadCitiesAsync(airport.cityId);
                    return View(airport);
                }

                airport.Latitude = coordinates.Latitude;
                airport.Longitude = coordinates.Longitude;

                await _airportRepository.InsertAsync(airport);

                var result = await _airportRepository.CommitAsync();

                if (result <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Unable to save the airport to the database.");

                    await LoadCitiesAsync(airport.cityId);
                    return View(airport);
                }

                await _activityLogService.LogAsync(
                    "AirportsController",
                    "Create",
                    "AirPort",
                    airport.Id.ToString(),
                    $"Created airport: {airport.Name} in city: {city.Name}"
                );

                TempData["Success"] =
                    "The airport and its coordinates were saved successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to connect to the coordinate service. Please try again.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The connection to the coordinate service timed out.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }
        }

        // Display Edit page
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var airport = await _airportRepository.GetOneAsync(
                a => a.Id == id);

            if (airport == null)
            {
                return NotFound();
            }

            await LoadCitiesAsync(airport.cityId);

            return View(airport);
        }

        // Update airport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AirPort airport)
        {
            if (id != airport.Id)
            {
                return BadRequest();
            }

            ModelState.Remove(nameof(AirPort.City));

            if (!ModelState.IsValid)
            {
                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            var existingAirport = await _airportRepository.GetOneAsync(
                a => a.Id == id);

            if (existingAirport == null)
            {
                return NotFound();
            }

            var city = await _cityRepository.GetOneAsync(
                c => c.Id == airport.cityId);

            if (city == null)
            {
                ModelState.AddModelError(
                    nameof(AirPort.cityId),
                    "The selected city does not exist.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            existingAirport.Name = airport.Name;
            existingAirport.cityId = airport.cityId;
            existingAirport.Latitude = airport.Latitude;
            existingAirport.Longitude = airport.Longitude;

            _airportRepository.Update(existingAirport);

            var result = await _airportRepository.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while saving the changes.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            await _activityLogService.LogAsync(
                "AirportsController",
                "Edit",
                "AirPort",
                existingAirport.Id.ToString(),
                $"Updated airport: {existingAirport.Name} in city: {city.Name}"
            );

            TempData["Success"] = "Airport information updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Display Delete confirmation page
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var airport = await _airportRepository.GetOneAsync(
                a => a.Id == id,
                includes: new Expression<Func<AirPort, object>>[]
                {
                    a => a.City
                });

            if (airport == null)
            {
                return NotFound();
            }

            return View(airport);
        }

        // Delete airport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var airport = await _airportRepository.GetOneAsync(
                a => a.Id == id,
                includes: new Expression<Func<AirPort, object>>[]
                {
                    a => a.City
                });

            if (airport == null)
            {
                TempData["Error"] = "The airport was not found.";
                return RedirectToAction(nameof(Index));
            }

            var airportId = airport.Id.ToString();
            var airportName = airport.Name;
            var cityName = airport.City?.Name ?? "Unknown";

            try
            {
                _airportRepository.Delete(airport);

                var result = await _airportRepository.CommitAsync();

                if (result <= 0)
                {
                    TempData["Error"] =
                        "Unable to delete the airport. It may be referenced by another record.";

                    return RedirectToAction(nameof(Index));
                }

                await _activityLogService.LogAsync(
                    "AirportsController",
                    "Delete",
                    "AirPort",
                    airportId,
                    $"Deleted airport: {airportName} in city: {cityName}"
                );

                TempData["Success"] = "Airport deleted successfully.";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                TempData["Error"] =
                    "Unable to delete this airport because it is referenced by another record.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Load cities for dropdown lists
        private async Task LoadCitiesAsync(int? selectedCityId = null)
        {
            var cities = await _cityRepository.GetAllAsync();

            ViewBag.Cities = new SelectList(
                cities,
                "Id",
                "Name",
                selectedCityId);
        }
    }
}
