using Microsoft.AspNetCore.Mvc;
using RideShareConnect.Data;

namespace RideShare_Connect.Controllers
{
    public class FinancialDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public FinancialDashboardController(AppDbContext db)
        {
            _db = db;
        }
        public IActionResult Finance()
        {
            return View();
        }
    }
}
