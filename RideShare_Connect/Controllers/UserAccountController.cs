using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models;
using RideShareConnect.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RideShare_Connect.Controllers
{
    public class UserAccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserAccountController> _logger;
        public UserAccountController(IHttpClientFactory httpClientFactory, ILogger<UserAccountController> logger)
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
        public async Task<IActionResult> Register(UserRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                    );
                var response = await httpClient.PostAsync("api/Users/register", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    // Optionally read the response content if your API returns the created user
                    var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
                    ViewBag.Message = "User registered successfully! ID: " + createdUser?.Id + ", Email: " + createdUser?.Email;
                    _logger.LogInformation("User {Email} registered successfully.", model.Email);
                    return View("Login"); // Or redirect to a login page
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"API Error: {response.StatusCode} - {errorContent}");
                    _logger.LogError("Error registering user {Email}: {StatusCode} - {ErrorContent}", model.Email, response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}. Make sure your API is running and accessible.");
                _logger.LogError(ex, "HTTP request error during user registration for {Email}.", model.Email);
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error parsing API response: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error during user registration for {Email}.", model.Email);
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
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(model),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("api/users/login", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                    // --- Store the JWT token and user claims in a cookie for MVC Authentication ---
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim("Token", authResponse.Token), // Store the JWT token itself
                        new Claim(ClaimTypes.Role, authResponse.UserType ?? "Rider"),
                        new Claim("VerificationBadge", authResponse.VerificationBadge ?? string.Empty)
                        //other claim needed from AuthResponseDto
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    if (model.RememberMe)
                    {
                        authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
                    }

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    // --- End JWT token storage and MVC Authentication ---

                    _logger.LogInformation("User {Email} logged in successfully. Token received.", model.Email);

                    // Redirect to returnUrl or a default authenticated page
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("User", "UserDashboard"); // Redirect to home or dashboard
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Login failed: {response.StatusCode} - {errorContent}");
                    _logger.LogError("Login failed for {Email}: {StatusCode} - {ErrorContent}", model.Email, response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}. Make sure your API is running and accessible.");
                _logger.LogError(ex, "HTTP request error during login for {Email}.", model.Email);
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error parsing API response: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error during login for {Email}.", model.Email);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/Users/{userId}/account")
            {
                Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json")
            };

            var token = User.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["SuccessMessage"] = "Account deleted successfully.";
                return RedirectToAction("Register", "UserAccount");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Account deletion failed: {response.StatusCode} - {error}");
            return RedirectToAction("PrivacySettings", "UserDashboard");
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
                var response = await httpClient.PostAsJsonAsync("api/Users/forgot-password", dto);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<PasswordResetTokenDto>();
                    return RedirectToAction("ResetPassword", new { token = tokenResponse?.Token });
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

        public IActionResult ResetPassword(string token)
        {
            return View(new ResetPasswordDto { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/Users/reset-password", dto);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Password reset successful. Please log in.";
                    return RedirectToAction("Login");
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



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login", "UserAccount");
        }












        //[HttpPost]
        //public IActionResult Login(string role, string status, string email, string password)
        //{
        //    // login/signup logic
        //    if (status == "new")
        //    {
        //        // TODO: Save user to DB in future
        //        return RedirectToAction("UserDashboard", "Dashboard"); 
        //    }
        //    else
        //    {
        //        // TODO: Validate credentials
        //        if (role == "user")
        //            return RedirectToAction("UserDashboard", "Dashboard");
        //        else if (role == "worker")
        //            return RedirectToAction("WorkerDashboard", "Dashboard");
        //        else if (role == "admin")
        //            return RedirectToAction("AdminDashboard", "Dashboard");
        //    }

        //    ViewBag.Error = "Invalid credentials or unknown role.";
        //    return View();
        //}

        // ✅ New Logout Action
        //[HttpPost]
        //public IActionResult Logout()
        //{
        //    // TODO: Clear session or auth logic in future
        //    return RedirectToAction("Login", "Home");
        //}

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}

    }
}
