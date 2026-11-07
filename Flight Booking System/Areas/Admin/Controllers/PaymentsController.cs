
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
    public class PaymentsController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IActivityLogService _activityLogService;

        public PaymentsController(
            IRepository<Payment> paymentRepository,
            IActivityLogService activityLogService)
        {
            _paymentRepository = paymentRepository;
            _activityLogService = activityLogService;
        }

        // GET: /Admin/Payments
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var payments = await _paymentRepository.GetAllAsync(
                includes: [
                    x => x.Booking
                ],
                IsTracking: false
            );

            var result = payments
                .OrderByDescending(x => x.PaymentDate)
                .ToList();

            return View(result);
        }

        // GET: /Admin/Payments/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentRepository.GetOneAsync(
                x => x.Id == id,
                includes: [
                    x => x.Booking
                ],
                IsTracking: false
            );

            if (payment == null)
                return NotFound();

            await _activityLogService.LogAsync(
                nameof(PaymentsController),
                nameof(Details),
                nameof(Payment),
                payment.Id.ToString(),
                $"Admin viewed payment #{payment.Id}."
            );

            return View(payment);
        }
    }
}
