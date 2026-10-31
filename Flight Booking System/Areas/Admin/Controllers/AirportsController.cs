using Microsoft.AspNetCore.Authorization;

using Flight_Booking_System.Models;
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

        public AirportsController(
            IRepository<AirPort> airportRepository,
            IRepository<city> cityRepository,
            AirportCoordinateService coordinateService)
        {
            _airportRepository = airportRepository;
            _cityRepository = cityRepository;
            _coordinateService = coordinateService;
        }

        // عرض المطارات
        public async Task<IActionResult> Index()
        {
            var airports = await _airportRepository.GetAllAsync(
                includes: new System.Linq.Expressions.Expression<
                    Func<AirPort, object>>[]
                {
                    a => a.City
                });

            return View(airports);
        }

        // صفحة إضافة مطار
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCitiesAsync();
            return View();
        }

        // حفظ المطار وجلب إحداثياته
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AirPort airport,
            CancellationToken cancellationToken)
        {
            // لا نستقبل الإحداثيات من المستخدم
            ModelState.Remove(nameof(airport.Latitude));
            ModelState.Remove(nameof(airport.Longitude));
            airport.Latitude = null;
            airport.Longitude = null;

            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value!.Errors)
                    {
                        Console.WriteLine(
                            $"Field: {item.Key} | Error: {error.ErrorMessage}");
                    }
                }

                await  LoadCitiesAsync();
                return View(airport);
            }


            var city = await _cityRepository.GetOneAsync(
                c => c.Id == airport.cityId);

            if (city == null)
            {
                ModelState.AddModelError(
                    nameof(airport.cityId), "المدينة غير موجودة.");

                await LoadCitiesAsync();
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
                        "لم يتم العثور على إحداثيات مؤكدة للمطار. " +
                        "راجع اسم المطار والمدينة.");

                    await LoadCitiesAsync();
                    return View(airport);
                }

                airport.Latitude = coordinates.Latitude;
                airport.Longitude = coordinates.Longitude;

                await _airportRepository.InsertAsync(airport);

                var result = await _airportRepository.CommitAsync();

                if (result <= 0)
                {
                    ModelState.AddModelError(
                        string.Empty, "تعذر حفظ المطار في قاعدة البيانات.");

                    await LoadCitiesAsync();
                    return View(airport);
                }

                TempData["Success"] =
                    "تم حفظ المطار وإحداثياته بنجاح.";

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "تعذر الاتصال بخدمة الإحداثيات. حاول مرة أخرى.");

                await LoadCitiesAsync();
                return View(airport);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "انتهت مهلة الاتصال بخدمة الإحداثيات.");

                await LoadCitiesAsync();
                return View(airport);
            }
        }

        private async Task LoadCitiesAsync(int? selectedCityId = null)
        {
            var cities = await _cityRepository.GetAllAsync();

            ViewBag.Cities = new SelectList(
                cities,
                "Id",
                "Name",
                selectedCityId);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var airport = await _airportRepository.GetOneAsync(
                a => a.Id == id);

            if (airport == null)
                return NotFound();

            await LoadCitiesAsync(airport.cityId);

            return View(airport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AirPort airport)
        {
            if (id != airport.Id)
                return BadRequest();

            // الخاصية الملاحية اختيارية، ونستخدم cityId لتحديد المدينة.
            ModelState.Remove(nameof(AirPort.City));

            if (!ModelState.IsValid)
            {
                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            var existingAirport = await _airportRepository.GetOneAsync(
                a => a.Id == id);

            if (existingAirport == null)
                return NotFound();

            var city = await _cityRepository.GetOneAsync(
                c => c.Id == airport.cityId);

            if (city == null)
            {
                ModelState.AddModelError(
                    nameof(AirPort.cityId),
                    "المدينة المحددة غير موجودة.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            existingAirport.Name = airport.Name;
            existingAirport.cityId = airport.cityId;
            existingAirport.Latitude = airport.Latitude;
            existingAirport.Longitude = airport.Longitude;

            _airportRepository.Update(existingAirport);

            int result = await _airportRepository.CommitAsync();

            if (result < 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "حدث خطأ أثناء حفظ التعديلات.");

                await LoadCitiesAsync(airport.cityId);
                return View(airport);
            }

            TempData["Success"] = "تم تعديل بيانات المطار بنجاح.";

            return RedirectToAction(nameof(Index));
        }

    }
}