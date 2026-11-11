
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Ticket> _ticketRepository;
        private readonly IActivityLogService _activityLogService;

        public BookingsController(
            IRepository<Booking> bookingRepository,
            IRepository<Ticket> ticketRepository,
            IActivityLogService activityLogService)
        {
            _bookingRepository = bookingRepository;
            _ticketRepository = ticketRepository;
            _activityLogService = activityLogService;
        }

        // GET: /Admin/Bookings
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var bookings = await _bookingRepository.GetAllAsync(
                includes: [
                    x => x.ApplicationUser
                ],
                IsTracking: false
            );

            var result = bookings
                .OrderByDescending(x => x.BookingDate)
                .ToList();

            return View(result);
        }

        // GET: /Admin/Bookings/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingRepository.GetOneAsync(
                x => x.Id == id,
                includes: [
                    x => x.ApplicationUser
                ],
                IsTracking: false
            );

            if (booking == null)
                return NotFound();

            var tickets = await _ticketRepository.GetAllAsync(
                x => x.BookingId == id,
                includes: [
                    x => x.Passenger,
                    x => x.Flight,
                    x => x.Seat
                ],
                IsTracking: false
            );

            ViewBag.Tickets = tickets.ToList();

            await _activityLogService.LogAsync(
                nameof(BookingsController),
                nameof(Details),
                nameof(Booking),
                booking.Id.ToString(),
                $"Admin viewed booking: {booking.BookingReference}."
            );

            return View(booking);
        }
    }
}
