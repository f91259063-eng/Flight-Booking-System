
using Flight_Booking_System.Models;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Security.Claims;

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

        public PassengerController(
            IRepository<Passenger> passengerRepository,
            IRepository<Seat> seatRepository,
            IRepository<Flight> flightRepository,
            IRepository<Class> classRepository,
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IRepository<Setting> settingRepository,
            IRepository<AircraftClass> aircraftClassRepository)
        {
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _flightRepository = flightRepository;
            _classRepository = classRepository;
            _bookingRepository = bookingRepository;
            _ticketRepository = ticketRepository;
            _settingRepository = settingRepository;
            _aircraftClassRepository = aircraftClassRepository;
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
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!selectedSeatIds.Any())
                return BadRequest("No valid seats selected.");

            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == flightId &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,
                includes: new Expression<Func<Flight, object>>[]
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

            var classInfo = await _classRepository.GetOneAsync(
                filter: c => c.Id == classId,
                IsTracking: false
            );

            if (classInfo == null)
                return NotFound("Class not found.");

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

            var setting = await GetLatestSetting();

            if (setting == null)
                return BadRequest("Ticket price setting was not found.");

            decimal distance = Convert.ToDecimal(flight.Route.Distance);

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            var model = new PassengerBookingViewModel
            {
                FlightId = flight.Id,
                ClassId = classId,
                TicketPrice = ticketPrice
            };

            foreach (var seatId in selectedSeatIds)
            {
                var seat = await _seatRepository.GetOneAsync(
                    filter: s =>
                        s.Id == seatId &&
                        s.AircraftClassId == aircraftClass.ID,
                    IsTracking: false
                );

                if (seat == null)
                    return BadRequest("One or more selected seats are invalid.");

                var existingTicket = await _ticketRepository.GetOneAsync(
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

                model.Passengers.Add(new PassengerSeatViewModel
                {
                    SeatId = seat.Id,
                    SeatNumber = seat.SeatNumber,
                    DateOfBirth = DateTime.Today.AddYears(-18)
                });
            }

            return View(model);
        }

        // =========================================================
        // GET: Customer/Passenger/SearchPassenger
        // Search using Passport Number OR National ID
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SearchPassenger(
            string? passportNumber,
            string? nationalID)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            passportNumber = passportNumber?.Trim();
            nationalID = nationalID?.Trim();

            if (string.IsNullOrWhiteSpace(passportNumber) &&
                string.IsNullOrWhiteSpace(nationalID))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a passport number or National ID."
                });
            }

            var matches = await _passengerRepository.GetAllAsync(
                filter: p =>
                    (!string.IsNullOrEmpty(passportNumber) &&
                     p.PassportNumber == passportNumber)
                    ||
                    (!string.IsNullOrEmpty(nationalID) &&
                     p.NationalID == nationalID),
                IsTracking: false
            );

            var passengers = matches
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();

            if (passengers.Count == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No passenger was found. You can enter new passenger details."
                });
            }

            if (passengers.Count > 1)
            {
                return Conflict(new
                {
                    success = false,
                    message = "The passport number and National ID match different passenger records. Please verify the numbers."
                });
            }

            var passenger = passengers[0];

            return Json(new
            {
                success = true,
                passenger = new
                {
                    passenger.Id,
                    passenger.FirstName,
                    passenger.MiddleName,
                    passenger.LastName,
                    passenger.PassportNumber,
                    passenger.NationalID,
                    passenger.Phone,
                    passenger.Email,
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
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (model.Passengers == null || !model.Passengers.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No passenger information was entered."
                );

                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            // Get Flight
            var flight = await _flightRepository.GetOneAsync(
                filter: f =>
                    f.Id == model.FlightId &&
                    f.FlightStatus &&
                    f.DepartureDateTime >= DateTime.Now,
                includes: new Expression<Func<Flight, object>>[]
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

            // Get Class
            var classInfo = await _classRepository.GetOneAsync(
                filter: c => c.Id == model.ClassId,
                IsTracking: false
            );

            if (classInfo == null)
                return NotFound("Class not found.");

            // Verify class belongs to aircraft
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

            // Get latest price setting
            var setting = await GetLatestSetting();

            if (setting == null)
                return BadRequest("Ticket price setting was not found.");

            // Calculate price on the server
            decimal distance = Convert.ToDecimal(flight.Route.Distance);

            decimal ticketPrice =
                distance *
                setting.PricePerKm *
                classInfo.DistancePercentage;

            // Validate seat IDs
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

                return View(model);
            }

            // Check seats are valid and available
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

                    return View(model);
                }

                var existingTicket = await _ticketRepository.GetOneAsync(
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
            // Find existing passengers and validate identity numbers
            // BEFORE creating the booking
            // =====================================================

            var matchedPassengers = new List<Passenger?>();

            foreach (var passengerModel in model.Passengers)
            {
                string passport = passengerModel.PassportNumber?.Trim()
                    ?? string.Empty;

                string nationalId = passengerModel.NationalID?.Trim()
                    ?? string.Empty;

                Passenger? matchedPassenger = null;

                // If a passenger was selected from the search result,
                // retrieve that record for updating.
                if (passengerModel.SelectedPassengerId.HasValue &&
                    passengerModel.SelectedPassengerId.Value > 0)
                {
                    matchedPassenger = await _passengerRepository.GetOneAsync(
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

                        return View(model);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(passport) ||
                         !string.IsNullOrWhiteSpace(nationalId))
                {
                    // Server-side duplicate prevention even if the user
                    // did not press the Search button.
                    var foundPassengers =
                        await _passengerRepository.GetAllAsync(
                            filter: p =>
                                (!string.IsNullOrEmpty(passport) &&
                                 p.PassportNumber == passport)
                                ||
                                (!string.IsNullOrEmpty(nationalId) &&
                                 p.NationalID == nationalId),
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

                        return View(model);
                    }

                    if (uniqueMatches.Count == 1)
                        matchedPassenger = uniqueMatches[0];
                }

                if (matchedPassenger != null)
                {
                    // Check identity numbers against OTHER passenger records.
                    var conflicts = await _passengerRepository.GetAllAsync(
                        filter: p =>
                            p.Id != matchedPassenger.Id &&
                            (
                                (!string.IsNullOrEmpty(passport) &&
                                 p.PassportNumber == passport)
                                ||
                                (!string.IsNullOrEmpty(nationalId) &&
                                 p.NationalID == nationalId)
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

                    // If the record was explicitly selected, prevent
                    // conflicting identity values from identifying another record.
                    if (passengerModel.SelectedPassengerId.HasValue &&
                        passengerModel.SelectedPassengerId.Value > 0)
                    {
                        bool passportMatchesAnother =
                            !string.IsNullOrWhiteSpace(passport) &&
                            !string.IsNullOrWhiteSpace(
                                matchedPassenger.PassportNumber) &&
                            matchedPassenger.PassportNumber != passport;

                        bool nationalIdMatchesAnother =
                            !string.IsNullOrWhiteSpace(nationalId) &&
                            !string.IsNullOrWhiteSpace(
                                matchedPassenger.NationalID) &&
                            matchedPassenger.NationalID != nationalId;

                        if (passportMatchesAnother || nationalIdMatchesAnother)
                        {
                            // A selected passenger may change identity numbers,
                            // but only if those new numbers are not used by
                            // another record. The conflict check above handles that.
                            // Therefore, changing the selected record's own
                            // numbers is permitted.
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(passport) &&
                         string.IsNullOrWhiteSpace(nationalId))
                {
                    ModelState.AddModelError(
                        "",
                        "Enter a passport number or National ID for each passenger."
                    );

                    return View(model);
                }

                matchedPassengers.Add(matchedPassenger);
            }

            // Prevent using the same passenger or identity numbers twice
            // within this booking.
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

                        return View(model);
                    }

                    string passport1 = first.PassportNumber?.Trim()
                        ?? string.Empty;
                    string passport2 = second.PassportNumber?.Trim()
                        ?? string.Empty;

                    string national1 = first.NationalID?.Trim()
                        ?? string.Empty;
                    string national2 = second.NationalID?.Trim()
                        ?? string.Empty;

                    if ((!string.IsNullOrWhiteSpace(passport1) &&
                         passport1 == passport2)
                        ||
                        (!string.IsNullOrWhiteSpace(national1) &&
                         national1 == national2))
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

            var booking = new Booking
            {
                UserId = userId,
                BookingDate = DateTime.Now,
                BookingReference = GenerateBookingReference(),
                Status = "Pending",
                NumberOfTickets = model.Passengers.Count
            };

            await _bookingRepository.InsertAsync(booking);

            var bookingResult = await _bookingRepository.CommitAsync();

            if (bookingResult <= 0 || booking.Id <= 0)
            {
                ModelState.AddModelError("", "Unable to create booking.");
                return View(model);
            }

            // =====================================================
            // Reuse or update existing passengers; otherwise create new
            // Create one ticket per passenger and selected seat
            // =====================================================

            for (int i = 0; i < model.Passengers.Count; i++)
            {
                var passengerModel = model.Passengers[i];

                string passport = passengerModel.PassportNumber?.Trim()
                    ?? string.Empty;

                string nationalId = passengerModel.NationalID?.Trim()
                    ?? string.Empty;

                Passenger passenger;

                if (matchedPassengers[i] != null)
                {
                    // Reload as tracked entity before updating.
                    passenger = await _passengerRepository.GetOneAsync(
                        filter: p => p.Id == matchedPassengers[i]!.Id,
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

                    // Update the existing passenger record.
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
                }
                else
                {
                    // Create a new passenger.
                    passenger = new Passenger
                    {
                        FirstName =
                            passengerModel.FirstName.Trim(),

                        MiddleName =
                            passengerModel.MiddleName?.Trim()
                            ?? string.Empty,

                        LastName =
                            passengerModel.LastName.Trim(),

                        PassportNumber = passport,
                        NationalID = nationalId,

                        Phone =
                            passengerModel.Phone?.Trim()
                            ?? string.Empty,

                        Email =
                            passengerModel.Email?.Trim()
                            ?? string.Empty,

                        DateOfBirth =
                            passengerModel.DateOfBirth
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
                }

                var ticket = new Ticket
                {
                    BookingId = booking.Id,
                    PassengerId = passenger.Id,
                    FlightId = flight.Id,
                    SeatId = passengerModel.SeatId,
                    TicketPrice = ticketPrice
                };

                await _ticketRepository.InsertAsync(ticket);

                var ticketResult = await _ticketRepository.CommitAsync();

                if (ticketResult <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to create ticket."
                    );

                    return View(model);
                }
            }

            return RedirectToAction(
                "Confirmation",
                "Booking",
                new
                {
                    area = "Customer",
                    id = booking.Id
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
        // Get latest Setting
        // =========================================================

        private async Task<Setting?> GetLatestSetting()
        {
            var settings = await _settingRepository.GetAllAsync(
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
