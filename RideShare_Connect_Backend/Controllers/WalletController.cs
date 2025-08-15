using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public WalletController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("add-money")]
        public async Task<IActionResult> AddMoney(AddMoneyDto dto)
        {
            var result = await _paymentService.AddMoneyAsync(dto);
            return Ok(result);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromQuery] int userId)
        {
            var balance = await _paymentService.GetWalletBalanceAsync(userId);
            return Ok(new { Balance = balance });
        }
    }
}
