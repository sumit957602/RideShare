using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.ViewModels;
using RideShare_Connect.Models.PaymentManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RideShare_Connect.Controllers
{
    [Authorize]
    public class FinancialDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public FinancialDashboardController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<IActionResult> Finance()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "UserAccount");
            }
            var userId = int.Parse(userIdClaim);

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            var balance = wallet?.Balance ?? 0;

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var paymentQuery = _db.Payments
                .Include(p => p.PaymentMethod)
                .Where(p => p.UserId == userId);

            var totalSpent = await paymentQuery
                .Where(p => p.Status == "Completed" && p.PaymentDate >= startOfMonth)
                .SumAsync(p => p.Amount);

            var walletTransactions = new List<WalletTransaction>();
            if (wallet != null)
            {
                walletTransactions = await _db.WalletTransactions
                    .Where(t => t.WalletId == wallet.Id)
                    .OrderByDescending(t => t.TxnDate)
                    .ToListAsync();
            }

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            var model = new FinanceViewModel
            {
                WalletBalance = balance,
                TotalSpentThisMonth = totalSpent,
                TotalSavedOnRides = 0,
                PaymentMethods = await _db.PaymentMethods.Where(pm => pm.UserId == userId).ToListAsync(),
                TransactionHistory = walletTransactions,
                RecentTransactions = walletTransactions.Take(3).ToList(),
                Profile = profile
            };

            ViewBag.RazorpayKey = _config["PaymentGateway:ApiKey"] ?? "rzp_test_R5NMyugFaCywDS";

            return View(model);
        }

        [HttpGet]
        public IActionResult AddPaymentMethod()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPaymentMethod(PaymentMethod method)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "UserAccount");
            }
            var userId = int.Parse(userIdClaim);

            if (!ModelState.IsValid)
            {
                return View(method);
            }

            method.UserId = userId;
            _db.PaymentMethods.Add(method);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Finance));
        }

        [HttpGet]
        public IActionResult TopUpWallet()
        {
            return View();
        }

        [HttpGet]
        public IActionResult PaymentHistory()
        {
            return View();
        }
    }
}
