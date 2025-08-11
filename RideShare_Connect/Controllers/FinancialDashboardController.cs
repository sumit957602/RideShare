using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.ViewModels;
using RideShare_Connect.Models.PaymentManagement;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RideShare_Connect.Controllers
{
    [Authorize]
    public class FinancialDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public FinancialDashboardController(AppDbContext db)
        {
            _db = db;
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

            var paymentHistory = await paymentQuery
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            var model = new FinanceViewModel
            {
                WalletBalance = balance,
                TotalSpentThisMonth = totalSpent,
                TotalSavedOnRides = 0,
                PaymentMethods = await _db.PaymentMethods.Where(pm => pm.UserId == userId).ToListAsync(),
                PaymentHistory = paymentHistory,
                RecentTransactions = paymentHistory.Take(5).ToList(),
                Profile = profile
            };

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
    }
}
