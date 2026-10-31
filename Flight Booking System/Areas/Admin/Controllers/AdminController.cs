
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Services;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IActivityLogService _activityLogService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _activityLogService = activityLogService;
        }

        // =========================================================
        // USERS LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var rows = new List<UserRowViewModel>();

            foreach (var u in users)
            {
                rows.Add(new UserRowViewModel
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    EmailConfirmed = u.EmailConfirmed,
                    IsAdmin = await _userManager.IsInRoleAsync(u, "Admin"),
                    IsLocked = u.LockoutEnd.HasValue &&
                               u.LockoutEnd > DateTimeOffset.UtcNow,
                    TwoFactorEnabled = u.TwoFactorEnabled
                });
            }

            return View(rows);
        }

        // =========================================================
        // CREATE USER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View();
        }

        // =========================================================
        // CREATE USER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return View(model);
            }

            // Check Email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                ViewBag.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return View(model);
            }

            // Create User
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return View(model);
            }

            // Assign Multiple Roles
            if (model.SelectedRoles != null &&
                model.SelectedRoles.Any())
            {
                foreach (var role in model.SelectedRoles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(
                            user,
                            role);

                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(
                                    "",
                                    error.Description);
                            }
                        }
                    }
                }
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Admin",
                "CreateUser",
                "ApplicationUser",
                user.Id,
                $"Created user: {user.Email}"
            );

            TempData["Success"] =
                $"{user.Email} was created successfully.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // EDIT USER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                SelectedRoles = roles.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                IsLocked = user.LockoutEnd.HasValue &&
                           user.LockoutEnd > DateTimeOffset.UtcNow
            };

            ViewBag.Roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(model);
        }

        // =========================================================
        // EDIT USER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(
            UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
                return NotFound();

            // Update Basic Information
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return View(model);
            }

            // Update Roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove Old Roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles);
            }

            // Add New Roles
            if (model.SelectedRoles != null &&
                model.SelectedRoles.Any())
            {
                foreach (var role in model.SelectedRoles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(
                            user,
                            role);
                    }
                }
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Admin",
                "EditUser",
                "ApplicationUser",
                user.Id,
                $"Updated user: {user.Email}"
            );

            TempData["Success"] =
                $"{user.Email} was updated successfully.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // TOGGLE ADMIN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            // Prevent Changing Own Role
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You can't change your own role.";

                return RedirectToAction(nameof(Users));
            }

            var isAdmin = await _userManager.IsInRoleAsync(
                user,
                "Admin");

            IdentityResult result;

            if (isAdmin)
            {
                result = await _userManager.RemoveFromRoleAsync(
                    user,
                    "Admin");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "Customer");
                }
            }
            else
            {
                result = await _userManager.RemoveFromRoleAsync(
                    user,
                    "Customer");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "Admin");
                }
            }

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Users));
            }

            var newRole = isAdmin ? "Customer" : "Admin";

            // Activity Log
            await _activityLogService.LogAsync(
                "Admin",
                "ToggleAdmin",
                "ApplicationUser",
                user.Id,
                $"Changed role for {user.Email} to {newRole}"
            );

            TempData["Success"] =
                $"{user.Email} is now a {newRole}.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // LOCK / UNLOCK
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            // Prevent Locking Own Account
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You can't lock your own account.";

                return RedirectToAction(nameof(Users));
            }

            var isLocked =
                user.LockoutEnd.HasValue &&
                user.LockoutEnd > DateTimeOffset.UtcNow;

            await _userManager.SetLockoutEnabledAsync(
                user,
                true);

            IdentityResult result;

            if (isLocked)
            {
                // Unlock
                result = await _userManager.SetLockoutEndDateAsync(
                    user,
                    null);
            }
            else
            {
                // Lock
                result = await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.UtcNow.AddYears(100));
            }

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Users));
            }

            // Activity Log
            await _activityLogService.LogAsync(
                "Admin",
                isLocked ? "UnlockUser" : "LockUser",
                "ApplicationUser",
                user.Id,
                $"{(isLocked ? "Unlocked" : "Locked")} user: {user.Email}"
            );

            TempData["Success"] = isLocked
                ? $"{user.Email} was unlocked."
                : $"{user.Email} was locked.";

            return RedirectToAction(nameof(Users));
        }
    }
}
