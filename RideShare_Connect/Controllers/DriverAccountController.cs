using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
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
            formData.Add(new StringContent(model.ConfirmPassword), "ConfirmPassword");
            formData.Add(new StringContent(model.FullName), "FullName");
            formData.Add(new StringContent(model.LicenseNumber), "LicenseNumber");
            formData.Add(new StringContent(model.DrivingExperienceYears.ToString()), "DrivingExperienceYears");
            formData.Add(new StringContent(model.DOB.ToString("o")), "DOB");
            try
            {
                var response = await httpClient.PostAsync("api/drivers/register", formData);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Driver registered successfully! You can now log in.";
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
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
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
                    if (authResponse == null)
                    {
                        ModelState.AddModelError(string.Empty, "Invalid response from server.");
                    }
                    else
                    {
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
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Login failed: {response.StatusCode} - {errorContent}");
                    _logger.LogError("Driver login failed for {Email}: {Status} - {Error}", model.Email, response.StatusCode, errorContent);
                }
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                // Step 1: Request OTP for password reset
                var response = await httpClient.PostAsJsonAsync("api/drivers/forgot-password", dto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "A password reset OTP has been sent to your email. Please check your inbox to continue.";
                    TempData["UserEmailForReset"] = dto.Email; // Store email to pass to the next view
                    return RedirectToAction("ResetPassword");
                }
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Error: {error}");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}");
            }
            return View(dto);
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                // Step 2: Verify OTP and reset password
                var response = await httpClient.PostAsJsonAsync("api/drivers/reset-password", dto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Password reset successful. Please log in with your new password.";
                    return RedirectToAction("Login");
                }
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Error: {error}");
                TempData.Keep("UserEmailForReset"); // Keep email if API call fails
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}");
                TempData.Keep("UserEmailForReset");
            }
            return View(dto);
        }
    }
}
