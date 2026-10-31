using Microsoft.AspNetCore.Authorization;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Passenger> _passengerRepository;
        private readonly IRepository<Aircraft> _aircraftRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly IRepository< Flight_Booking_System.Models.Route> _routeRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<city> _cityRepository;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRepository<Flight> flightRepository,
            IRepository<Booking> bookingRepository,
            IRepository<Passenger> passengerRepository,
            IRepository<Aircraft> aircraftRepository,
            IRepository<AirPort> airportRepository,
            IRepository< Flight_Booking_System.Models.Route> routeRepository,
            IRepository<Seat> seatRepository,
            IRepository<Payment> paymentRepository,
            IRepository<city> cityRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;

            _flightRepository = flightRepository;
            _bookingRepository = bookingRepository;
            _passengerRepository = passengerRepository;
            _aircraftRepository = aircraftRepository;
            _airportRepository = airportRepository;
            _routeRepository = routeRepository;
            _seatRepository = seatRepository;
            _paymentRepository = paymentRepository;
            _cityRepository = cityRepository;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = new AdminDashboardViewModel
            {
                TotalUsers = _userManager.Users.Count(),
                TotalRoles = _roleManager.Roles.Count(),

                TotalFlights = await _flightRepository.CountAsync(),
                TotalBookings = await _bookingRepository.CountAsync(),
                TotalPassengers = await _passengerRepository.CountAsync(),
                TotalAircraft = await _aircraftRepository.CountAsync(),
                TotalAirports = await _airportRepository.CountAsync(),
                TotalRoutes = await _routeRepository.CountAsync(),
                TotalSeats = await _seatRepository.CountAsync(),
                TotalPayments = await _paymentRepository.CountAsync(),
                TotalCities = await _cityRepository.CountAsync()
            };

            return View(dashboard);
        }
    }
}