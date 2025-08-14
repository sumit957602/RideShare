using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/refunds")]
    public class RefundsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public RefundsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestRefund(RefundRequestDto dto)
        {
            var refund = await _paymentService.RequestRefundAsync(dto);
            return Ok(refund);
        }
    }
}
