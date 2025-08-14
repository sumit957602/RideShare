namespace RideShare_Connect.Services;

using System.Threading.Tasks;
using RideShare_Connect.DTOs;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> CreateChargeAsync(PaymentProcessDto dto);
    Task<string> GetPaymentStatusAsync(string transactionId);
}

public class PaymentGatewayResult
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
