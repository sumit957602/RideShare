using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using System.Security.Claims;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    public class AdminAccountController : Controller
    {
        private readonly IAdminAccountService _adminAccountService;
        private readonly ILogger<AdminAccountController> _logger;

        public AdminAccountController(IAdminAccountService adminAccountService, ILogger<AdminAccountController> logger)
        {
            _adminAccountService = adminAccountService;
            _logger = logger;
        }

        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var authResponse = await _adminAccountService.LoginAsync(model);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResponse.AdminId.ToString()),
                    new Claim(ClaimTypes.Name, authResponse.Username),
                    new Claim("Token", authResponse.Token),
                    new Claim(ClaimTypes.Role, authResponse.Role ?? "Admin")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Admin", "AdminDashboard");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Admin login failed for {Username}", model.Username);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while logging in.");
                _logger.LogError(ex, "Unexpected error during admin login for {Username}", model.Username);
                return View(model);
            }
        }
    }
}
