using System.Text;
using System.Text.RegularExpressions;
using Flight_Booking_System.Areas.Identity.ViewModels;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Flight_Booking_System.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private static readonly Regex UserNameRegex = new(@"^[A-Za-z0-9._-]{3,20}$");

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAppEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly RegistrationSettings _registration;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAppEmailSender emailSender,
            ILogger<AccountController> logger,
            IOptions<RegistrationSettings> registration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _registration = registration.Value;
        }

        // true: the administrator confirms the new accounts (no email is sent)
        private bool AdminConfirms =>
            string.Equals(_registration.ConfirmationMode, "Admin", StringComparison.OrdinalIgnoreCase);

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

            if (await _userManager.FindByNameAsync(model.UserName) is not null)
            {
                ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                MiddelName = string.Empty,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // anyone who registers from the site is a customer
                await _userManager.AddToRoleAsync(user, AppRoles.Customer);

                if (AdminConfirms)
                {
                    // no email: the administrator confirms the account from the users page
                    TempData["WaitingForAdmin"] = true;
                }
                else
                {
                    var sent = await TrySendConfirmationEmailAsync(user);
                    TempData["EmailFailed"] = !sent;
                }

                return RedirectToAction(nameof(RegisterConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // Called by the register form while the user types (remote validation)
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UserNameRegex.IsMatch(userName))
                return Json("Use 3 to 20 letters, numbers, dot, dash or underscore.");

            if (await _userManager.FindByNameAsync(userName) is null)
                return Json(true);

            // taken: look for a free name close to it
            var random = new Random();
            var baseName = userName.Length > 17 ? userName.Substring(0, 17) : userName;

            for (var i = 0; i < 10; i++)
            {
                var candidate = baseName + random.Next(10, 99);
                if (await _userManager.FindByNameAsync(candidate) is null)
                    return Json($"'{userName}' is already taken. Try '{candidate}'.");
            }

            return Json($"'{userName}' is already taken.");
        }

        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            if (TempData["WaitingForAdmin"] is true)
            {
                return View("Message", new MessageViewModel
                {
                    Title = "Account created",
                    Message = "Thank you! An administrator will confirm your account. After that you can log in.",
                    LinkText = "Back to login",
                    LinkUrl = LoginUrl()
                });
            }

            var failed = TempData["EmailFailed"] is true;

            return View("Message", new MessageViewModel
            {
                Title = failed ? "Account created" : "Check your email",
                Message = failed
                    ? "Your account was created, but we could not send the confirmation email. Use \"Resend confirmation\" on the login page in a few minutes."
                    : "We sent you a confirmation link. Click it to activate your account, then log in.",
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

                        if (AdminConfirms)
            {
                return View("Message", new MessageViewModel
                {
                    Title = "Waiting for the administrator",
                    Message = "New accounts are confirmed by the administrator. Please try to log in again later.",
                    LinkText = "Back to login",
                    LinkUrl = LoginUrl()
                });
            }

var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
                await TrySendConfirmationEmailAsync(user);

            // Same answer whether or not the email exists (don't leak which emails are registered)
            return View("Message", new MessageViewModel
            {
                Title = "Check your email",
                Message = "If that account exists and is not confirmed yet, we sent a new confirmation link.",
                LinkText = "Back to login",
                LinkUrl = LoginUrl()
            });
        }

        // =====================================================
        // Login / 2FA / Logout
        // =====================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // the user can type his username or his email
            var input = model.UserNameOrEmail.Trim();
            var user = input.Contains('@')
                ? await _userManager.FindByEmailAsync(input)
                : await _userManager.FindByNameAsync(input);

            if (user is not null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                    return await RedirectAfterLoginAsync(user, model.ReturnUrl);

                if (result.RequiresTwoFactor)
                    return RedirectToAction(nameof(LoginWith2fa),
                        new { rememberMe = model.RememberMe, returnUrl = model.ReturnUrl });

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "This account is locked. Try again later or contact the administrator.");
                    return View(model);
                }

                if (result.IsNotAllowed)
                {
                    if (AdminConfirms)
                    {
                        ModelState.AddModelError(string.Empty, "Your account is waiting for the administrator to confirm it. Please try again later.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "You need to confirm your email before logging in.");
                        ViewData["ShowResend"] = true;
                    }

                    return View(model);
                }
            }

            // Same message for "no such user" and "wrong password"
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
                return RedirectToAction(nameof(Login));

            return View(new TwoFactorLoginViewModel { RememberMe = rememberMe, ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
                return RedirectToAction(nameof(Login));

            var code = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code, model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
                return await RedirectAfterLoginAsync(user, model.ReturnUrl);

            ModelState.AddModelError(string.Empty,
                result.IsLockedOut ? "This account is locked. Try again later." : "Invalid authenticator code.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
        {
            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() is null)
                return RedirectToAction(nameof(Login));

            return View(new RecoveryCodeLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
                return RedirectToAction(nameof(Login));

            var code = model.RecoveryCode.Replace(" ", string.Empty);
            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

            if (result.Succeeded)
                return await RedirectAfterLoginAsync(user, model.ReturnUrl);

            ModelState.AddModelError(string.Empty,
                result.IsLockedOut ? "This account is locked. Try again later." : "Invalid recovery code.");
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // =====================================================
        // Forgot / reset password
        // =====================================================

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    var link = Url.Action(nameof(ResetPassword), "Account",
                        new { area = "Identity", code, email = model.Email }, Request.Scheme)!;

                    var body = EmailTemplates.WithButton(user.FirstName,
                        "We received a request to reset your password.", "Reset my password", link);

                    await _emailSender.SendAsync(user.Email!, "Reset your password", body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not send the password reset email to {Email}", model.Email);
                }
            }

            // Same answer whether or not the email exists
            return View("Message", new MessageViewModel
            {
                Title = "Check your email",
                Message = "If an account with that email exists and is confirmed, we sent a link to reset the password.",
                LinkText = "Back to login",
                LinkUrl = LoginUrl()
            });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (!TryDecode(code, out _))
                return View("Message", Error("Invalid link", "This password reset link is invalid or incomplete."));

            return View(new ResetPasswordViewModel { Code = code!, Email = email ?? string.Empty });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!TryDecode(model.Code, out var token))
                return View("Message", Error("Invalid link", "This password reset link is invalid or incomplete."));

            var done = new MessageViewModel
            {
                Title = "Password reset",
                Message = "Your password was changed. You can log in with the new one.",
                LinkText = "Log in",
                LinkUrl = LoginUrl()
            };

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
                return View("Message", done);   // don't reveal that the email doesn't exist

            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (result.Succeeded)
                return View("Message", done);

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // =====================================================
        // Helpers
        // =====================================================

        // Returns false if the email could not be sent (the error is logged)
        private async Task<bool> TrySendConfirmationEmailAsync(ApplicationUser user)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var link = Url.Action(nameof(ConfirmEmail), "Account",
                    new { area = "Identity", userId = user.Id, code }, Request.Scheme)!;

                var body = EmailTemplates.WithButton(user.FirstName,
                    "Thanks for registering. Please confirm your email address to activate your account.",
                    "Confirm my email", link);

                await _emailSender.SendAsync(user.Email!, "Confirm your email", body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the confirmation email to {Email}", user.Email);
                return false;
            }
        }

        // After login: admins go to the admin panel, employees to the staff desk, customers to the home page
        private async Task<IActionResult> RedirectAfterLoginAsync(ApplicationUser user, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains(AppRoles.Admin))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (roles.Contains(AppRoles.Receptionist) || roles.Contains(AppRoles.BookingAgent))
                return RedirectToAction("Index", "Home", new { area = "Staff" });

            // customers (and booking agents) start from the flights page
            return RedirectToAction("Index", "Flights", new { area = "Customer" });
        }

        private static bool TryDecode(string? code, out string decoded)
        {
            decoded = string.Empty;
            if (string.IsNullOrEmpty(code)) return false;

            try
            {
                decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private string LoginUrl() => Url.Action(nameof(Login), "Account", new { area = "Identity" })!;

        private static MessageViewModel Error(string title, string message)
            => new() { Title = title, Message = message, IsSuccess = false };
    }
}
