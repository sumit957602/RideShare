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
    public class AdminAccountController : Controller
    {

        public IActionResult AdminLogin()
        {
            return View();
        }
    }
}
