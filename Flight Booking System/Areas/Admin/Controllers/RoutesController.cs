
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
    public class RoutesController : Controller
    {
        private readonly IRepository<Flight_Booking_System.Models.Route> _routeRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly DistanceCalculatorService _distanceCalculator;
        private readonly IActivityLogService _activityLogService;

        public RoutesController(
            IRepository<Flight_Booking_System.Models.Route> routeRepository,
            IRepository<AirPort> airportRepository,
            DistanceCalculatorService distanceCalculator,
            IActivityLogService activityLogService)
        {
            _routeRepository = routeRepository;
            _airportRepository = airportRepository;
            _distanceCalculator = distanceCalculator;
            _activityLogService = activityLogService;
        }

        // GET: Routes
        public async Task<IActionResult> Index()
        {
            Expression<Func<Flight_Booking_System.Models.Route, object>>[] includes =
            {
                r => r.FromAirport,
                r => r.ToAirport
            };

            var routes = await _routeRepository.GetAllAsync(
                includes: includes,
                IsTracking: false);

            return View(routes);
        }

        // GET: Routes/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadAirportsAsync();
            return View();
        }

        // POST: Routes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int FromAirportId,
            int ToAirportId)
        {
            if (FromAirportId == ToAirportId)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Departure and destination airports cannot be the same.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            var fromAirport = await _airportRepository.GetOneAsync(
                a => a.Id == FromAirportId);

            var toAirport = await _airportRepository.GetOneAsync(
                a => a.Id == ToAirportId);

            if (fromAirport == null || toAirport == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Departure or destination airport was not found.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            if (!HasCoordinates(fromAirport) ||
                !HasCoordinates(toAirport))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Both airports must have coordinates.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            var duplicate = await _routeRepository.GetOneAsync(
                r => r.FromAirportId == FromAirportId &&
                     r.ToAirportId == ToAirportId);

            if (duplicate != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This route already exists.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            var route = new Flight_Booking_System.Models.Route
            {
                FromAirportId = FromAirportId,
                ToAirportId = ToAirportId,
                Distance = CalculateDistance(fromAirport, toAirport)
            };

            await _routeRepository.InsertAsync(route);

            int result = await _routeRepository.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save the route.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            // Activity Log
            await _activityLogService.LogAsync(
                nameof(RoutesController),
                nameof(Create),
                nameof(Flight_Booking_System.Models.Route),
                route.Id.ToString(),
                $"Created route from {fromAirport.Name} to {toAirport.Name}. " +
                $"Distance: {route.Distance:F2} km."
            );

            TempData["Success"] =
                $"Route created successfully. Distance: {route.Distance:F2} km.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Routes/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var route = await _routeRepository.GetOneAsync(
                r => r.Id == id);

            if (route == null)
                return NotFound();

            await LoadAirportsAsync(
                route.FromAirportId,
                route.ToAirportId);

            return View(route);
        }

        // POST: Routes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int FromAirportId,
            int ToAirportId)
        {
            if (id <= 0)
                return BadRequest();

            var route = await _routeRepository.GetOneAsync(
                r => r.Id == id);

            if (route == null)
                return NotFound();

            if (FromAirportId == ToAirportId)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Departure and destination airports cannot be the same.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            var fromAirport = await _airportRepository.GetOneAsync(
                a => a.Id == FromAirportId);

            var toAirport = await _airportRepository.GetOneAsync(
                a => a.Id == ToAirportId);

            if (fromAirport == null || toAirport == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Departure or destination airport was not found.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            if (!HasCoordinates(fromAirport) ||
                !HasCoordinates(toAirport))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Both airports must have coordinates.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            var duplicate = await _routeRepository.GetOneAsync(
                r => r.FromAirportId == FromAirportId &&
                     r.ToAirportId == ToAirportId &&
                     r.Id != id);

            if (duplicate != null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Another route with the same airports already exists.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            route.FromAirportId = FromAirportId;
            route.ToAirportId = ToAirportId;
            route.Distance = CalculateDistance(fromAirport, toAirport);

            _routeRepository.Update(route);

            int result = await _routeRepository.CommitAsync();

            if (result <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the route.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            // Activity Log
            await _activityLogService.LogAsync(
                nameof(RoutesController),
                nameof(Edit),
                nameof(Flight_Booking_System.Models.Route),
                route.Id.ToString(),
                $"Updated route ID {route.Id}: " +
                $"From {fromAirport.Name} to {toAirport.Name}. " +
                $"New distance: {route.Distance:F2} km."
            );

            TempData["Success"] =
                $"Route updated successfully. New distance: {route.Distance:F2} km.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Routes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var route = await _routeRepository.GetOneAsync(
                r => r.Id == id);

            if (route == null)
            {
                TempData["Error"] = "Route not found.";
                return RedirectToAction(nameof(Index));
            }

            int routeId = route.Id;
            int fromAirportId = route.FromAirportId;
            int toAirportId = route.ToAirportId;

            _routeRepository.Delete(route);

            int result = await _routeRepository.CommitAsync();

            if (result <= 0)
            {
                TempData["Error"] =
                    "Unable to delete the route. It may be associated with existing flights.";
            }
            else
            {
                // Activity Log
                await _activityLogService.LogAsync(
                    nameof(RoutesController),
                    nameof(Delete),
                    nameof(Flight_Booking_System.Models.Route),
                    routeId.ToString(),
                    $"Deleted route ID {routeId}. " +
                    $"From Airport ID: {fromAirportId}, " +
                    $"To Airport ID: {toAirportId}."
                );

                TempData["Success"] = "Route deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private double CalculateDistance(
            AirPort fromAirport,
            AirPort toAirport)
        {
            return _distanceCalculator.CalculateDistance(
                fromAirport.Latitude!.Value,
                fromAirport.Longitude!.Value,
                toAirport.Latitude!.Value,
                toAirport.Longitude!.Value);
        }

        private bool HasCoordinates(AirPort airport)
        {
            return airport.Latitude.HasValue &&
                   airport.Longitude.HasValue;
        }

        private async Task LoadAirportsAsync(
            int? selectedFrom = null,
            int? selectedTo = null)
        {
            var airports = await _airportRepository.GetAllAsync(
                IsTracking: false);

            ViewBag.FromAirports = new SelectList(
                airports,
                "Id",
                "Name",
                selectedFrom);

            ViewBag.ToAirports = new SelectList(
                airports,
                "Id",
                "Name",
                selectedTo);
        }
    }
}
