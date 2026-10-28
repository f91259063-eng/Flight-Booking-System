
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;
using Flight_Booking_System.Models.ENUM;

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

        // Added for creating the payment record
        private readonly IRepository<Payment> _paymentRepository;

        public PassengerController(
            IRepository<Passenger> passengerRepository,
            IRepository<Seat> seatRepository,
            IRepository<Flight> flightRepository,
            IRepository<Class> classRepository,
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IRepository<Setting> settingRepository,
            IRepository<AircraftClass> aircraftClassRepository,
            IRepository<Payment> paymentRepository)
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
            if (string.IsNullOrWhiteSpace(seatIds))
                return BadRequest("No seats selected.");

            var selectedSeatIds = seatIds
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                    int.TryParse(x, out var id)
                        ? id
                        : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!selectedSeatIds.Any())
                return BadRequest("No valid seats selected.");

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
                return NotFound(
                    "Flight not found or unavailable."
                );

            if (flight.Route == null)
                return BadRequest(
                    "Flight route is not available."
                );

            // =====================================================
            // Get Class
            // =====================================================

            var classInfo =
                await _classRepository.GetOneAsync(
                    filter: c => c.Id == classId,
                    IsTracking: false
                );

            if (classInfo == null)
                return NotFound("Class not found.");

            // =====================================================
            // Check Class belongs to Aircraft
            // =====================================================

            var aircraftClass =
                await GetAircraftClass(
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

            var setting =
                await GetLatestSetting();

            if (setting == null)
                return BadRequest(
                    "Ticket price setting was not found."
                );

            // =====================================================
            // Calculate Ticket Price
            // =====================================================

            decimal distance =
                Convert.ToDecimal(
                    flight.Route.Distance
                );

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            // =====================================================
            // Create ViewModel
            // =====================================================

            var model =
                new PassengerBookingViewModel
                {
                    FlightId = flight.Id,
                    ClassId = classId,
                    TicketPrice = ticketPrice
                };

            // =====================================================
            // Validate selected seats
            // =====================================================

            foreach (var seatId in selectedSeatIds)
            {
                var seat =
                    await _seatRepository.GetOneAsync(
                        filter: s =>
                            s.Id == seatId &&
                            s.AircraftClassId ==
                                aircraftClass.ID,

                        IsTracking: false
                    );

                if (seat == null)
                {
                    return BadRequest(
                        "One or more selected seats are invalid."
                    );
                }

                // Check if seat already booked
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

                        DateOfBirth =
                            DateTime.Today.AddYears(-18)
                    }
                );
            }

            if (!model.Passengers.Any())
            {
                return BadRequest(
                    "No available seats were selected."
                );
            }

            return View(model);
        }

        // =========================================================
        // GET: Customer/Passenger/SearchPassenger
        //
        // Search using:
        // Passport Number OR National ID
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SearchPassenger(
            [FromQuery] string? passportNumber,
            [FromQuery] string? nationalID)
        {
            // =====================================================
            // Check Login
            // =====================================================

            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            // =====================================================
            // Trim input
            // =====================================================

            passportNumber =
                passportNumber?.Trim();

            nationalID =
                nationalID?.Trim();

            // =====================================================
            // Debug information
            // =====================================================

            // يمكنك حذف هذه الأسطر بعد التأكد أن البحث يعمل
            Console.WriteLine(
                $"Search Passport: [{passportNumber}]"
            );

            Console.WriteLine(
                $"Search NationalID: [{nationalID}]"
            );

            // =====================================================
            // Must enter at least one value
            // =====================================================

            if (string.IsNullOrWhiteSpace(passportNumber) &&
                string.IsNullOrWhiteSpace(nationalID))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Enter a passport number or National ID."
                });
            }

            // =====================================================
            // Search Passenger
            //
            // Passport OR National ID
            // =====================================================

            var matches =
                await _passengerRepository.GetAllAsync(
                    filter: p =>
                        (
                            !string.IsNullOrWhiteSpace(
                                passportNumber
                            )
                            &&
                            p.PassportNumber ==
                                passportNumber
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(
                                nationalID
                            )
                            &&
                            p.NationalID ==
                                nationalID
                        ),

                    IsTracking: false
                );

            // =====================================================
            // Remove duplicate records
            // =====================================================

            var passengers =
                matches
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

            // =====================================================
            // Passenger not found
            // =====================================================

            if (passengers.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "No passenger was found. You can enter new passenger details."
                });
            }

            // =====================================================
            // More than one passenger
            // =====================================================

            if (passengers.Count > 1)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "The passport number and National ID match different passenger records. Please verify the numbers."
                });
            }

            // =====================================================
            // Passenger found
            // =====================================================

            var passenger =
                passengers.First();

            // =====================================================
            // Return passenger data
            // =====================================================

            return Json(new
            {
                success = true,

                passenger = new
                {
                    Id = passenger.Id,

                    FirstName =
                        passenger.FirstName
                        ?? string.Empty,

                    MiddleName =
                        passenger.MiddleName
                        ?? string.Empty,

                    LastName =
                        passenger.LastName
                        ?? string.Empty,

                    PassportNumber =
                        passenger.PassportNumber
                        ?? string.Empty,

                    NationalID =
                        passenger.NationalID
                        ?? string.Empty,

                    Phone =
                        passenger.Phone
                        ?? string.Empty,

                    Email =
                        passenger.Email
                        ?? string.Empty,

                    DateOfBirth =
                        passenger.DateOfBirth
                            .ToString("yyyy-MM-dd")
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

            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // =====================================================
            // Validate passengers
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
                return View(model);

            // =====================================================
            // Get Flight
            // =====================================================

            var flight =
                await _flightRepository.GetOneAsync(
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
                return NotFound(
                    "Flight not found or unavailable."
                );

            if (flight.Route == null)
                return BadRequest(
                    "Flight route is not available."
                );

            // =====================================================
            // Get Class
            // =====================================================

            var classInfo =
                await _classRepository.GetOneAsync(
                    filter: c =>
                        c.Id == model.ClassId,

                    IsTracking: false
                );

            if (classInfo == null)
                return NotFound(
                    "Class not found."
                );

            // =====================================================
            // Verify Aircraft Class
            // =====================================================

            var aircraftClass =
                await GetAircraftClass(
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

            var setting =
                await GetLatestSetting();

            if (setting == null)
                return BadRequest(
                    "Ticket price setting was not found."
                );

            // =====================================================
            // Calculate Ticket Price
            // =====================================================

            decimal distance =
                Convert.ToDecimal(
                    flight.Route.Distance
                );

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            // =====================================================
            // Validate Seat IDs
            // =====================================================

            var seatIds =
                model.Passengers
                    .Select(p => p.SeatId)
                    .ToList();

            if (seatIds.Any(id => id <= 0) ||
                seatIds.Count !=
                seatIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "",
                    "Invalid or duplicate seat selection."
                );

                return View(model);
            }

            // =====================================================
            // Check Seat Availability
            // =====================================================

            foreach (
                var passengerModel
                in model.Passengers)
            {
                var seat =
                    await _seatRepository.GetOneAsync(
                        filter: s =>
                            s.Id ==
                                passengerModel.SeatId
                            &&
                            s.AircraftClassId ==
                                aircraftClass.ID,

                        IsTracking: false
                    );

                if (seat == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Seat {passengerModel.SeatNumber} is invalid."
                    );

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

                    return View(model);
                }
            }

            // =====================================================
            // Find Existing Passengers
            // =====================================================

            var matchedPassengers =
                new List<Passenger?>();

            foreach (
                var passengerModel
                in model.Passengers)
            {
                string passport =
                    passengerModel
                        .PassportNumber?
                        .Trim()
                    ?? string.Empty;

                string nationalId =
                    passengerModel
                        .NationalID?
                        .Trim()
                    ?? string.Empty;

                Passenger? matchedPassenger = null;

                // =================================================
                // Passenger selected from Search
                // =================================================

                if (
                    passengerModel
                        .SelectedPassengerId
                        .HasValue
                    &&
                    passengerModel
                        .SelectedPassengerId
                        .Value > 0
                )
                {
                    matchedPassenger =
                        await _passengerRepository
                            .GetOneAsync(
                                filter: p =>
                                    p.Id ==
                                    passengerModel
                                        .SelectedPassengerId
                                        .Value,

                                IsTracking: true
                            );

                    if (matchedPassenger == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "The selected passenger could not be found. Search again."
                        );

                        return View(model);
                    }
                }
                else if (
                    !string.IsNullOrWhiteSpace(passport)
                    ||
                    !string.IsNullOrWhiteSpace(nationalId))
                {
                    // =============================================
                    // Search by Passport OR National ID
                    // =============================================

                    var foundPassengers =
                        await _passengerRepository
                            .GetAllAsync(
                                filter: p =>
                                    (
                                        !string.IsNullOrWhiteSpace(
                                            passport)
                                        &&
                                        p.PassportNumber ==
                                            passport
                                    )
                                    ||
                                    (
                                        !string.IsNullOrWhiteSpace(
                                            nationalId)
                                        &&
                                        p.NationalID ==
                                            nationalId
                                    ),

                                IsTracking: false
                            );

                    var uniqueMatches =
                        foundPassengers
                            .GroupBy(p => p.Id)
                            .Select(g => g.First())
                            .ToList();

                    if (uniqueMatches.Count > 1)
                    {
                        ModelState.AddModelError(
                            "",
                            "The passport number and National ID match different passenger records."
                        );

                        return View(model);
                    }

                    if (uniqueMatches.Count == 1)
                    {
                        matchedPassenger =
                            uniqueMatches[0];
                    }
                }

                // =================================================
                // Existing Passenger
                // =================================================

                if (matchedPassenger != null)
                {
                    // =============================================
                    // Check identity conflicts
                    // =============================================

                    var conflicts =
                        await _passengerRepository
                            .GetAllAsync(
                                filter: p =>
                                    p.Id != matchedPassenger.Id
                                    &&
                                    (
                                        (
                                            !string.IsNullOrWhiteSpace(
                                                passport)
                                            &&
                                            p.PassportNumber ==
                                                passport
                                        )
                                        ||
                                        (
                                            !string.IsNullOrWhiteSpace(
                                                nationalId)
                                            &&
                                            p.NationalID ==
                                                nationalId
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

                        return View(model);
                    }
                }
                else
                {
                    // =============================================
                    // New Passenger
                    // =============================================

                    if (
                        string.IsNullOrWhiteSpace(passport)
                        &&
                        string.IsNullOrWhiteSpace(nationalId))
                    {
                        ModelState.AddModelError(
                            "",
                            "Enter a passport number or National ID for each passenger."
                        );

                        return View(model);
                    }
                }

                matchedPassengers.Add(matchedPassenger);
            }

            // =====================================================
            // Prevent duplicate passenger in same booking
            // =====================================================

            for (
                int i = 0;
                i < model.Passengers.Count;
                i++)
            {
                for (
                    int j = i + 1;
                    j < model.Passengers.Count;
                    j++)
                {
                    var first =
                        model.Passengers[i];

                    var second =
                        model.Passengers[j];

                    // Same Passenger
                    if (
                        matchedPassengers[i] != null
                        &&
                        matchedPassengers[j] != null
                        &&
                        matchedPassengers[i]!.Id ==
                        matchedPassengers[j]!.Id
                    )
                    {
                        ModelState.AddModelError(
                            "",
                            "The same passenger cannot be assigned to multiple seats in one booking."
                        );

                        return View(model);
                    }

                    string passport1 =
                        first.PassportNumber?.Trim()
                        ?? string.Empty;

                    string passport2 =
                        second.PassportNumber?.Trim()
                        ?? string.Empty;

                    string national1 =
                        first.NationalID?.Trim()
                        ?? string.Empty;

                    string national2 =
                        second.NationalID?.Trim()
                        ?? string.Empty;

                    bool samePassport =
                        !string.IsNullOrWhiteSpace(passport1)
                        &&
                        !string.IsNullOrWhiteSpace(passport2)
                        &&
                        passport1 == passport2;

                    bool sameNationalId =
                        !string.IsNullOrWhiteSpace(national1)
                        &&
                        !string.IsNullOrWhiteSpace(national2)
                        &&
                        national1 == national2;

                    if (samePassport || sameNationalId)
                    {
                        ModelState.AddModelError(
                            "",
                            "Two passengers in the same booking cannot use the same passport number or National ID."
                        );

                        return View(model);
                    }
                }
            }

            // =====================================================
            // Create Booking
            // =====================================================

            var booking =
                new Booking
                {
                    UserId = userId,

                    BookingDate =
                        DateTime.Now,

                    BookingReference =
                        GenerateBookingReference(),

                    Status = "Pending",

                    NumberOfTickets =
                        model.Passengers.Count
                };

            await _bookingRepository
                .InsertAsync(booking);

            var bookingResult =
                await _bookingRepository
                    .CommitAsync();

            if (
                bookingResult <= 0 ||
                booking.Id <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to create booking."
                );

                return View(model);
            }

            // =====================================================
            // Save Passengers + Tickets
            // =====================================================

            for (
                int i = 0;
                i < model.Passengers.Count;
                i++)
            {
                var passengerModel =
                    model.Passengers[i];

                string passport =
                    passengerModel
                        .PassportNumber?
                        .Trim()
                    ?? string.Empty;

                string nationalId =
                    passengerModel
                        .NationalID?
                        .Trim()
                    ?? string.Empty;

                Passenger passenger;

                // =================================================
                // Existing Passenger
                // =================================================

                if (matchedPassengers[i] != null)
                {
                    passenger =
                        await _passengerRepository
                            .GetOneAsync(
                                filter: p =>
                                    p.Id ==
                                    matchedPassengers[i]!.Id,

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
                        passengerModel
                            .FirstName
                            .Trim();

                    passenger.MiddleName =
                        passengerModel
                            .MiddleName?
                            .Trim()
                        ?? string.Empty;

                    passenger.LastName =
                        passengerModel
                            .LastName
                            .Trim();

                    passenger.PassportNumber =
                        passport;

                    passenger.NationalID =
                        nationalId;

                    passenger.Phone =
                        passengerModel
                            .Phone?
                            .Trim()
                        ?? string.Empty;

                    passenger.Email =
                        passengerModel
                            .Email?
                            .Trim()
                        ?? string.Empty;

                    passenger.DateOfBirth =
                        passengerModel.DateOfBirth;

                    _passengerRepository.Update(passenger);

                    var updateResult =
                        await _passengerRepository
                            .CommitAsync();

                    if (updateResult <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unable to update passenger information."
                        );

                        return View(model);
                    }
                }
                else
                {
                    // =================================================
                    // New Passenger
                    // =================================================

                    passenger =
                        new Passenger
                        {
                            FirstName =
                                passengerModel
                                    .FirstName
                                    .Trim(),

                            MiddleName =
                                passengerModel
                                    .MiddleName?
                                    .Trim()
                                ?? string.Empty,

                            LastName =
                                passengerModel
                                    .LastName
                                    .Trim(),

                            PassportNumber =
                                passport,

                            NationalID =
                                nationalId,

                            Phone =
                                passengerModel
                                    .Phone?
                                    .Trim()
                                ?? string.Empty,

                            Email =
                                passengerModel
                                    .Email?
                                    .Trim()
                                ?? string.Empty,

                            DateOfBirth =
                                passengerModel.DateOfBirth
                        };

                    await _passengerRepository
                        .InsertAsync(passenger);

                    var passengerResult =
                        await _passengerRepository
                            .CommitAsync();

                    if (
                        passengerResult <= 0 ||
                        passenger.Id <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unable to save passenger information."
                        );

                        return View(model);
                    }
                }

                // =================================================
                // Create Ticket
                // =================================================

                var ticket =
                    new Ticket
                    {
                        BookingId =
                            booking.Id,

                        PassengerId =
                            passenger.Id,

                        FlightId =
                            flight.Id,

                        SeatId =
                            passengerModel.SeatId,

                        TicketPrice =
                            ticketPrice
                    };

                await _ticketRepository
                    .InsertAsync(ticket);

                var ticketResult =
                    await _ticketRepository
                        .CommitAsync();

                if (ticketResult <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to create ticket."
                    );

                    return View(model);
                }
            }

            // =====================================================
            // Create Payment Record
            // =====================================================

            decimal totalAmount =
                ticketPrice * model.Passengers.Count;

            var payment =
                new Payment
                {
                    BookingId = booking.Id,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = PaymentMethod.CreditCard,
                    Amount = totalAmount,
                    PaymentStatus = false
                };

            await _paymentRepository
                .InsertAsync(payment);

            var paymentResult =
                await _paymentRepository
                    .CommitAsync();

            if (
                paymentResult <= 0 ||
                payment.Id <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to create payment record."
                );

                return View(model);
            }

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
                });
        }

        // =========================================================
        // Get AircraftClass
        // =========================================================

        private async Task<AircraftClass?> GetAircraftClass(
            int aircraftId,
            int classId)
        {
            var aircraftClasses =
                await _aircraftClassRepository
                    .GetAllAsync(
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
                await _settingRepository
                    .GetAllAsync(
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
