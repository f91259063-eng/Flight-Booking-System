
using System.Text;
using System.Text.Encodings.Web;
using Flight_Booking_System.Areas.Identity.ViewModels;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.Services;
using Flight_Booking_System.Services.Rewards;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Flight_Booking_System.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAppEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;
        private readonly IRewardService _rewardService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAppEmailSender emailSender,
            IWebHostEnvironment env,
            IRewardService rewardService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _env = env;
            _rewardService = rewardService;
        }

        // =====================================================
        // Register + email confirmation
        // =====================================================

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                MiddelName = model.MiddleName ?? string.Empty,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Create Customer role assignment
                await _userManager.AddToRoleAsync(user, "Customer");

                // Create Reward Account automatically with zero points
                await _rewardService.EnsureAccountAsync(user.Id);

                var link = await SendConfirmationEmailAsync(user);
                TempData["DevLink"] = _env.IsDevelopment() ? link : null;

                return RedirectToAction(nameof(RegisterConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View("Message", new MessageViewModel
            {
                Title = "Check your email",
                Message = "We sent you a confirmation link. Click it to activate your account, then log in.",
                DevLink = TempData["DevLink"] as string,
                LinkText = "Back to login",
                LinkUrl = LoginUrl()
            });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
        {
            if (userId is null || !TryDecode(code, out var token))
                return View("Message", Error("Invalid link", "This confirmation link is invalid or incomplete."));

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return View("Message", Error("Invalid link", "We couldn't find an account for this link."));

            var result = await _userManager.ConfirmEmailAsync(user, token);

            return View("Message", result.Succeeded
                ? new MessageViewModel
                {
                    Title = "Email confirmed",
                    Message = "Thank you! Your email is confirmed. You can log in now.",
                    LinkText = "Log in",
                    LinkUrl = LoginUrl()
                }
                : Error("Confirmation failed", "The link is invalid or has expired. Request a new one from the login page."));
        }

        [HttpGet]
        public IActionResult ResendConfirmation() => View(new ResendConfirmationViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string? devLink = null;
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                var link = await SendConfirmationEmailAsync(user);

                if (_env.IsDevelopment())
                    devLink = link;
            }

            return View("Message", new MessageViewModel
            {
                Title = "Check your email",
                Message = "If that account exists and is not confirmed yet, we sent a new confirmation link.",
                DevLink = devLink,
                LinkText = "Back to login",
                LinkUrl = LoginUrl()
            });
        }

        // =====================================================
        // Login / 2FA / Logout
        // =====================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Redirect("/Admin/Home/Index");
                }

                return Redirect("/Customer/Flights/Index");
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(
                    nameof(LoginWith2fa),
                    new
                    {
                        rememberMe = model.RememberMe,
                        returnUrl = model.ReturnUrl
                    });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Account locked after too many failed attempts. Try again in a few minutes.");

                return View(model);
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You need to confirm your email before logging in.");

                ViewData["ShowResend"] = true;

                return View(model);
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(
            bool rememberMe,
            string? returnUrl = null)
        {
            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
                return RedirectToAction(nameof(Login));

            return View(new TwoFactorLoginViewModel
            {
                RememberMe = rememberMe,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
                return RedirectToAction(nameof(Login));

            var code = model.TwoFactorCode
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code,
                model.RememberMe,
                model.RememberMachine);

            if (result.Succeeded)
                return RedirectToLocal(model.ReturnUrl);

            ModelState.AddModelError(
                string.Empty,
                result.IsLockedOut
                    ? "Account locked. Try again in a few minutes."
                    : "Invalid authenticator code.");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
        {
            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
                return RedirectToAction(nameof(Login));

            return View(new RecoveryCodeLoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(
            RecoveryCodeLoginViewModel model)
        {
            var code = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

            if (result.Succeeded)
                return RedirectToLocal(model.ReturnUrl);

            ModelState.AddModelError(
                string.Empty,
                result.IsLockedOut
                    ? "Account locked. Try again in a few minutes."
                    : "Invalid recovery code.");

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // =====================================================
        // Forgot / reset password
        // =====================================================

        [HttpGet]
        public IActionResult ForgotPassword()
            => View(new ForgotPasswordViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string? devLink = null;

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var code = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                var link = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { area = "Identity", code, email = model.Email },
                    Request.Scheme)!;

                await _emailSender.SendAsync(
                    user.Email!,
                    "Reset your password",
                    $"<p>Hi {HtmlEncoder.Default.Encode(user.FirstName)},</p>" +
                    $"<p>You can reset your password by <a href=\"{HtmlEncoder.Default.Encode(link)}\">clicking here</a>.</p>" +
                    "<p>If you didn't ask for this, you can ignore this email.</p>");

                if (_env.IsDevelopment())
                    devLink = link;
            }

            return View("Message", new MessageViewModel
            {
                Title = "Check your email",
                Message = "If an account with that email exists and is confirmed, we sent a link to reset the password.",
                DevLink = devLink,
                LinkText = "Back to login",
                LinkUrl = LoginUrl()
            });
        }

        [HttpGet]
        public IActionResult ResetPassword(
            string? code = null,
            string? email = null)
        {
            if (!TryDecode(code, out _))
            {
                return View("Message", Error(
                    "Invalid link",
                    "This password reset link is invalid or incomplete."));
            }

            return View(new ResetPasswordViewModel
            {
                Code = code!,
                Email = email ?? string.Empty
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!TryDecode(model.Code, out var token))
            {
                return View("Message", Error(
                    "Invalid link",
                    "This password reset link is invalid or incomplete."));
            }

            var done = new MessageViewModel
            {
                Title = "Password reset",
                Message = "Your password was changed. You can log in with the new one.",
                LinkText = "Log in",
                LinkUrl = LoginUrl()
            };

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return View("Message", done);

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                model.Password);

            if (result.Succeeded)
                return View("Message", done);

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // =====================================================
        // My Account
        // =====================================================

        
        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var availablePoints =
                await _rewardService.GetAvailablePointsAsync(user.Id);

            var model = new MyAccountViewModel
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddelName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,

                AvailablePoints = availablePoints
            };

            return View(model);
        }

        // =====================================================
        // Helpers
        // =====================================================

        private async Task<string> SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token));

            var link = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new
                {
                    area = "Identity",
                    userId = user.Id,
                    code
                },
                Request.Scheme)!;

            await _emailSender.SendAsync(
                user.Email!,
                "Confirm your email",
                $"<p>Hi {HtmlEncoder.Default.Encode(user.FirstName)},</p>" +
                $"<p>Please confirm your account by <a href=\"{HtmlEncoder.Default.Encode(link)}\">clicking here</a>.</p>");

            return link;
        }

        private static bool TryDecode(string? code, out string decoded)
        {
            decoded = string.Empty;

            if (string.IsNullOrEmpty(code))
                return false;

            try
            {
                decoded = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(code));

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private string LoginUrl()
        {
            return Url.Action(
                nameof(Login),
                "Account",
                new { area = "Identity" })!;
        }

        private static MessageViewModel Error(string title, string message)
        {
            return new MessageViewModel
            {
                Title = title,
                Message = message,
                IsSuccess = false
            };
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
