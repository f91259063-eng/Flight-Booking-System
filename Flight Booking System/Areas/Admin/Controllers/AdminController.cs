
using System.Text.RegularExpressions;
using Flight_Booking_System.Data;
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
        private static readonly Regex UserNameRegex =
            new(@"^[A-Za-z0-9._-]{3,20}$");

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly IActivityLogService _activityLogService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _activityLogService = activityLogService;
        }

        // =========================================================
        // USERS LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Users(
            string? search,
            string? role,
            string? status)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(search)) ||
                    (u.Email != null && u.Email.Contains(search)) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var rows = new List<UserRowViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                var locked = user.LockoutEnd.HasValue &&
                             user.LockoutEnd > DateTimeOffset.UtcNow;

                if (!string.IsNullOrEmpty(role) &&
                    !roles.Contains(role))
                {
                    continue;
                }

                if (status == "locked" && !locked)
                    continue;

                if (status == "active" && locked)
                    continue;

                if (status == "unconfirmed" && user.EmailConfirmed)
                    continue;

                rows.Add(new UserRowViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    IsAdmin = roles.Contains("Admin"),
                    IsLocked = locked,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles.ToList()
                });
            }

            var model = new UserListViewModel
            {
                Users = rows,
                AllRoles = await GetAllRoleNamesAsync(),
                Search = search,
                Role = role,
                Status = status,
                CurrentUserId = _userManager.GetUserId(User) ?? string.Empty,
                PendingCount = await _userManager.Users
                    .CountAsync(u => !u.EmailConfirmed)
            };

            return View(model);
        }

        // =========================================================
        // CREATE USER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            return View(new UserCreateViewModel
            {
                AllRoles = await GetAllRoleNamesAsync()
            });
        }

        // =========================================================
        // CREATE USER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            UserCreateViewModel model)
        {
            if (model.SelectedRoles == null ||
                model.SelectedRoles.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedRoles),
                    "Choose at least one role.");
            }

            if (!string.IsNullOrWhiteSpace(model.UserName))
            {
                if (!UserNameRegex.IsMatch(model.UserName))
                {
                    ModelState.AddModelError(
                        nameof(model.UserName),
                        "Use 3 to 20 letters, numbers, dot, dash or underscore.");
                }
                else if (await _userManager.FindByNameAsync(model.UserName) != null)
                {
                    ModelState.AddModelError(
                        nameof(model.UserName),
                        "This username is already taken.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Email) &&
                await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered.");
            }

            if (model.SelectedRoles != null)
            {
                foreach (var role in model.SelectedRoles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        ModelState.AddModelError(
                            nameof(model.SelectedRoles),
                            $"Role '{role}' does not exist.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                model.AllRoles = await GetAllRoleNamesAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                MiddelName = string.Empty,
                LastName = model.LastName,
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
                        string.Empty,
                        error.Description);
                }

                model.AllRoles = await GetAllRoleNamesAsync();
                return View(model);
            }

            var roleResult = await _userManager.AddToRolesAsync(
                user,
                model.SelectedRoles!);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                // Remove the newly created user if role assignment fails.
                await _userManager.DeleteAsync(user);

                model.AllRoles = await GetAllRoleNamesAsync();
                return View(model);
            }

            await _activityLogService.LogAsync(
                "Admin",
                "CreateUser",
                "ApplicationUser",
                user.Id,
                $"Created user: {user.Email}");

            TempData["Success"] =
                $"User '{user.UserName}' was created successfully.";

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

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                SelectedRoles = (await _userManager.GetRolesAsync(user))
                    .ToList()
            };

            await FillEditInfoAsync(model, user);

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
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var isMe = user.Id == _userManager.GetUserId(User);

            if (model.SelectedRoles == null ||
                model.SelectedRoles.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedRoles),
                    "Choose at least one role.");
            }

            // Validate username format only when it changes.
            if (model.UserName != user.UserName &&
                !UserNameRegex.IsMatch(model.UserName ?? string.Empty))
            {
                ModelState.AddModelError(
                    nameof(model.UserName),
                    "Use 3 to 20 letters, numbers, dot, dash or underscore.");
            }

            var sameName = await _userManager.FindByNameAsync(
                model.UserName);

            if (sameName != null && sameName.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.UserName),
                    "This username is already taken.");
            }

            var sameEmail = await _userManager.FindByEmailAsync(
                model.Email);

            if (sameEmail != null && sameEmail.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered.");
            }

            if (model.SelectedRoles != null)
            {
                foreach (var role in model.SelectedRoles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        ModelState.AddModelError(
                            nameof(model.SelectedRoles),
                            $"Role '{role}' does not exist.");
                    }
                }
            }

            // Never leave the system without an admin.
            if (currentRoles.Contains("Admin") &&
                model.SelectedRoles != null &&
                !model.SelectedRoles.Contains("Admin"))
            {
                if (isMe)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "You can't remove the Admin role from your own account.");
                }
                else if (await IsLastAdminAsync(user))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "This is the last admin. The system needs at least one.");
                }
            }

            if (!ModelState.IsValid)
            {
                await FillEditInfoAsync(model, user);
                return View(model);
            }

            // Update basic information.
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.EmailConfirmed = model.EmailConfirmed;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await FillEditInfoAsync(model, user);
                return View(model);
            }

            // Update roles.
            var toRemove = currentRoles
                .Except(model.SelectedRoles!)
                .ToList();

            var toAdd = model.SelectedRoles!
                .Except(currentRoles)
                .ToList();

            if (toRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user,
                    toRemove);

                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    await FillEditInfoAsync(model, user);
                    return View(model);
                }
            }

            if (toAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(
                    user,
                    toAdd);

                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    await FillEditInfoAsync(model, user);
                    return View(model);
                }
            }

            // Optional new password.
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager
                    .GeneratePasswordResetTokenAsync(user);

                var passwordResult = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(
                            nameof(model.NewPassword),
                            error.Description);
                    }

                    await FillEditInfoAsync(model, user);
                    return View(model);
                }
            }

            // Refresh the user's security stamp when editing another user.
            if (!isMe)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            await _activityLogService.LogAsync(
                "Admin",
                "EditUser",
                "ApplicationUser",
                user.Id,
                $"Updated user: {user.Email}");

            TempData["Success"] =
                $"User '{user.UserName}' was updated successfully.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // DELETE USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You can't delete your own account.";

                return RedirectToAction(nameof(EditUser), new { id });
            }

            if (await _userManager.IsInRoleAsync(user, "Admin") &&
                await IsLastAdminAsync(user))
            {
                TempData["Error"] =
                    "This is the last admin. The system needs at least one.";

                return RedirectToAction(nameof(EditUser), new { id });
            }

            // Keep users who have bookings.
            if (await CountBookingsAsync(user.Id) > 0)
            {
                TempData["Error"] =
                    "This user has bookings, so he can't be deleted. Lock the account instead.";

                return RedirectToAction(nameof(EditUser), new { id });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(EditUser), new { id });
            }

            await _activityLogService.LogAsync(
                "Admin",
                "DeleteUser",
                "ApplicationUser",
                user.Id,
                $"Deleted user: {user.Email}");

            TempData["Success"] =
                $"User '{user.UserName}' was deleted.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // CONFIRM ACCOUNT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _activityLogService.LogAsync(
                    "Admin",
                    "ConfirmAccount",
                    "ApplicationUser",
                    user.Id,
                    $"Confirmed account: {user.Email}");

                TempData["Success"] =
                    $"Account '{user.UserName}' was confirmed. The user can log in now.";
            }
            else
            {
                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // TOGGLE ADMIN ROLE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var isMe = user.Id == _userManager.GetUserId(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isMe && isAdmin)
            {
                TempData["Error"] =
                    "You can't remove the Admin role from your own account.";

                return RedirectToAction(nameof(Users));
            }

            if (isAdmin && await IsLastAdminAsync(user))
            {
                TempData["Error"] =
                    "This is the last admin. The system needs at least one.";

                return RedirectToAction(nameof(Users));
            }

            IdentityResult result;

            if (isAdmin)
            {
                result = await _userManager.RemoveFromRoleAsync(
                    user,
                    "Admin");

                if (result.Succeeded &&
                    !await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    result = await _userManager.AddToRoleAsync(
                        user,
                        "Customer");
                }
            }
            else
            {
                result = await _userManager.AddToRoleAsync(
                    user,
                    "Admin");
            }

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(
                    ", ",
                    result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Users));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            var newRole = isAdmin ? "Customer" : "Admin";

            await _activityLogService.LogAsync(
                "Admin",
                "ToggleAdmin",
                "ApplicationUser",
                user.Id,
                $"Changed role for {user.Email} to {newRole}");

            TempData["Success"] =
                $"{user.Email} is now a {newRole}.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // LOCK USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(
            string id,
            string? duration)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] =
                    "You can't lock your own account.";

                return RedirectToAction(nameof(Users));
            }

            DateTimeOffset until = duration switch
            {
                "1h" => DateTimeOffset.UtcNow.AddHours(1),
                "1d" => DateTimeOffset.UtcNow.AddDays(1),
                "7d" => DateTimeOffset.UtcNow.AddDays(7),
                _ => DateTimeOffset.UtcNow.AddYears(100)
            };

            await _userManager.SetLockoutEnabledAsync(user, true);

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                until);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(
                    ", ",
                    result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Users));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _activityLogService.LogAsync(
                "Admin",
                "LockUser",
                "ApplicationUser",
                user.Id,
                $"Locked user: {user.Email}");

            TempData["Success"] =
                $"User '{user.UserName}' was locked.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // UNLOCK USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            await _activityLogService.LogAsync(
                "Admin",
                "UnlockUser",
                "ApplicationUser",
                user.Id,
                $"Unlocked user: {user.Email}");

            TempData["Success"] =
                $"User '{user.UserName}' was unlocked.";

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private async Task<List<string>> GetAllRoleNamesAsync()
        {
            return await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();
        }

        private async Task<bool> IsLastAdminAsync(
            ApplicationUser user)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            return admins.Count <= 1 &&
                   admins.Any(a => a.Id == user.Id);
        }

        private Task<int> CountBookingsAsync(string userId)
        {
            return _db.Bookings.CountAsync(
                booking => booking.UserId == userId);
        }

        private async Task FillEditInfoAsync(
            UserEditViewModel model,
            ApplicationUser user)
        {
            model.AllRoles = await GetAllRoleNamesAsync();

            model.IsLocked = user.LockoutEnd.HasValue &&
                             user.LockoutEnd > DateTimeOffset.UtcNow;

            model.TwoFactorEnabled = user.TwoFactorEnabled;

            model.IsCurrentUser =
                user.Id == _userManager.GetUserId(User);

            model.BookingsCount = await CountBookingsAsync(user.Id);
        }
    }
}
