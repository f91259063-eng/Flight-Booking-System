using System.Text;
using System.Text.Encodings.Web;
using Flight_Booking_System.Areas.Identity.ViewModels;
using Flight_Booking_System.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Flight_Booking_System.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class ManageController : Controller
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ---------- Profile ----------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            return View(new ProfileViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddelName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            // Email is display-only; always show the saved one
            model.Email = user.Email;
            model.EmailConfirmed = user.EmailConfirmed;

            if (!ModelState.IsValid)
                return View(model);

            user.FirstName = model.FirstName;
            user.MiddelName = model.MiddleName ?? string.Empty;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Your profile was updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Change password ----------

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Your password was changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        // ---------- Two-factor authentication ----------

        [HttpGet]
        public async Task<IActionResult> TwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            return View(new TwoFactorStatusViewModel
            {
                Is2faEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user)
            });
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var model = new EnableAuthenticatorViewModel();
            await LoadKeyAsync(user, model);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadKeyAsync(user, model);
                return View(model);
            }

            var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.Code), "Verification code is invalid.");
                await LoadKeyAsync(user, model);
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.RefreshSignInAsync(user);

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                TempData["RecoveryCodes"] = string.Join(";", codes!);
                return RedirectToAction(nameof(ShowRecoveryCodes));
            }

            TempData["Success"] = "Authenticator app verified. Two-factor authentication is on.";
            return RedirectToAction(nameof(TwoFactor));
        }

        [HttpGet]
        public IActionResult ShowRecoveryCodes()
        {
            var joined = TempData["RecoveryCodes"] as string;
            if (string.IsNullOrEmpty(joined))
                return RedirectToAction(nameof(TwoFactor));

            return View(joined.Split(';'));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            if (!user.TwoFactorEnabled)
            {
                TempData["Error"] = "Turn on two-factor authentication first.";
                return RedirectToAction(nameof(TwoFactor));
            }

            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData["RecoveryCodes"] = string.Join(";", codes!);
            return RedirectToAction(nameof(ShowRecoveryCodes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);   // next time a fresh key is created
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Two-factor authentication is off.";
            return RedirectToAction(nameof(TwoFactor));
        }

        // ---------- Helpers ----------

        private async Task LoadKeyAsync(ApplicationUser user, EnableAuthenticatorViewModel model)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            model.SharedKey = FormatKey(key!);
            model.AuthenticatorUri = string.Format(
                AuthenticatorUriFormat,
                UrlEncoder.Default.Encode("Flight Booking"),
                UrlEncoder.Default.Encode(user.Email!),
                key);
        }

        // "abcdefghijkl" -> "abcd efgh ijkl" (easier to type by hand)
        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var position = 0;

            while (position + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(position, 4)).Append(' ');
                position += 4;
            }

            if (position < unformattedKey.Length)
                result.Append(unformattedKey.AsSpan(position));

            return result.ToString().ToLowerInvariant();
        }
    }
}
