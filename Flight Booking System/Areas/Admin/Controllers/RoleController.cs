
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ModelsServices;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IActivityLogService activityLogService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _activityLogService = activityLogService;
        }

        // GET: /Admin/Roles/Index
        [HttpGet]
        public IActionResult Index()
        {
            var roles = _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToList();

            return View(roles);
        }

        // GET: /Admin/Roles/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Roles/Create
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

            var role = new IdentityRole(name);

            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                await _activityLogService.LogAsync(
                    nameof(RolesController),
                    nameof(Create),
                    nameof(IdentityRole),
                    role.Id,
                    $"Created role: {role.Name}."
                );

                TempData["Success"] = "Role created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        // POST: /Admin/Roles/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            // Prevent deleting Admin role
            if (string.Equals(
                role.Name,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The Admin role cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Check whether the role is assigned to users
            var usersInRole = await _userManager.GetUsersInRoleAsync(
                role.Name!);

            if (usersInRole.Any())
            {
                TempData["Error"] =
                    "This role cannot be deleted because it is assigned to users.";

                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                await _activityLogService.LogAsync(
                    nameof(RolesController),
                    nameof(Delete),
                    nameof(IdentityRole),
                    role.Id,
                    $"Deleted role: {role.Name}."
                );

                TempData["Success"] = "Role deleted successfully.";
            }
            else
            {
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
