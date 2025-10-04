namespace RideShare_Connect.Services;

public class PaymentGatewayOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
}
