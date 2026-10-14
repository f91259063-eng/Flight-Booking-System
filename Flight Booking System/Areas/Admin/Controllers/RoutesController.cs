using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace Flight_Booking_System.Controllers
{

    [Area("Admin")]
    public class RoutesController : Controller
    {
        private readonly IRepository<Flight_Booking_System.Models.Route> _routeRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly DistanceCalculatorService _distanceCalculator;

        public RoutesController(
            IRepository<Flight_Booking_System.Models.Route> routeRepository,
            IRepository<AirPort> airportRepository,
            DistanceCalculatorService distanceCalculator)
        {
            _routeRepository = routeRepository;
            _airportRepository = airportRepository;
            _distanceCalculator = distanceCalculator;
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
                    "لا يمكن اختيار نفس المطار للمغادرة والوصول.");

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
                    "مطار المغادرة أو الوصول غير موجود.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            if (!HasCoordinates(fromAirport) ||
                !HasCoordinates(toAirport))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "يجب تسجيل إحداثيات المطارين أولاً.");

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
                    "هذا المسار مسجل بالفعل.");

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
                    "تعذر حفظ المسار في قاعدة البيانات.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View();
            }

            TempData["Success"] =
                $"تم إنشاء المسار بنجاح. المسافة: {route.Distance:F2} كم.";

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
                    "لا يمكن اختيار نفس المطار للمغادرة والوصول.");

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
                    "مطار المغادرة أو الوصول غير موجود.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            if (!HasCoordinates(fromAirport) ||
                !HasCoordinates(toAirport))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "يجب تسجيل إحداثيات المطارين أولاً.");

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
                    "يوجد مسار آخر بنفس مطار المغادرة والوصول.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            route.FromAirportId = FromAirportId;
            route.ToAirportId = ToAirportId;
            route.Distance = CalculateDistance(fromAirport, toAirport);

            _routeRepository.Update(route);

            int result = await _routeRepository.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "حدث خطأ أثناء تعديل المسار.");

                await LoadAirportsAsync(FromAirportId, ToAirportId);
                return View(route);
            }

            TempData["Success"] =
                $"تم تعديل المسار بنجاح. المسافة الجديدة: {route.Distance:F2} كم.";

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
                TempData["Error"] = "المسار غير موجود.";
                return RedirectToAction(nameof(Index));
            }

            _routeRepository.Delete(route);

            int result = await _routeRepository.CommitAsync();

            if (result < 0)
            {
                TempData["Error"] =
                    "تعذر حذف المسار. قد يكون مرتبطًا برحلات مسجلة.";
            }
            else
            {
                TempData["Success"] = "تم حذف المسار بنجاح.";
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