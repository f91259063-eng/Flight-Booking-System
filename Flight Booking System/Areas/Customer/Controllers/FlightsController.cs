
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

        public FlightsController(
            IRepository<Flight> flightRepository,
            IRepository<AirPort> airportRepository)
        {
            _flightRepository = flightRepository;
            _airportRepository = airportRepository;
        }

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
                    AircraftName = flight.Aircraft?.RegistrationNumber ?? "Not specified",
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
    }
}
