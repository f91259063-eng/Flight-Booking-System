
﻿
using System.Linq.Expressions;
using System.Security.Claims;
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer,BookingAgent,Admin")]
    public class BookingController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Ticket> _ticketRepository;
        private readonly IRepository<Passenger> _passengerRepository;
        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<AirPort> _airportRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IActivityLogService _activityLogService;

        public BookingController(
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IRepository<Passenger> passengerRepository,
            IRepository<Flight> flightRepository,
            IRepository<Seat> seatRepository,
            IRepository<AirPort> airportRepository,
            IRepository<Payment> paymentRepository,
            IActivityLogService activityLogService)
        {
            _bookingRepository = bookingRepository;
            _ticketRepository = ticketRepository;
            _passengerRepository = passengerRepository;
            _flightRepository = flightRepository;
            _seatRepository = seatRepository;
            _airportRepository = airportRepository;
            _paymentRepository = paymentRepository;
            _activityLogService = activityLogService;
        }

        // =========================================================
        // GET: Customer/Booking/Confirmation/5
        // Booking Confirmation
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Get booking belonging to current user
            var booking = await _bookingRepository.GetOneAsync(
                filter: b => b.Id == id &&
                             b.UserId == userId,
                IsTracking: false);

            if (booking == null)
                return NotFound();

            // Check booking status
            if (booking.Status != "Confirmed")
                return BadRequest(
                    "Booking has not been confirmed.");

            // Get payment
            var payment = await _paymentRepository.GetOneAsync(
                filter: p => p.BookingId == booking.Id,
                IsTracking: false);

            if (payment == null || !payment.PaymentStatus)
                return BadRequest(
                    "Payment has not been completed.");

            // Get booking tickets
            var tickets = await _ticketRepository.GetAllAsync(
                filter: t => t.BookingId == booking.Id,
                IsTracking: false);

            var ticketList = tickets.ToList();

            if (!ticketList.Any())
                return NotFound(
                    "No tickets were found for this booking.");

            // Get flight from first ticket
            var firstTicket = ticketList.First();

            var flight = await _flightRepository.GetOneAsync(
                filter: f => f.Id == firstTicket.FlightId,
                includes: new Expression<Func<Flight, object>>[]
                {
                    f => f.Route
                },
                IsTracking: false);

            if (flight == null || flight.Route == null)
                return NotFound(
                    "Flight information was not found.");

            // Get airports
            var fromAirport = await _airportRepository.GetOneAsync(
                filter: a => a.Id == flight.Route.FromAirportId,
                IsTracking: false);

            var toAirport = await _airportRepository.GetOneAsync(
                filter: a => a.Id == flight.Route.ToAirportId,
                IsTracking: false);

            // Create ViewModel
            var model = new BookingConfirmationViewModel
            {
                BookingId = booking.Id,

                BookingReference = booking.BookingReference,

                BookingDate = booking.BookingDate,

                Status = booking.Status,

                FlightNumber = flight.FlightNumber,

                FromAirport = fromAirport?.Name ?? "Unknown Airport",

                ToAirport = toAirport?.Name ?? "Unknown Airport",

                DepartureDateTime = flight.DepartureDateTime,

                ArrivalDateTime = flight.ArrivalDateTime,

                PointsRedeemed = booking.PointsRedeemed,

                DiscountAmount = booking.DiscountAmount,

                AmountPaid = payment.Amount
            };

            // Get passengers and seats
            foreach (var ticket in ticketList)
            {
                var passenger = await _passengerRepository.GetOneAsync(
                    filter: p => p.Id == ticket.PassengerId,
                    IsTracking: false);

                var seat = await _seatRepository.GetOneAsync(
                    filter: s => s.Id == ticket.SeatId,
                    IsTracking: false);

                model.Tickets.Add(new BookingTicketViewModel
                {
                    PassengerName = passenger == null
                        ? "Unknown Passenger"
                        : $"{passenger.FirstName} {passenger.MiddleName} {passenger.LastName}"
                            .Replace("  ", " ")
                            .Trim(),

                    SeatNumber = seat?.SeatNumber ?? "Unknown Seat",

                    TicketPrice = ticket.TicketPrice
                });
            }

            // Activity Log
            await _activityLogService.LogAsync(
                nameof(BookingController),
                nameof(Confirmation),
                nameof(Booking),
                booking.Id.ToString(),
                $"Customer viewed booking confirmation. Booking Reference: {booking.BookingReference}. Tickets: {ticketList.Count}."
            );

            return View(model);
        }

        // =========================================================
        // GET: Customer/Booking
        // My Bookings
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Get only current user's bookings
            var bookings = await _bookingRepository.GetAllAsync(
                filter: b => b.UserId == userId,
                IsTracking: false);

            var bookingList = bookings
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            var model = new List<MyBookingViewModel>();

            foreach (var booking in bookingList)
            {
                // Get tickets for this booking
                var tickets = await _ticketRepository.GetAllAsync(
                    filter: t => t.BookingId == booking.Id,
                    IsTracking: false);

                var ticketList = tickets.ToList();

                if (!ticketList.Any())
                {
                    model.Add(new MyBookingViewModel
                    {
                        BookingId = booking.Id,
                        BookingReference = booking.BookingReference,
                        BookingDate = booking.BookingDate,
                        Status = booking.Status,
                        NumberOfTickets = booking.NumberOfTickets,
                        TotalPrice = 0
                    });

                    continue;
                }

                // First ticket gives us the flight
                var firstTicket = ticketList.First();

                var flight = await _flightRepository.GetOneAsync(
                    filter: f => f.Id == firstTicket.FlightId,
                    includes: new Expression<Func<Flight, object>>[]
                    {
                        f => f.Route
                    },
                    IsTracking: false);

                if (flight == null)
                {
                    model.Add(new MyBookingViewModel
                    {
                        BookingId = booking.Id,
                        BookingReference = booking.BookingReference,
                        BookingDate = booking.BookingDate,
                        Status = booking.Status,
                        NumberOfTickets = booking.NumberOfTickets,
                        TotalPrice = ticketList.Sum(t => t.TicketPrice)
                    });

                    continue;
                }

                // Get airport names
                string fromAirport = "Unknown Airport";
                string toAirport = "Unknown Airport";

                if (flight.Route != null)
                {
                    var from = await _airportRepository.GetOneAsync(
                        filter: a =>
                            a.Id == flight.Route.FromAirportId,
                        IsTracking: false);

                    var to = await _airportRepository.GetOneAsync(
                        filter: a =>
                            a.Id == flight.Route.ToAirportId,
                        IsTracking: false);

                    fromAirport = from?.Name ?? "Unknown Airport";
                    toAirport = to?.Name ?? "Unknown Airport";
                }

                // Calculate ticket subtotal
                decimal totalPrice = ticketList.Sum(
                    t => t.TicketPrice);

                model.Add(new MyBookingViewModel
                {
                    BookingId = booking.Id,

                    BookingReference = booking.BookingReference,

                    BookingDate = booking.BookingDate,

                    Status = booking.Status,

                    NumberOfTickets = booking.NumberOfTickets,

                    TotalPrice = totalPrice,

                    FlightNumber = flight.FlightNumber,

                    DepartureAirport = fromAirport,

                    ArrivalAirport = toAirport,

                    DepartureDateTime = flight.DepartureDateTime,

                    ArrivalDateTime = flight.ArrivalDateTime
                });
            }

            // Activity Log: My Bookings Viewed
            await _activityLogService.LogAsync(
                nameof(BookingController),
                nameof(Index),
                nameof(Booking),
                null,
                $"Customer viewed their bookings list. Total bookings: {bookingList.Count}."
            );

            return View(model);
        }
    }
}
