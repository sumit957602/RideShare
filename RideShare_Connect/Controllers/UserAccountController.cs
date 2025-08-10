using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models;
using RideShareConnect.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
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
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Model validation error: " + error.ErrorMessage);
                }

                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/Users/send-verification-otp", model);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RegistrationOtpResponseDto>();

                    TempData["SuccessMessage"] = result?.Message ?? "An OTP has been sent to your email. Please check your inbox to verify and complete registration.";
                    TempData["RegistrationId"] = result?.Id;
                    return RedirectToAction("VerifyRegistrationOtp");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"API Error: {response.StatusCode} - {errorContent}");
                    _logger.LogError("Error sending registration OTP for {Email}: {StatusCode} - {ErrorContent}", model.Email, response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}. Make sure your API is running and accessible.");
                _logger.LogError(ex, "HTTP request error during registration OTP send for {Email}.", model.Email);
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error parsing API response: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error during registration OTP send for {Email}.", model.Email);
            }
            return View(model);
        }


        public IActionResult VerifyRegistrationOtp()
        {
            if (TempData["RegistrationId"] == null)
            {
                TempData["ErrorMessage"] = "Registration process not initiated or expired. Please register again.";
                return RedirectToAction("Register");
            }

            TempData.Keep("RegistrationId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationOtp(string otp)
        {
            var registrationId = TempData["RegistrationId"]?.ToString();
            if (string.IsNullOrEmpty(registrationId))
            {
                TempData["ErrorMessage"] = "Registration process expired. Please try again.";
                return RedirectToAction("Register");
            }

            var verifyDto = new UserRegisterVerifyDto
            {
                RegistrationId = registrationId,
                Otp = otp
            };

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/Users/register", verifyDto);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User registered successfully! You can now log in.";
                    _logger.LogInformation("User registration completed for RegistrationId {RegistrationId}.", registrationId);
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Verification failed: {response.StatusCode} - {errorContent}");
                    _logger.LogError("Error verifying OTP for RegistrationId {RegistrationId}: {StatusCode} - {ErrorContent}", registrationId, response.StatusCode, errorContent);
                    TempData["RegistrationId"] = registrationId;
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Network error: {ex.Message}. Make sure your API is running and accessible.");
                _logger.LogError(ex, "HTTP request error during OTP verification for RegistrationId {RegistrationId}.", registrationId);
                TempData["RegistrationId"] = registrationId;
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error parsing API response during OTP verification: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error during OTP verification for RegistrationId {RegistrationId}.", registrationId);
                TempData["RegistrationId"] = registrationId;
            }
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var claimsIdentity = new ClaimsIdentity(result.Principal.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // After successful Google authentication, redirect the user to their dashboard
            return RedirectToAction("User", "UserDashboard");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult FacebookLogin()
        {
            var redirectUrl = Url.Action("FacebookResponse");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var claimsIdentity = new ClaimsIdentity(result.Principal.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("User", "UserDashboard");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleFirebaseSignIn([FromBody] GoogleLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/Users/google-signin", dto);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim("Token", authResponse.Token),
                        new Claim(ClaimTypes.Role, authResponse.UserType ?? "Rider")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Ok(new { redirectUrl = Url.Action("User", "UserDashboard") });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google sign-in failed: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error during Google sign-in.");
                return StatusCode(500, "An error occurred while communicating with the API.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error during Google sign-in.");
                return StatusCode(500, "Invalid response from API.");
            }
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
            TempData["ErrorMessage"] = $"Account deletion failed: {response.StatusCode} - {error}";
            return RedirectToAction("DeleteAccount", "UserDashboard");
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
                var response = await httpClient.PostAsJsonAsync("api/Users/forgot-password", dto);
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
                var response = await httpClient.PostAsJsonAsync("api/Users/reset-password", dto);
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
