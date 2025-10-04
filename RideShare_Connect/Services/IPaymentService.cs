using System.Collections.Generic;
using System.Threading.Tasks;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;

namespace RideShare_Connect.Services
{
    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(PaymentProcessDto dto);
        Task UpdatePaymentStatusAsync(string providerTransactionId);
        Task<Payment?> ConfirmPaymentAsync(PaymentConfirmDto dto);
        Task<AddMoneyResultDto> AddMoneyAsync(AddMoneyDto dto);
        Task<decimal> GetWalletBalanceAsync(int userId);
        Task<List<Payment>> GetPaymentHistoryAsync(int userId);
        Task<Refund> RequestRefundAsync(RefundRequestDto dto);
        Task<List<PaymentMethod>> GetPaymentMethodsAsync(int userId);
        Task<PaymentMethod> SavePaymentMethodAsync(SavePaymentMethodDto dto);
        Task<PaymentMethod?> UpdatePaymentMethodAsync(PaymentMethodUpdateDto dto);
        Task<bool> DeletePaymentMethodAsync(int id);
        Task<PaymentGatewayResult> CreatePaymentLinkAsync(PaymentLinkRequestDto dto);
    }
}
