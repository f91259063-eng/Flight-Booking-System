using System.Text.RegularExpressions;
using Flight_Booking_System.Data;
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
        private static readonly Regex UserNameRegex = new(@"^[A-Za-z0-9._-]{3,20}$");

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // =========================================================
        // USERS LIST (with search and filters)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Users(string? search, string? role, string? status)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    u.UserName!.Contains(search) ||
                    u.Email!.Contains(search) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.PhoneNumber!.Contains(search));
            }

            var users = await query.OrderBy(u => u.UserName).ToListAsync();
            var rows = new List<UserRowViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var locked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

                if (!string.IsNullOrEmpty(role) && !roles.Contains(role))
                    continue;

                if (status == "locked" && !locked) continue;
                if (status == "active" && locked) continue;
                if (status == "unconfirmed" && u.EmailConfirmed) continue;

                rows.Add(new UserRowViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    IsLocked = locked,
                    LockoutEnd = u.LockoutEnd,
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
                PendingCount = await _userManager.Users.CountAsync(u => !u.EmailConfirmed)
            };

            return View(model);
        }

        // =========================================================
        // CREATE USER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            return View(new UserCreateViewModel
            {
                AllRoles = await GetAllRoleNamesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (model.SelectedRoles.Count == 0)
                ModelState.AddModelError(string.Empty, "Choose at least one role.");

            if (await _userManager.FindByNameAsync(model.UserName) is not null)
                ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");

            if (await _userManager.FindByEmailAsync(model.Email) is not null)
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

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
                EmailConfirmed = true,      // the admin vouches for this account
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                model.AllRoles = await GetAllRoleNamesAsync();
                return View(model);
            }

            await AddRolesAsync(user, model.SelectedRoles);

            TempData["Success"] = $"User '{user.UserName}' was created successfully.";
            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // EDIT USER
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
                SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList()
            };

            await FillEditInfoAsync(model, user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var isMe = user.Id == _userManager.GetUserId(User);

            if (model.SelectedRoles.Count == 0)
                ModelState.AddModelError(string.Empty, "Choose at least one role.");

            // the format is checked only when the username is changed
            if (model.UserName != user.UserName && !UserNameRegex.IsMatch(model.UserName))
                ModelState.AddModelError(nameof(model.UserName), "Use 3 to 20 letters, numbers, dot, dash or underscore.");

            var sameName = await _userManager.FindByNameAsync(model.UserName);
            if (sameName is not null && sameName.Id != user.Id)
                ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");

            var sameEmail = await _userManager.FindByEmailAsync(model.Email);
            if (sameEmail is not null && sameEmail.Id != user.Id)
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

            // never leave the system without an admin
            if (currentRoles.Contains("Admin") && !model.SelectedRoles.Contains("Admin"))
            {
                if (isMe)
                {
                    ModelState.AddModelError(string.Empty, "You can't remove the Admin role from your own account.");
                }
                else if (await IsLastAdminAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "This is the last admin. The system needs at least one.");
                }
            }

            if (!ModelState.IsValid)
            {
                await FillEditInfoAsync(model, user);
                return View(model);
            }

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
                    ModelState.AddModelError(string.Empty, error.Description);

                await FillEditInfoAsync(model, user);
                return View(model);
            }

            // roles: remove the unticked ones, add the new ones
            var toRemove = currentRoles.Except(model.SelectedRoles).ToList();
            var toAdd = model.SelectedRoles.Except(currentRoles).ToList();

            if (toRemove.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            await AddRolesAsync(user, toAdd);

            // optional new password
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                        ModelState.AddModelError(nameof(model.NewPassword), error.Description);

                    await FillEditInfoAsync(model, user);
                    return View(model);
                }
            }

            // makes the user's open sessions check his new roles
            // (not for yourself, or you would be logged out)
            if (!isMe)
                await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"User '{user.UserName}' was updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // DELETE USER (button on the edit page)
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
                TempData["Error"] = "You can't delete your own account.";
                return RedirectToAction(nameof(EditUser), new { id });
            }

            if (await _userManager.IsInRoleAsync(user, "Admin") && await IsLastAdminAsync(user))
            {
                TempData["Error"] = "This is the last admin. The system needs at least one.";
                return RedirectToAction(nameof(EditUser), new { id });
            }

            // the bookings belong to the user, so we keep the user and lock him instead
            if (await CountBookingsAsync(user.Id) > 0)
            {
                TempData["Error"] = "This user has bookings, so he can't be deleted. Lock the account instead.";
                return RedirectToAction(nameof(EditUser), new { id });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(EditUser), new { id });
            }

            TempData["Success"] = $"User '{user.UserName}' was deleted.";
            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // CONFIRM ACCOUNT (the admin confirms a new account so the user can log in)
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
                TempData["Success"] = $"Account '{user.UserName}' was confirmed. The user can log in now.";
            else
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // LOCK / UNLOCK
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id, string duration)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't lock your own account.";
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
            await _userManager.SetLockoutEndDateAsync(user, until);
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"User '{user.UserName}' was locked.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = $"User '{user.UserName}' was unlocked.";
            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // Helpers
        // =========================================================

        private async Task<List<string>> GetAllRoleNamesAsync()
        {
            return await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();
        }

        // adds only roles that really exist
        private async Task AddRolesAsync(ApplicationUser user, IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                    await _userManager.AddToRoleAsync(user, role);
            }
        }

        private async Task<bool> IsLastAdminAsync(ApplicationUser user)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            return admins.Count <= 1 && admins.Any(a => a.Id == user.Id);
        }

        private Task<int> CountBookingsAsync(string userId)
        {
            return _db.Bookings.CountAsync(b => b.UserId == userId || b.ApplicationUser.Id == userId);
        }

        private async Task FillEditInfoAsync(UserEditViewModel model, ApplicationUser user)
        {
            model.AllRoles = await GetAllRoleNamesAsync();
            model.IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
            model.TwoFactorEnabled = user.TwoFactorEnabled;
            model.IsCurrentUser = user.Id == _userManager.GetUserId(User);
            model.BookingsCount = await CountBookingsAsync(user.Id);
        }
    }
}
