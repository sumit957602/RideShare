using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using System.Security.Claims;
using System.Collections.Generic;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    public class DriverAccountController : Controller
    {
        private readonly IDriverAccountService _driverAccountService;
        private readonly ILogger<DriverAccountController> _logger;
        public DriverAccountController(IDriverAccountService driverAccountService, ILogger<DriverAccountController> logger)
        {
            _driverAccountService = driverAccountService;
            _logger = logger;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DriverRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _driverAccountService.RegisterAsync(model);
                TempData["SuccessMessage"] = "Driver registered successfully! You can now log in.";
                return RedirectToAction("Login");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Driver registration failed for {Email}", model.Email);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while registering.");
                _logger.LogError(ex, "Unexpected error during driver registration for {Email}", model.Email);
                return View(model);
            }
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var authResponse = await _driverAccountService.LoginAsync(model);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                    new Claim(ClaimTypes.Email, authResponse.Email),
                    new Claim("Token", authResponse.Token),
                    new Claim(ClaimTypes.Role, authResponse.UserType ?? "Driver")
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Driver", "DriverDashboard");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Driver login failed for {Email}", model.Email);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while logging in.");
                _logger.LogError(ex, "Unexpected error during driver login for {Email}", model.Email);
                return View(model);
            }
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                await _driverAccountService.ForgotPasswordAsync(dto);
                TempData["SuccessMessage"] = "A password reset OTP has been sent to your email. Please check your inbox to continue.";
                TempData["UserEmailForReset"] = dto.Email;
                return RedirectToAction("ResetPassword");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        public IActionResult ResetPassword()
        {
            // Retrieve the email from TempData
            var email = TempData["UserEmailForReset"] as string;
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Password reset process not initiated or expired. Please request a new OTP.";
                return RedirectToAction("ForgotPassword");
            }
            // Keep the email in TempData for subsequent postback if validation fails
            TempData.Keep("UserEmailForReset");
            return View(new ResetPasswordDto { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData.Keep("UserEmailForReset"); // Keep email if validation fails
                return View(dto);
            }

            try
            {
                await _driverAccountService.ResetPasswordAsync(dto);
                TempData["SuccessMessage"] = "Password reset successful. Please log in with your new password.";
                return RedirectToAction("Login");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData.Keep("UserEmailForReset");
                return View(dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while resetting the password.");
                _logger.LogError(ex, "Unexpected error during driver password reset for {Email}", dto.Email);
                TempData.Keep("UserEmailForReset");
                return View(dto);
            }
        }
    }
}
