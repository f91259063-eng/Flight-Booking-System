
using System.Security.Claims;
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Ticket> _ticketRepository;
        private readonly IRepository<Passenger> _passengerRepository;
        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly IActivityLogService _activityLogService;

        public TicketsController(
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IRepository<Passenger> passengerRepository,
            IRepository<Flight> flightRepository,
            IRepository<Seat> seatRepository,
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Class> classRepository,
            IRepository<AirPort> airportRepository,
            IActivityLogService activityLogService)
        {
            _bookingRepository = bookingRepository;
            _ticketRepository = ticketRepository;
            _passengerRepository = passengerRepository;
            _flightRepository = flightRepository;
            _seatRepository = seatRepository;
            _aircraftClassRepository = aircraftClassRepository;
            _classRepository = classRepository;
            _airportRepository = airportRepository;
            _activityLogService = activityLogService;
        }

        // GET: Customer/Tickets/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Get current logged-in user
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // 2. Get only bookings belonging to current user
            var bookings = await _bookingRepository.GetAllAsync(
                filter: b => b.UserId == userId,
                IsTracking: false
            );

            var model = new List<TicketDetailsViewModel>();

            // 3. Get tickets for every booking
            foreach (var booking in bookings)
            {
                var tickets = await _ticketRepository.GetAllAsync(
                    filter: t => t.BookingId == booking.Id,
                    IsTracking: false
                );

                foreach (var ticket in tickets)
                {
                    // 4. Get passenger
                    var passenger =
                        await _passengerRepository.GetOneAsync(
                            filter: p => p.Id == ticket.PassengerId,
                            IsTracking: false
                        );

                    // 5. Get flight
                    var flight =
                        await _flightRepository.GetOneAsync(
                            filter: f => f.Id == ticket.FlightId,
                            IsTracking: false
                        );

                    // 6. Get seat
                    var seat =
                        await _seatRepository.GetOneAsync(
                            filter: s => s.Id == ticket.SeatId,
                            IsTracking: false
                        );

                    if (flight == null || seat == null)
                        continue;

                    // 7. Get departure and arrival airports
                    string departureAirport = "Unknown Airport";
                    string arrivalAirport = "Unknown Airport";

                    if (flight.Route != null)
                    {
                        var fromAirport =
                            await _airportRepository.GetOneAsync(
                                filter: a =>
                                    a.Id == flight.Route.FromAirportId,
                                IsTracking: false
                            );

                        var toAirport =
                            await _airportRepository.GetOneAsync(
                                filter: a =>
                                    a.Id == flight.Route.ToAirportId,
                                IsTracking: false
                            );

                        departureAirport =
                            fromAirport?.Name ?? "Unknown Airport";

                        arrivalAirport =
                            toAirport?.Name ?? "Unknown Airport";
                    }
                    else
                    {
                        // Load the route if it was not loaded with Flight
                        // This will be handled by the Flight repository
                        // if Route is configured as a navigation property.
                    }

                    // 8. Get aircraft class and ticket class
                    string className = "Unknown Class";

                    var aircraftClass =
                        await _aircraftClassRepository.GetOneAsync(
                            filter: ac =>
                                ac.ID == seat.AircraftClassId,
                            IsTracking: false
                        );

                    if (aircraftClass != null)
                    {
                        var classInfo =
                            await _classRepository.GetOneAsync(
                                filter: c =>
                                    c.Id == aircraftClass.ClassId,
                                IsTracking: false
                            );

                        className = classInfo?.Name ?? "Unknown Class";
                    }

                    // 9. Build ticket ViewModel
                    model.Add(new TicketDetailsViewModel
                    {
                        TicketId = ticket.Id,

                        BookingReference =
                            booking.BookingReference,

                        BookingStatus =
                            booking.Status,

                        PassengerName = passenger == null
                            ? "Unknown Passenger"
                            : string.Join(" ",
                                new[]
                                {
                                    passenger.FirstName,
                                    passenger.MiddleName,
                                    passenger.LastName
                                }.Where(x =>
                                    !string.IsNullOrWhiteSpace(x))),

                        FlightNumber = flight.FlightNumber,

                        DepartureAirport = departureAirport,

                        ArrivalAirport = arrivalAirport,

                        DepartureDateTime =
                            flight.DepartureDateTime,

                        SeatNumber = seat.SeatNumber,

                        ClassName = className,

                        TicketPrice = ticket.TicketPrice
                    });
                }
            }

            // 10. Sort tickets by departure date
            model = model
                .OrderByDescending(t => t.DepartureDateTime)
                .ToList();

            // 11. Activity Log
            await _activityLogService.LogAsync(
                nameof(TicketsController),
                nameof(Index),
                nameof(Ticket),
                null,
                $"Customer viewed tickets. Total tickets: {model.Count}."
            );

            return View(model);
        }
    }
}
