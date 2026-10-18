using Flight_Booking_System.Models.Identity;
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

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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


            // =====================================================
            // ASSIGN MULTIPLE ROLES
            // =====================================================

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


            // Update basic information
            user.FirstName = model.FirstName;

            user.LastName = model.LastName;

            user.Email = model.Email;

            user.UserName = model.Email;

            user.PhoneNumber = model.PhoneNumber;


            var updateResult =
                await _userManager.UpdateAsync(user);


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


            // =====================================================
            // UPDATE ROLES
            // =====================================================

            var currentRoles =
                await _userManager.GetRolesAsync(user);


            // Remove old roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles);
            }


            // Add new roles
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


            // Prevent changing own role
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You can't change your own role.";

                return RedirectToAction(nameof(Users));
            }


            if (await _userManager.IsInRoleAsync(
                user,
                "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(
                    user,
                    "Admin");

                await _userManager.AddToRoleAsync(
                    user,
                    "Customer");

                TempData["Success"] =
                    $"{user.Email} is now a Customer.";
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(
                    user,
                    "Customer");

                await _userManager.AddToRoleAsync(
                    user,
                    "Admin");

                TempData["Success"] =
                    $"{user.Email} is now an Admin.";
            }


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


            // Prevent locking own account
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


            if (isLocked)
            {
                // Unlock
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null);

                TempData["Success"] =
                    $"{user.Email} was unlocked.";
            }
            else
            {
                // Lock
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.UtcNow.AddYears(100));

                TempData["Success"] =
                    $"{user.Email} was locked.";
            }


            return RedirectToAction(nameof(Users));
        }
    }
}