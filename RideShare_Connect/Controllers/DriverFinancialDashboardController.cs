using Microsoft.AspNetCore.Mvc;

namespace RideShare_Connect.Controllers
{
    public class DriverFinancialDashboardController : Controller
    {
        public IActionResult DriverFinance()
        {
            return View();
        }
    }
}
