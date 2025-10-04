using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    public class UserAccountController : Controller
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ILogger<UserAccountController> _logger;
        public UserAccountController(IUserAccountService userAccountService, ILogger<UserAccountController> logger)
        {
            _userAccountService = userAccountService;
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

            if (!string.IsNullOrWhiteSpace(model.ConfirmPassword) && model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Password and confirmation do not match.");
                return View(model);
            }

            try
            {
                await _userAccountService.RegisterUserAsync(model);
                TempData["SuccessMessage"] = "User registered successfully! You can now log in.";
                _logger.LogInformation("User registration completed for {Email}.", model.Email);
                return RedirectToAction("Login");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Error registering user {Email}", model.Email);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration.");
                _logger.LogError(ex, "Unexpected error during registration for {Email}", model.Email);
                return View(model);
            }
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

            try
            {
                var authResponse = await _userAccountService.GoogleSignInAsync(dto);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                    new Claim(ClaimTypes.Email, authResponse.Email),
                    new Claim("Token", authResponse.Token),
                    new Claim(ClaimTypes.Role, authResponse.UserType ?? "Rider"),
                    new Claim("ProfilePicture", authResponse.ProfilePicture ?? "https://avatar.iran.liara.run/public/boy?username=Ash"),
                    new Claim(ClaimTypes.Name, authResponse.FullName ?? string.Empty)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return Ok(new { redirectUrl = Url.Action("User", "UserDashboard") });
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Google sign-in failed.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google sign-in.");
                return StatusCode(500, "An unexpected error occurred during Google sign-in.");
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

            try
            {
                var authResponse = await _userAccountService.LoginAsync(model);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                    new Claim(ClaimTypes.Email, authResponse.Email),
                    new Claim("Token", authResponse.Token),
                    new Claim(ClaimTypes.Role, authResponse.UserType ?? "Rider")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

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

                _logger.LogInformation("User {Email} logged in successfully. Token received.", model.Email);

                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("User", "UserDashboard");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Login failed for {Email}", model.Email);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while logging in.");
                _logger.LogError(ex, "Unexpected error during login for {Email}.", model.Email);
                return View(model);
            }
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

            if (!int.TryParse(userId, out var userIdValue))
            {
                return Unauthorized();
            }

            try
            {
                await _userAccountService.DeleteAccountAsync(userIdValue, model);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["SuccessMessage"] = "Account deleted successfully.";
                return RedirectToAction("Register", "UserAccount");
            }
            catch (ServiceException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning(ex, "Account deletion failed for user {UserId}.", userIdValue);
                return RedirectToAction("DeleteAccount", "UserDashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Account deletion failed due to an unexpected error.";
                _logger.LogError(ex, "Unexpected error while deleting account for user {UserId}.", userIdValue);
                return RedirectToAction("DeleteAccount", "UserDashboard");
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
                await _userAccountService.ForgotPasswordAsync(dto);
                TempData["SuccessMessage"] = "A password reset OTP has been sent to your email. Please check your inbox to continue.";
                TempData["UserEmailForReset"] = dto.Email;
                return RedirectToAction("ResetPassword");
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while requesting the password reset OTP.");
                _logger.LogError(ex, "Unexpected error during forgot password for {Email}.", dto.Email);
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
                TempData.Keep("UserEmailForReset");
                return View(dto);
            }

            try
            {
                await _userAccountService.ResetPasswordAsync(dto);
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
                _logger.LogError(ex, "Unexpected error during password reset for {Email}.", dto.Email);
                TempData.Keep("UserEmailForReset");
                return View(dto);
            }
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
