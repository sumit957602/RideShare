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
                        new Claim(ClaimTypes.Role, authResponse.UserType ?? "Rider")
                        //other claim needed from AuthResponseDto
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Keep the user logged in across browser sessions
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) // Match JWT expiration or set longer for cookie persistence
                    };

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
