using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RideShare_Connect.DTOs;

namespace RideShare_Connect.Services;

public class PaymentGateway : IPaymentGateway
{
    private readonly PaymentGatewayOptions _options;

    public PaymentGateway(IOptions<PaymentGatewayOptions> options)
    {
        _options = options.Value;
    }

    public Task<PaymentGatewayResult> CreateChargeAsync(PaymentProcessDto dto)
    {
        // Here you would call the real provider SDK using _options credentials.
        var result = new PaymentGatewayResult
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = "Pending"
        };
        return Task.FromResult(result);
    }

    public Task<string> GetPaymentStatusAsync(string transactionId)
    {
        // In a real implementation this would query the provider for the current status.
        return Task.FromResult("Completed");
    }
}
