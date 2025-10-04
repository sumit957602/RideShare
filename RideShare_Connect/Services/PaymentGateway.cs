using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using RideShare_Connect.DTOs;

namespace RideShare_Connect.Services;

public class PaymentGateway : IPaymentGateway
{
    private readonly PaymentGatewayOptions _options;
    private readonly RazorpayClient _client;

    public PaymentGateway(IOptions<PaymentGatewayOptions> options)
    {
        _options = options.Value;
        _client = new RazorpayClient(_options.ApiKey, _options.SecretKey);
    }

    public Task<PaymentGatewayResult> CreateChargeAsync(PaymentProcessDto dto)
    {
        var options = new Dictionary<string, object>
        {
            { "amount", (int)(dto.Amount * 100) }, // Razorpay expects amount in paise
            { "currency", _options.Currency },
            { "receipt", $"rcpt_{dto.BookingId}" }
        };

        Order order = _client.Order.Create(options);
        var result = new PaymentGatewayResult
        {
            TransactionId = order["id"].ToString(),
            Status = order["status"].ToString() ?? "created"
        };
        return Task.FromResult(result);
    }

    public Task<PaymentGatewayResult> CreatePaymentLinkAsync(PaymentLinkRequestDto dto)
    {
        var options = new Dictionary<string, object>
        {
            { "amount", (int)(dto.Amount * 100) },
            { "currency", _options.Currency },
            { "description", dto.Description ?? "Wallet top-up" }
        };

        PaymentLink link = _client.PaymentLink.Create(options);
        var result = new PaymentGatewayResult
        {
            TransactionId = link["id"].ToString(),
            Status = link["status"].ToString() ?? "created",
            ShortUrl = link["short_url"].ToString()
        };
        return Task.FromResult(result);
    }

    public Task<string> GetPaymentStatusAsync(string transactionId)
    {
        Order order = _client.Order.Fetch(transactionId);
        return Task.FromResult(order["status"].ToString());
    }

    public Task<CardTokenResult> TokenizeCardAsync(string cardNumber, DateTime expiryDate)
    {
        // Razorpay card tokenization is not available in this demo environment.
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var brand = cardNumber.StartsWith("4") ? "Visa" :
                    cardNumber.StartsWith("5") ? "Mastercard" : "Unknown";
        var result = new CardTokenResult
        {
            CardTokenNo = token,
            CardLast4Digit = cardNumber[^4..],
            CardBrand = brand
        };
        return Task.FromResult(result);
    }
}

