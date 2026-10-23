
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Flight_Booking_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class FlightsController : Controller
    {
        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<Setting> _settingRepository;
        private readonly IRepository<Ticket> _ticketRepository;

        public FlightsController(
            IRepository<Flight> flightRepository,
            IRepository<AirPort> airportRepository,
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Class> classRepository,
            IRepository<Seat> seatRepository,
            IRepository<Setting> settingRepository,
            IRepository<Ticket> ticketRepository)
        {
            _flightRepository = flightRepository;
            _airportRepository = airportRepository;
            _aircraftClassRepository = aircraftClassRepository;
            _classRepository = classRepository;
            _seatRepository = seatRepository;
            _settingRepository = settingRepository;
            _ticketRepository = ticketRepository;
        }

        // GET: /Customer/Flights
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var flights = await _flightRepository.GetAllAsync(
                filter: f =>
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,
                includes: new Expression<Func<Flight, object>>[]
                {
                    f => f.Airline,
                    f => f.Aircraft,
                    f => f.Route
                },
                IsTracking: false
            );

            var model = new List<AvailableFlightViewModel>();

            foreach (var flight in flights.OrderBy(f => f.DepartureDateTime))
            {
                AirPort? fromAirport = null;
                AirPort? toAirport = null;

                if (flight.Route != null)
                {
                    fromAirport = await _airportRepository.GetOneAsync(
                        filter: a => a.Id == flight.Route.FromAirportId,
                        IsTracking: false
                    );

                    toAirport = await _airportRepository.GetOneAsync(
                        filter: a => a.Id == flight.Route.ToAirportId,
                        IsTracking: false
                    );
                }

                model.Add(new AvailableFlightViewModel
                {
                    FlightId = flight.Id,
                    FlightNumber = flight.FlightNumber,
                    AirlineName = flight.Airline?.Name ?? "Unknown Airline",
                    AircraftName = flight.Aircraft?.RegistrationNumber
                        ?? "Not specified",
                    DepartureAirport = fromAirport?.Name ?? "Unknown Airport",
                    ArrivalAirport = toAirport?.Name ?? "Unknown Airport",
                    Distance = flight.Route?.Distance ?? 0,
                    DepartureDateTime = flight.DepartureDateTime,
                    ArrivalDateTime = flight.ArrivalDateTime,
                    FlightStatus = flight.FlightStatus
                });
            }

            return View(model);
        }

        // GET: /Customer/Flights/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == id &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,
                includes: new Expression<Func<Flight, object>>[]
                {
                    f => f.Airline,
                    f => f.Aircraft,
                    f => f.Route
                },
                IsTracking: false
            );

            if (flight == null)
                return NotFound();

            if (flight.Route == null)
                return NotFound("Flight route was not found.");

            var fromAirport = await _airportRepository.GetOneAsync(
                filter: a => a.Id == flight.Route.FromAirportId,
                IsTracking: false
            );

            var toAirport = await _airportRepository.GetOneAsync(
                filter: a => a.Id == flight.Route.ToAirportId,
                IsTracking: false
            );

            var setting = await _settingRepository.GetOneAsync(
                filter: s => true,
                IsTracking: false
            );

            if (setting == null)
            {
                return Problem(
                    "Ticket pricing settings were not found. Please configure the price per kilometer."
                );
            }

            var aircraftClasses = await _aircraftClassRepository.GetAllAsync(
                filter: ac => ac.AircraftId == flight.AircraftId,
                IsTracking: false
            );

            var model = new FlightDetailsViewModel
            {
                FlightId = flight.Id,
                FlightNumber = flight.FlightNumber,
                AirlineName = flight.Airline?.Name ?? "Unknown Airline",
                AircraftName = flight.Aircraft?.RegistrationNumber
                    ?? "Unknown Aircraft",
                DepartureAirport = fromAirport?.Name ?? "Unknown Airport",
                ArrivalAirport = toAirport?.Name ?? "Unknown Airport",
                Distance = flight.Route.Distance,
                DepartureDateTime = flight.DepartureDateTime,
                ArrivalDateTime = flight.ArrivalDateTime
            };

            foreach (var aircraftClass in aircraftClasses)
            {
                var classInfo = await _classRepository.GetOneAsync(
                    filter: c => c.Id == aircraftClass.ClassId,
                    IsTracking: false
                );

                if (classInfo == null)
                    continue;

                var seats = await _seatRepository.GetAllAsync(
                    filter: s => s.AircraftClassId == aircraftClass.ID,
                    IsTracking: false
                );

                decimal multiplier = classInfo.DistancePercentage;

                decimal ticketPrice =
                    Convert.ToDecimal(flight.Route.Distance)
                    * setting.PricePerKm
                    * multiplier;

                // Count seats not booked for this flight.
                var bookedTickets = await _ticketRepository.GetAllAsync(
                    filter: t => t.FlightId == flight.Id,
                    IsTracking: false
                );

                var bookedSeatIds = bookedTickets
                    .Select(t => t.SeatId)
                    .ToHashSet();

                int availableSeats = seats.Count(
                    s => !bookedSeatIds.Contains(s.Id)
                );

                model.Classes.Add(new ClassOptionViewModel
                {
                    ClassId = classInfo.Id,
                    ClassName = classInfo.Name,
                    DistancePercentage = classInfo.DistancePercentage,
                    AvailableSeats = availableSeats,
                    TicketPrice = ticketPrice
                });
            }

            return View(model);
        }

        // GET: /Customer/Flights/ChooseSeat
        [HttpGet]
        public async Task<IActionResult> ChooseSeat(int flightId, int classId)
        {
            // 1. Get available flight
            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == flightId &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,
                includes: new Expression<Func<Flight, object>>[]
                {
                    f => f.Aircraft,
                    f => f.Route
                },
                IsTracking: false
            );

            if (flight == null)
                return NotFound("Flight not found or unavailable.");

            if (flight.Route == null)
                return NotFound("Flight route not found.");

            // 2. Get selected class on this aircraft
            var aircraftClass = await _aircraftClassRepository.GetOneAsync(
                filter: ac =>
                    ac.AircraftId == flight.AircraftId &&
                    ac.ClassId == classId,
                IsTracking: false
            );

            if (aircraftClass == null)
                return NotFound(
                    "This class is not available on this aircraft."
                );

            // 3. Get class details
            var classInfo = await _classRepository.GetOneAsync(
                filter: c => c.Id == classId,
                IsTracking: false
            );

            if (classInfo == null)
                return NotFound("Class not found.");

            // 4. Get general pricing settings
            var setting = await _settingRepository.GetOneAsync(
                IsTracking: false
            );

            if (setting == null)
                return Problem(
                    "Ticket pricing settings have not been configured."
                );

            // 5. Calculate ticket price
            decimal ticketPrice =
                Convert.ToDecimal(flight.Route.Distance)
                * setting.PricePerKm
                * classInfo.DistancePercentage;

            // 6. Get seats belonging to this aircraft class
            var seats = await _seatRepository.GetAllAsync(
                filter: s =>
                    s.AircraftClassId == aircraftClass.ID,
                IsTracking: false
            );

            // 7. Get tickets booked on this flight
            var bookedTickets = await _ticketRepository.GetAllAsync(
                filter: t => t.FlightId == flight.Id,
                IsTracking: false
            );

            var bookedSeatIds = bookedTickets
                .Select(t => t.SeatId)
                .ToHashSet();

            // 8. Prepare ViewModel
            var model = new ChooseSeatViewModel
            {
                FlightId = flight.Id,
                ClassId = classInfo.Id,
                FlightNumber = flight.FlightNumber,
                AircraftName = flight.Aircraft?.RegistrationNumber
                    ?? "Unknown Aircraft",
                ClassName = classInfo.Name,
                TicketPrice = ticketPrice,

                Seats = seats
                    .OrderBy(s => s.SeatNumber)
                    .Select(s => new SeatOptionViewModel
                    {
                        SeatId = s.Id,
                        SeatNumber = s.SeatNumber,
                        IsAvailable = !bookedSeatIds.Contains(s.Id)
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}
