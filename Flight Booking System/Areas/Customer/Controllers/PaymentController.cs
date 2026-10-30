
using System.Security.Claims;
using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Flight_Booking_System.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly StripeSettings _stripeSettings;
        private readonly IActivityLogService _activityLogService;

        public PaymentController(
            IRepository<Booking> bookingRepository,
            IRepository<Payment> paymentRepository,
            IOptions<StripeSettings> stripeOptions,
            IActivityLogService activityLogService)
        {
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
            _stripeSettings = stripeOptions.Value;
            _activityLogService = activityLogService;
        }

        // GET: /Customer/Payment/Checkout?bookingId=1
        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var booking = await _bookingRepository.GetOneAsync(
                filter: b => b.Id == bookingId &&
                             b.UserId == userId,
                IsTracking: false);

            if (booking == null)
                return NotFound();

            var payment = await _paymentRepository.GetOneAsync(
                filter: p => p.BookingId == bookingId,
                IsTracking: false);

            if (payment == null)
                return NotFound("Payment record not found.");

            if (payment.PaymentStatus ||
                booking.Status == "Confirmed")
            {
                return RedirectToAction(
                    "Confirmation", "Booking",
                    new { area = "Customer", id = booking.Id });
            }

            if (booking.Status != "Pending")
                return BadRequest("Booking is not pending.");

            if (payment.Amount <= 0)
                return BadRequest("Invalid payment amount.");

            if (string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
                return StatusCode(500, "Stripe SecretKey is missing.");

            if (string.IsNullOrWhiteSpace(_stripeSettings.Currency))
                return StatusCode(500, "Stripe Currency is missing.");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var domain = $"{Request.Scheme}://{Request.Host}";

            var amountInSmallestUnit = Convert.ToInt64(
                Math.Round(
                    payment.Amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            var ticketCount = booking.NumberOfTickets;

            var options = new SessionCreateOptions
            {
                Mode = "payment",

                SuccessUrl =
                    $"{domain}/Customer/Payment/Success" +
                    "?session_id={CHECKOUT_SESSION_ID}",

                CancelUrl =
                    $"{domain}/Customer/Payment/Cancel" +
                    $"?bookingId={booking.Id}",

                ClientReferenceId = booking.Id.ToString(),

                CustomerEmail = User.FindFirstValue(
                    ClaimTypes.Email),

                Metadata = new Dictionary<string, string>
                {
                    ["BookingId"] = booking.Id.ToString(),
                    ["BookingReference"] = booking.BookingReference,
                    ["NumberOfTickets"] = ticketCount.ToString()
                },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,

                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency =
                                _stripeSettings.Currency.ToLowerInvariant(),

                            UnitAmount = amountInSmallestUnit,

                            ProductData =
                                new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name =
                                        $"Flight Booking {booking.BookingReference}",

                                    Description =
                                        $"Booking Reference: {booking.BookingReference}. " +
                                        $"Number of tickets: {ticketCount}. " +
                                        $"Total booking amount: {payment.Amount:N2} " +
                                        $"{_stripeSettings.Currency.ToUpperInvariant()}.",

                                    Metadata = new Dictionary<string, string>
                                    {
                                        ["BookingId"] = booking.Id.ToString(),
                                        ["NumberOfTickets"] = ticketCount.ToString()
                                    }
                                }
                        }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            if (string.IsNullOrWhiteSpace(session.Url))
                return StatusCode(
                    500, "Stripe Checkout URL was not returned.");

            // Activity Log: Checkout Created
            await _activityLogService.LogAsync(
                nameof(PaymentController),
                nameof(Checkout),
                nameof(Payment),
                payment.Id.ToString(),
                $"Stripe Checkout session created for booking {booking.BookingReference}."
            );

            return Redirect(session.Url);
        }

        // GET: /Customer/Payment/Success?session_id=...
        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return BadRequest("Missing Stripe session ID.");

            if (string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
                return StatusCode(500, "Stripe SecretKey is missing.");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var stripeService = new SessionService();
            var session = await stripeService.GetAsync(session_id);

            // Do not trust the return URL alone.
            if (session.PaymentStatus != "paid" ||
                !int.TryParse(session.ClientReferenceId, out int bookingId))
            {
                return View("PaymentFailed");
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var booking = await _bookingRepository.GetOneAsync(
                filter: b => b.Id == bookingId &&
                             b.UserId == userId,
                IsTracking: true);

            if (booking == null)
                return NotFound();

            var payment = await _paymentRepository.GetOneAsync(
                filter: p => p.BookingId == bookingId,
                IsTracking: true);

            if (payment == null)
                return NotFound("Payment record not found.");

            // Verify Stripe session details.
            var expectedAmount = Convert.ToInt64(
                Math.Round(
                    payment.Amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            if (session.Metadata == null ||
                !session.Metadata.TryGetValue(
                    "BookingId", out var metadataBookingId) ||
                metadataBookingId != bookingId.ToString() ||
                session.AmountTotal != expectedAmount ||
                !string.Equals(
                    session.Currency,
                    _stripeSettings.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Payment details do not match the booking.");
            }

            // Make the operation safe to repeat.
            if (!payment.PaymentStatus)
            {
                payment.PaymentStatus = true;
                payment.PaymentDate = DateTime.Now;

                booking.Status = "Confirmed";

                _paymentRepository.Update(payment);
                _bookingRepository.Update(booking);

                await _paymentRepository.CommitAsync();
                await _bookingRepository.CommitAsync();

                // Activity Log: Payment Successful
                await _activityLogService.LogAsync(
                    nameof(PaymentController),
                    nameof(Success),
                    nameof(Payment),
                    payment.Id.ToString(),
                    $"Payment completed successfully for booking {booking.BookingReference}. Amount: {payment.Amount:N2}."
                );
            }

            return RedirectToAction(
                "Confirmation", "Booking",
                new { area = "Customer", id = booking.Id });
        }

        // GET: /Customer/Payment/Cancel?bookingId=1
        [HttpGet]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var booking = await _bookingRepository.GetOneAsync(
                filter: b => b.Id == bookingId &&
                             b.UserId == userId,
                IsTracking: false);

            if (booking == null)
                return NotFound();

            var payment = await _paymentRepository.GetOneAsync(
                filter: p => p.BookingId == bookingId,
                IsTracking: false);

            ViewBag.BookingReference = booking.BookingReference;

            // Activity Log: Payment Cancelled
            if (payment != null && !payment.PaymentStatus)
            {
                await _activityLogService.LogAsync(
                    nameof(PaymentController),
                    nameof(Cancel),
                    nameof(Payment),
                    payment.Id.ToString(),
                    $"Customer cancelled Stripe Checkout for booking {booking.BookingReference}."
                );
            }

            return View("PaymentCancelled");
        }
    }
}
