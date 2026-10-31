
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]


    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: /Roles/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToList();

            return View(roles);
        }

        // GET: /Roles/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Role name is required.");
                return View();
            }

            name = name.Trim();

            if (await _roleManager.RoleExistsAsync(name))
            {
                ModelState.AddModelError("", "This role already exists.");
                return View();
            }

            var result = await _roleManager.CreateAsync(
                new IdentityRole(name));

            if (result.Succeeded)
            {
                TempData["Success"] = "Role created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        // POST: /Roles/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            // the system needs these roles (Admin, Receptionist, BookingAgent, Customer)
            if (role.Name != null && Flight_Booking_System.Models.Identity.AppRoles.All.Contains(role.Name))
            {
                TempData["Error"] = $"The {role.Name} role is used by the system and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["Success"] = "Role deleted successfully.";
            }
            else
            {
                TempData["Error"] = string.Join(
                    " ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
