
using System.Linq.Expressions;
using System.Security.Claims;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.ENUM;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Flight_Booking_System.Services.Rewards;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class PassengerController : Controller
    {
        private readonly IRepository<Passenger> _passengerRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<Flight> _flightRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Ticket> _ticketRepository;
        private readonly IRepository<Setting> _settingRepository;
        private readonly IRepository<AircraftClass> _aircraftClassRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IActivityLogService _activityLogService;
        private readonly IRewardService _rewardService;

        public PassengerController(
            IRepository<Passenger> passengerRepository,
            IRepository<Seat> seatRepository,
            IRepository<Flight> flightRepository,
            IRepository<Class> classRepository,
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IRepository<Setting> settingRepository,
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Payment> paymentRepository,
            IActivityLogService activityLogService,
            IRewardService rewardService)
        {
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _flightRepository = flightRepository;
            _classRepository = classRepository;
            _bookingRepository = bookingRepository;
            _ticketRepository = ticketRepository;
            _settingRepository = settingRepository;
            _aircraftClassRepository = aircraftClassRepository;
            _paymentRepository = paymentRepository;
            _activityLogService = activityLogService;
            _rewardService = rewardService;
        }

        // =========================================================
        // GET: Customer/Passenger/Create
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int flightId,
            int classId,
            string seatIds)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(seatIds))
                return BadRequest("No seats selected.");

            var selectedSeatIds = seatIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!selectedSeatIds.Any())
                return BadRequest("No valid seats selected.");

            // =====================================================
            // Get Available Reward Points
            // =====================================================

            int availablePoints =
                await _rewardService.GetAvailablePointsAsync(userId);

            // =====================================================
            // Get Flight
            // =====================================================

            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == flightId &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,

                includes:
                    new Expression<Func<Flight, object>>[]
                    {
                        f => f.Route,
                        f => f.Aircraft
                    },

                IsTracking: false
            );

            if (flight == null)
                return NotFound("Flight not found or unavailable.");

            if (flight.Route == null)
                return BadRequest("Flight route is not available.");

            // =====================================================
            // Get Class
            // =====================================================

            var classInfo = await _classRepository.GetOneAsync(
                filter: c => c.Id == classId,
                IsTracking: false
            );

            if (classInfo == null)
                return NotFound("Class not found.");

            // =====================================================
            // Check Class belongs to Aircraft
            // =====================================================

            var aircraftClass = await GetAircraftClass(
                flight.AircraftId,
                classId
            );

            if (aircraftClass == null)
            {
                return BadRequest(
                    "The selected class is not available on this aircraft."
                );
            }

            // =====================================================
            // Get Latest Setting
            // =====================================================

            var setting = await GetLatestSetting();

            if (setting == null)
                return BadRequest("Ticket price setting was not found.");

            // =====================================================
            // Calculate Ticket Price
            // =====================================================

            decimal distance = Convert.ToDecimal(flight.Route.Distance);

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            // =====================================================
            // Create ViewModel
            // =====================================================

            var model = new PassengerBookingViewModel
            {
                FlightId = flight.Id,
                ClassId = classId,
                TicketPrice = ticketPrice,

                AvailablePoints = availablePoints,
                PointsToRedeem = 0,
                DiscountAmount = 0m,
                TotalPrice = 0m
            };

            // =====================================================
            // Validate selected seats
            // =====================================================

            foreach (var seatId in selectedSeatIds)
            {
                var seat = await _seatRepository.GetOneAsync(
                    filter: s =>
                        s.Id == seatId &&
                        s.AircraftClassId == aircraftClass.ID,

                    IsTracking: false
                );

                if (seat == null)
                {
                    return BadRequest(
                        "One or more selected seats are invalid."
                    );
                }

                var existingTicket =
                    await _ticketRepository.GetOneAsync(
                        filter: t =>
                            t.FlightId == flight.Id &&
                            t.SeatId == seat.Id,

                        IsTracking: false
                    );

                if (existingTicket != null)
                {
                    return BadRequest(
                        $"Seat {seat.SeatNumber} is no longer available."
                    );
                }

                model.Passengers.Add(
                    new PassengerSeatViewModel
                    {
                        SeatId = seat.Id,
                        SeatNumber = seat.SeatNumber,
                        DateOfBirth = DateTime.Today.AddYears(-18)
                    }
                );
            }

            if (!model.Passengers.Any())
            {
                return BadRequest(
                    "No available seats were selected."
                );
            }

            model.TotalPrice =
                model.TicketPrice * model.Passengers.Count;

            // =====================================================
            // Activity Log
            // =====================================================

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Create),
                nameof(Flight),
                flight.Id.ToString(),
                $"Customer opened passenger information form for flight {flight.Id}, class {classId}, and {model.Passengers.Count} seats."
            );

            return View(model);
        }

        // =========================================================
        // GET: Customer/Passenger/SearchPassenger
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SearchPassenger(
            [FromQuery] string? passportNumber,
            [FromQuery] string? nationalID)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            passportNumber = passportNumber?.Trim();
            nationalID = nationalID?.Trim();

            if (string.IsNullOrWhiteSpace(passportNumber) &&
                string.IsNullOrWhiteSpace(nationalID))
            {
                return Json(new
                {
                    success = false,
                    message = "Enter a passport number or National ID."
                });
            }

            var matches = await _passengerRepository.GetAllAsync(
                filter: p =>
                    (
                        !string.IsNullOrWhiteSpace(passportNumber) &&
                        p.PassportNumber == passportNumber
                    )
                    ||
                    (
                        !string.IsNullOrWhiteSpace(nationalID) &&
                        p.NationalID == nationalID
                    ),

                IsTracking: false
            );

            var passengers = matches
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(SearchPassenger),
                nameof(Passenger),
                null,
                $"Customer searched for a passenger. Results found: {passengers.Count}."
            );

            if (passengers.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No passenger was found. You can enter new passenger details."
                });
            }

            if (passengers.Count > 1)
            {
                return Json(new
                {
                    success = false,
                    message = "The passport number and National ID match different passenger records. Please verify the numbers."
                });
            }

            var passenger = passengers.First();

            return Json(new
            {
                success = true,

                passenger = new
                {
                    Id = passenger.Id,
                    FirstName = passenger.FirstName ?? string.Empty,
                    MiddleName = passenger.MiddleName ?? string.Empty,
                    LastName = passenger.LastName ?? string.Empty,
                    PassportNumber = passenger.PassportNumber ?? string.Empty,
                    NationalID = passenger.NationalID ?? string.Empty,
                    Phone = passenger.Phone ?? string.Empty,
                    Email = passenger.Email ?? string.Empty,
                    DateOfBirth = passenger.DateOfBirth.ToString("yyyy-MM-dd")
                }
            });
        }

        // =========================================================
        // POST: Customer/Passenger/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PassengerBookingViewModel model)
        {
            // =====================================================
            // Check Login
            // =====================================================

            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // =====================================================
            // Validate Passengers
            // =====================================================

            if (model.Passengers == null ||
                !model.Passengers.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No passenger information was entered."
                );

                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.AvailablePoints =
                    await _rewardService.GetAvailablePointsAsync(userId);

                return View(model);
            }

            // =====================================================
            // Get Flight
            // =====================================================

            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == model.FlightId &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,

                includes:
                    new Expression<Func<Flight, object>>[]
                    {
                        f => f.Route,
                        f => f.Aircraft
                    },

                IsTracking: false
            );

            if (flight == null)
                return NotFound("Flight not found or unavailable.");

            if (flight.Route == null)
                return BadRequest("Flight route is not available.");

            // =====================================================
            // Get Class
            // =====================================================

            var classInfo = await _classRepository.GetOneAsync(
                filter: c => c.Id == model.ClassId,
                IsTracking: false
            );

            if (classInfo == null)
                return NotFound("Class not found.");

            // =====================================================
            // Verify Aircraft Class
            // =====================================================

            var aircraftClass = await GetAircraftClass(
                flight.AircraftId,
                model.ClassId
            );

            if (aircraftClass == null)
            {
                return BadRequest(
                    "The selected class is not available on this aircraft."
                );
            }

            // =====================================================
            // Get Latest Setting
            // =====================================================

            var setting = await GetLatestSetting();

            if (setting == null)
                return BadRequest("Ticket price setting was not found.");

            // =====================================================
            // Calculate Ticket Price on Server
            // =====================================================

            decimal distance = Convert.ToDecimal(flight.Route.Distance);

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            decimal subtotal =
                ticketPrice * model.Passengers.Count;

            // =====================================================
            // Validate Reward Points on Server
            // =====================================================

            int availablePoints =
                await _rewardService.GetAvailablePointsAsync(userId);

            int pointsToRedeem = model.PointsToRedeem;

            decimal discountAmount = 0m;

            if (pointsToRedeem < 0)
            {
                ModelState.AddModelError(
                    nameof(model.PointsToRedeem),
                    "Points cannot be negative."
                );
            }
            else if (pointsToRedeem > 0)
            {
                if (pointsToRedeem < 100 ||
                    pointsToRedeem % 100 != 0)
                {
                    ModelState.AddModelError(
                        nameof(model.PointsToRedeem),
                        "Points must be redeemed in multiples of 100, with a minimum of 100 points."
                    );
                }
                else if (pointsToRedeem > availablePoints)
                {
                    ModelState.AddModelError(
                        nameof(model.PointsToRedeem),
                        "You do not have enough available reward points."
                    );
                }
                else
                {
                    discountAmount =
                        (pointsToRedeem / 100) * 50m;

                    if (discountAmount > subtotal)
                    {
                        ModelState.AddModelError(
                            nameof(model.PointsToRedeem),
                            "The reward discount cannot exceed the booking total."
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                model.TicketPrice = ticketPrice;
                model.AvailablePoints = availablePoints;
                model.DiscountAmount = 0m;
                model.TotalPrice = subtotal;

                return View(model);
            }

            decimal finalTotal = subtotal - discountAmount;

            // =====================================================
            // Validate Seat IDs
            // =====================================================

            var seatIds = model.Passengers
                .Select(p => p.SeatId)
                .ToList();

            if (seatIds.Any(id => id <= 0) ||
                seatIds.Count != seatIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "Invalid or duplicate seat selection."
                );

                model.AvailablePoints = availablePoints;
                model.TicketPrice = ticketPrice;

                return View(model);
            }

            // =====================================================
            // Check Seat Availability
            // =====================================================

            foreach (var passengerModel in model.Passengers)
            {
                var seat = await _seatRepository.GetOneAsync(
                    filter: s =>
                        s.Id == passengerModel.SeatId &&
                        s.AircraftClassId == aircraftClass.ID,

                    IsTracking: false
                );

                if (seat == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Seat {passengerModel.SeatNumber} is invalid."
                    );

                    model.AvailablePoints = availablePoints;
                    model.TicketPrice = ticketPrice;

                    return View(model);
                }

                var existingTicket =
                    await _ticketRepository.GetOneAsync(
                        filter: t =>
                            t.FlightId == flight.Id &&
                            t.SeatId == seat.Id,

                        IsTracking: false
                    );

                if (existingTicket != null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Seat {seat.SeatNumber} is no longer available."
                    );

                    model.AvailablePoints = availablePoints;
                    model.TicketPrice = ticketPrice;

                    return View(model);
                }
            }

            // =====================================================
            // Find Existing Passengers
            // =====================================================

            var matchedPassengers = new List<Passenger?>();

            foreach (var passengerModel in model.Passengers)
            {
                string passport =
                    passengerModel.PassportNumber?.Trim()
                    ?? string.Empty;

                string nationalId =
                    passengerModel.NationalID?.Trim()
                    ?? string.Empty;

                Passenger? matchedPassenger = null;

                if (passengerModel.SelectedPassengerId.HasValue &&
                    passengerModel.SelectedPassengerId.Value > 0)
                {
                    matchedPassenger =
                        await _passengerRepository.GetOneAsync(
                            filter: p =>
                                p.Id == passengerModel.SelectedPassengerId.Value,

                            IsTracking: true
                        );

                    if (matchedPassenger == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "The selected passenger could not be found. Search again."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(passport) ||
                         !string.IsNullOrWhiteSpace(nationalId))
                {
                    var foundPassengers =
                        await _passengerRepository.GetAllAsync(
                            filter: p =>
                                (
                                    !string.IsNullOrWhiteSpace(passport) &&
                                    p.PassportNumber == passport
                                )
                                ||
                                (
                                    !string.IsNullOrWhiteSpace(nationalId) &&
                                    p.NationalID == nationalId
                                ),

                            IsTracking: false
                        );

                    var uniqueMatches = foundPassengers
                        .GroupBy(p => p.Id)
                        .Select(g => g.First())
                        .ToList();

                    if (uniqueMatches.Count > 1)
                    {
                        ModelState.AddModelError(
                            "",
                            "The passport number and National ID match different passenger records."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }

                    if (uniqueMatches.Count == 1)
                        matchedPassenger = uniqueMatches[0];
                }

                if (matchedPassenger != null)
                {
                    var conflicts =
                        await _passengerRepository.GetAllAsync(
                            filter: p =>
                                p.Id != matchedPassenger.Id &&
                                (
                                    (
                                        !string.IsNullOrWhiteSpace(passport) &&
                                        p.PassportNumber == passport
                                    )
                                    ||
                                    (
                                        !string.IsNullOrWhiteSpace(nationalId) &&
                                        p.NationalID == nationalId
                                    )
                                ),

                            IsTracking: false
                        );

                    if (conflicts.Any())
                    {
                        ModelState.AddModelError(
                            "",
                            "The passport number or National ID is already used by another passenger."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(passport) &&
                        string.IsNullOrWhiteSpace(nationalId))
                    {
                        ModelState.AddModelError(
                            "",
                            "Enter a passport number or National ID for each passenger."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }
                }

                matchedPassengers.Add(matchedPassenger);
            }

            // =====================================================
            // Prevent Duplicate Passenger in Same Booking
            // =====================================================

            for (int i = 0; i < model.Passengers.Count; i++)
            {
                for (int j = i + 1; j < model.Passengers.Count; j++)
                {
                    var first = model.Passengers[i];
                    var second = model.Passengers[j];

                    if (matchedPassengers[i] != null &&
                        matchedPassengers[j] != null &&
                        matchedPassengers[i]!.Id == matchedPassengers[j]!.Id)
                    {
                        ModelState.AddModelError(
                            "",
                            "The same passenger cannot be assigned to multiple seats in one booking."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }

                    string passport1 =
                        first.PassportNumber?.Trim() ?? string.Empty;

                    string passport2 =
                        second.PassportNumber?.Trim() ?? string.Empty;

                    string national1 =
                        first.NationalID?.Trim() ?? string.Empty;

                    string national2 =
                        second.NationalID?.Trim() ?? string.Empty;

                    bool samePassport =
                        !string.IsNullOrWhiteSpace(passport1) &&
                        !string.IsNullOrWhiteSpace(passport2) &&
                        passport1 == passport2;

                    bool sameNationalId =
                        !string.IsNullOrWhiteSpace(national1) &&
                        !string.IsNullOrWhiteSpace(national2) &&
                        national1 == national2;

                    if (samePassport || sameNationalId)
                    {
                        ModelState.AddModelError(
                            "",
                            "Two passengers in the same booking cannot use the same passport number or National ID."
                        );

                        model.AvailablePoints = availablePoints;
                        model.TicketPrice = ticketPrice;

                        return View(model);
                    }
                }
            }

            // =====================================================
            // Create Booking
            // =====================================================

            var booking = new Booking
            {
                UserId = userId,
                BookingDate = DateTime.Now,
                BookingReference = GenerateBookingReference(),
                Status = "Pending",
                NumberOfTickets = model.Passengers.Count,
                PointsRedeemed = pointsToRedeem,
                DiscountAmount = discountAmount
            };

            await _bookingRepository.InsertAsync(booking);

            var bookingResult =
                await _bookingRepository.CommitAsync();

            if (bookingResult <= 0 || booking.Id <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to create booking."
                );

                model.AvailablePoints = availablePoints;
                model.TicketPrice = ticketPrice;

                return View(model);
            }

            // =====================================================
            // Reserve Reward Points
            // =====================================================

            if (pointsToRedeem > 0)
            {
                bool reserved = await _rewardService.ReservePointsAsync(
                    userId,
                    booking.Id,
                    pointsToRedeem
                );

                if (!reserved)
                {
                    booking.Status = "Cancelled";

                    _bookingRepository.Update(booking);
                    await _bookingRepository.CommitAsync();

                    ModelState.AddModelError(
                        "",
                        "Unable to reserve reward points. Your booking was cancelled. Please try again."
                    );

                    model.AvailablePoints =
                        await _rewardService.GetAvailablePointsAsync(userId);

                    model.TicketPrice = ticketPrice;
                    model.DiscountAmount = 0m;
                    model.TotalPrice = subtotal;

                    return View(model);
                }
            }

            // Activity Log: Booking Created

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Create),
                nameof(Booking),
                booking.Id.ToString(),
                $"Customer created booking {booking.BookingReference} with {booking.NumberOfTickets} tickets. Status: Pending. Points reserved: {pointsToRedeem}. Discount: {discountAmount:N2}."
            );

            // =====================================================
            // Save Passengers + Tickets
            // =====================================================

            for (int i = 0; i < model.Passengers.Count; i++)
            {
                var passengerModel = model.Passengers[i];

                string passport =
                    passengerModel.PassportNumber?.Trim()
                    ?? string.Empty;

                string nationalId =
                    passengerModel.NationalID?.Trim()
                    ?? string.Empty;

                Passenger passenger;

                // =================================================
                // Existing Passenger
                // =================================================

                if (matchedPassengers[i] != null)
                {
                    passenger = await _passengerRepository.GetOneAsync(
                        filter: p =>
                            p.Id == matchedPassengers[i]!.Id,

                        IsTracking: true
                    );

                    if (passenger == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "A selected passenger could not be loaded."
                        );

                        return View(model);
                    }

                    passenger.FirstName =
                        passengerModel.FirstName.Trim();

                    passenger.MiddleName =
                        passengerModel.MiddleName?.Trim()
                        ?? string.Empty;

                    passenger.LastName =
                        passengerModel.LastName.Trim();

                    passenger.PassportNumber = passport;
                    passenger.NationalID = nationalId;

                    passenger.Phone =
                        passengerModel.Phone?.Trim()
                        ?? string.Empty;

                    passenger.Email =
                        passengerModel.Email?.Trim()
                        ?? string.Empty;

                    passenger.DateOfBirth =
                        passengerModel.DateOfBirth;

                    _passengerRepository.Update(passenger);

                    var updateResult =
                        await _passengerRepository.CommitAsync();

                    if (updateResult <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unable to update passenger information."
                        );

                        return View(model);
                    }

                    await _activityLogService.LogAsync(
                        nameof(PassengerController),
                        nameof(Create),
                        nameof(Passenger),
                        passenger.Id.ToString(),
                        $"Existing passenger {passenger.Id} information was updated during booking {booking.BookingReference}."
                    );
                }
                else
                {
                    // =================================================
                    // New Passenger
                    // =================================================

                    passenger = new Passenger
                    {
                        FirstName = passengerModel.FirstName.Trim(),

                        MiddleName =
                            passengerModel.MiddleName?.Trim()
                            ?? string.Empty,

                        LastName = passengerModel.LastName.Trim(),
                        PassportNumber = passport,
                        NationalID = nationalId,

                        Phone =
                            passengerModel.Phone?.Trim()
                            ?? string.Empty,

                        Email =
                            passengerModel.Email?.Trim()
                            ?? string.Empty,

                        DateOfBirth = passengerModel.DateOfBirth
                    };

                    await _passengerRepository.InsertAsync(passenger);

                    var passengerResult =
                        await _passengerRepository.CommitAsync();

                    if (passengerResult <= 0 || passenger.Id <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unable to save passenger information."
                        );

                        return View(model);
                    }

                    await _activityLogService.LogAsync(
                        nameof(PassengerController),
                        nameof(Create),
                        nameof(Passenger),
                        passenger.Id.ToString(),
                        $"New passenger {passenger.Id} was created during booking {booking.BookingReference}."
                    );
                }

                // =================================================
                // Create Ticket
                // =================================================

                var ticket = new Ticket
                {
                    BookingId = booking.Id,
                    PassengerId = passenger.Id,
                    FlightId = flight.Id,
                    SeatId = passengerModel.SeatId,
                    TicketPrice = ticketPrice
                };

                await _ticketRepository.InsertAsync(ticket);

                var ticketResult =
                    await _ticketRepository.CommitAsync();

                if (ticketResult <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to create ticket."
                    );

                    return View(model);
                }

                await _activityLogService.LogAsync(
                    nameof(PassengerController),
                    nameof(Create),
                    nameof(Ticket),
                    ticket.Id.ToString(),
                    $"Ticket created for booking {booking.BookingReference}. Flight: {flight.Id}, Seat: {passengerModel.SeatId}, Passenger: {passenger.Id}."
                );
            }

            // =====================================================
            // Create Payment Record
            // =====================================================

            decimal totalAmount = finalTotal;

            var payment = new Payment
            {
                BookingId = booking.Id,
                PaymentDate = DateTime.Now,
                PaymentMethod = PaymentMethod.CreditCard,
                Amount = totalAmount,
                PaymentStatus = false
            };

            await _paymentRepository.InsertAsync(payment);

            var paymentResult =
                await _paymentRepository.CommitAsync();

            if (paymentResult <= 0 || payment.Id <= 0)
            {
                // Return reserved points if payment record could not be saved.
                if (pointsToRedeem > 0)
                {
                    await _rewardService.ReleaseReservedPointsAsync(
                        userId,
                        booking.Id
                    );
                }

                booking.Status = "Cancelled";

                _bookingRepository.Update(booking);
                await _bookingRepository.CommitAsync();

                ModelState.AddModelError(
                    "",
                    "Unable to create payment record. The booking was cancelled."
                );

                model.AvailablePoints =
                    await _rewardService.GetAvailablePointsAsync(userId);

                return View(model);
            }

            // =====================================================
            // Activity Log: Payment Record Created
            // =====================================================

            await _activityLogService.LogAsync(
                nameof(PassengerController),
                nameof(Create),
                nameof(Payment),
                payment.Id.ToString(),
                $"Payment record created for booking {booking.BookingReference}. Original subtotal: {subtotal:N2}. Discount: {discountAmount:N2}. Amount: {payment.Amount:N2}. Method: {payment.PaymentMethod}. Status: Pending."
            );

            // =====================================================
            // Redirect To Stripe Checkout
            // =====================================================

            return RedirectToAction(
                "Checkout",
                "Payment",
                new
                {
                    area = "Customer",
                    bookingId = booking.Id
                }
            );
        }

        // =========================================================
        // Get AircraftClass
        // =========================================================

        private async Task<AircraftClass?> GetAircraftClass(
            int aircraftId,
            int classId)
        {
            var aircraftClasses =
                await _aircraftClassRepository.GetAllAsync(
                    filter: ac =>
                        ac.AircraftId == aircraftId &&
                        ac.ClassId == classId,

                    IsTracking: false
                );

            return aircraftClasses.FirstOrDefault();
        }

        // =========================================================
        // Get Latest Setting
        // =========================================================

        private async Task<Setting?> GetLatestSetting()
        {
            var settings =
                await _settingRepository.GetAllAsync(
                    IsTracking: false
                );

            return settings
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();
        }

        // =========================================================
        // Generate Booking Reference
        // =========================================================

        private string GenerateBookingReference()
        {
            return
                $"BK-{DateTime.Now:yyyyMMddHHmmss}-" +
                $"{Random.Shared.Next(1000, 9999)}";
        }
    }
}
