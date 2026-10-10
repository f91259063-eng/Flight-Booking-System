using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
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
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                    TwoFactorEnabled = u.TwoFactorEnabled
                });
            }

            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't change your own role.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "Customer");
                TempData["Success"] = $"{user.Email} is now a Customer.";
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(user, "Customer");
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"{user.Email} is now an Admin.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't lock your own account.";
                return RedirectToAction(nameof(Users));
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, isLocked ? null : DateTimeOffset.UtcNow.AddYears(100));

            TempData["Success"] = isLocked ? $"{user.Email} was unlocked." : $"{user.Email} was locked.";
            return RedirectToAction(nameof(Users));
        }
    }
}
