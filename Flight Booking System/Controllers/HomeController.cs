using System.Diagnostics;
using Flight_Booking_System.Models;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // the site starts at the login page
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account", new { area = "Identity" });

            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (User.IsInRole("Receptionist") || User.IsInRole("BookingAgent"))
                return RedirectToAction("Index", "Home", new { area = "Staff" });

            return RedirectToAction("Index", "Flights", new { area = "Customer" });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
