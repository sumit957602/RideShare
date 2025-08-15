using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment(PaymentProcessDto dto)
        {
            var payment = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(payment);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment(PaymentConfirmDto dto)
        {
            var payment = await _paymentService.ConfirmPaymentAsync(dto);
            if (payment == null)
            {
                return NotFound();
            }
            return Ok(payment);
        }

        [HttpPost("payment-link")]
        public async Task<IActionResult> CreatePaymentLink(PaymentLinkRequestDto dto)
        {
            var link = await _paymentService.CreatePaymentLinkAsync(dto);
            return Ok(link);
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

        [HttpPut("methods")]
        public async Task<IActionResult> UpdateMethod(PaymentMethodUpdateDto dto)
        {
            var method = await _paymentService.UpdatePaymentMethodAsync(dto);
            if (method == null)
            {
                return NotFound();
            }
            return Ok(method);
        }

        [HttpDelete("methods")]
        public async Task<IActionResult> DeleteMethod([FromQuery] int methodId)
        {
            var result = await _paymentService.DeletePaymentMethodAsync(methodId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
