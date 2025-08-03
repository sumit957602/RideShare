using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Collections.Generic;

namespace RideShare_Connect.Controllers
{
    public class DriverAccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DriverAccountController> _logger;
        public DriverAccountController(IHttpClientFactory httpClientFactory, ILogger<DriverAccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(model.Email), "Email");
            if (!string.IsNullOrEmpty(model.PhoneNumber))
                formData.Add(new StringContent(model.PhoneNumber), "PhoneNumber");
            formData.Add(new StringContent(model.Password), "Password");
            formData.Add(new StringContent(model.FullName), "FullName");
            formData.Add(new StringContent(model.LicenseNumber), "LicenseNumber");
            formData.Add(new StringContent(model.DrivingExperienceYears.ToString()), "DrivingExperienceYears");
            if (!string.IsNullOrEmpty(model.ReferenceContacts))
                formData.Add(new StringContent(model.ReferenceContacts), "ReferenceContacts");
            if (model.LicenseDocument != null)
            {
                var fileContent = new StreamContent(model.LicenseDocument.OpenReadStream());
                formData.Add(fileContent, "LicenseDocument", model.LicenseDocument.FileName);
            }
            try
            {
                var response = await httpClient.PostAsync("api/drivers/register", formData);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Login");
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"API Error: {response.StatusCode} - {errorContent}");
                _logger.LogError("Driver registration failed for {Email}: {Status} - {Error}", model.Email, response.StatusCode, errorContent);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}");
                _logger.LogError(ex, "HTTP request error during driver registration for {Email}", model.Email);
            }
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("api/drivers/login", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim("Token", authResponse.Token),
                        new Claim(ClaimTypes.Role, authResponse.UserType ?? "Driver"),
                        new Claim("IsVerified", authResponse.IsVerified.ToString()),
                        new Claim("VerificationBadge", authResponse.VerificationBadge ?? string.Empty)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                    };
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Driver", "DriverDashboard");
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Login failed: {response.StatusCode} - {errorContent}");
                _logger.LogError("Driver login failed for {Email}: {Status} - {Error}", model.Email, response.StatusCode, errorContent);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}");
                _logger.LogError(ex, "HTTP request error during driver login for {Email}", model.Email);
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error parsing API response: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error during driver login for {Email}", model.Email);
            }
            return View(model);
        }
    }
}
