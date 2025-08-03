using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentsController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment(PaymentProcessDto dto)
        {
            var payment = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(payment);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int userId)
        {
            var history = await _paymentService.GetPaymentHistoryAsync(userId);
            return Ok(history);
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetMethods([FromQuery] int userId)
        {
            var methods = await _paymentService.GetPaymentMethodsAsync(userId);
            return Ok(methods);
        }

        [HttpPost("save-method")]
        public async Task<IActionResult> SaveMethod(SavePaymentMethodDto dto)
        {
            var method = await _paymentService.SavePaymentMethodAsync(dto);
            return Ok(method);
        }
    }
}
