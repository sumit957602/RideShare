namespace RideShare_Connect.Services;

using System;
using System.Threading.Tasks;
using RideShare_Connect.DTOs;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> CreateChargeAsync(PaymentProcessDto dto);
    Task<string> GetPaymentStatusAsync(string transactionId);
    Task<CardTokenResult> TokenizeCardAsync(string cardNumber, DateTime expiryDate);
    Task<PaymentGatewayResult> CreatePaymentLinkAsync(PaymentLinkRequestDto dto);
}

public class PaymentGatewayResult
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ShortUrl { get; set; }
}

public class CardTokenResult
{
    public string CardTokenNo { get; set; } = string.Empty;
    public string CardLast4Digit { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
}
