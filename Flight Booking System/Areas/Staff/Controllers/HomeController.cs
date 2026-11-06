using Flight_Booking_System.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = AppRoles.StaffRoles)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
