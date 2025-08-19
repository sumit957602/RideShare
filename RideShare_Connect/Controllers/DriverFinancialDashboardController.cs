using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RideShare_Connect.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverFinancialDashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DriverFinancialDashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> DriverFinance()
        {
            var driverIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(driverIdClaim))
            {
                return RedirectToAction("Login", "DriverAccount");
            }
            var driverId = int.Parse(driverIdClaim);

            var driver = await _db.Set<Driver>()
                .Include(d => d.DriverProfile)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            var wallet = await _db.DriverWallets.FirstOrDefaultAsync(w => w.DriverId == driverId);
            var balance = wallet?.Balance ?? 0m;

            var walletTransactions = new List<WalletTransaction>();
            if (wallet != null)
            {
                var driverTransactions = await _db.DriverWalletTransactions
                    .Where(t => t.DriverWalletId == wallet.Id)
                    .OrderByDescending(t => t.TxnDate)
                    .ToListAsync();

                walletTransactions = driverTransactions.Select(t => new WalletTransaction
                {
                    Id = t.Id,
                    WalletId = t.DriverWalletId,
                    Amount = t.Amount,
                    TxnType = t.TxnType,
                    TxnDate = t.TxnDate,
                    Description = t.Description,
                    TransactionId = t.TransactionId,
                    PaymentMethod = t.PaymentMethod,
                    Status = t.Status
                }).ToList();
            }

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var totalSpent = walletTransactions
                .Where(t => t.Status.ToLower() == "successful" &&
                            t.TxnType.ToLower() == "debit" &&
                            t.TxnDate >= startOfMonth)
                .Sum(t => t.Amount);

            UserProfile profile = null;
            if (driver != null)
            {
                profile = new UserProfile
                {
                    UserId = driver.DriverId,
                    FullName = driver.FullName,
                    ProfilePicture = null
                };
            }

            var model = new FinanceViewModel
            {
                WalletBalance = balance,
                TotalSpentThisMonth = totalSpent,
                TotalSavedOnRides = 0,
                TransactionHistory = walletTransactions,
                RecentTransactions = walletTransactions.Take(3).ToList(),
                Profile = profile
            };

            return View(model);
        }
    }
}
