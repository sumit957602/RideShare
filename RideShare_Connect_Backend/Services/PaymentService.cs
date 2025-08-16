using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;

namespace RideShare_Connect.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentGateway _gateway;

        public PaymentService(ApplicationDbContext context, IPaymentGateway gateway)
        {
            _context = context;
            _gateway = gateway;
        }

        public async Task<Payment> ProcessPaymentAsync(PaymentProcessDto dto)
        {
            var result = await _gateway.CreateChargeAsync(dto);

            var payment = new Payment
            {
                UserId = dto.UserId,
                BookingId = dto.BookingId,
                Amount = dto.Amount,
                PaymentMode = dto.PaymentMode,
                PaymentMethodId = dto.PaymentMethodId,
                PaymentDate = DateTime.UtcNow,
                Status = result.Status,
                ProviderTransactionId = result.TransactionId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task UpdatePaymentStatusAsync(string providerTransactionId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ProviderTransactionId == providerTransactionId);

            if (payment == null || string.IsNullOrEmpty(payment.ProviderTransactionId))
            {
                return;
            }

            var status = await _gateway.GetPaymentStatusAsync(payment.ProviderTransactionId);
            payment.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> ConfirmPaymentAsync(PaymentConfirmDto dto)
        {
            var payment = await _context.Payments.FindAsync(dto.PaymentId);
            if (payment == null)
            {
                return null;
            }

            if (dto.PaymentMethodId.HasValue)
            {
                payment.PaymentMethodId = dto.PaymentMethodId;
            }

            payment.PaymentDate = DateTime.UtcNow;
            payment.Status = "Completed";

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<AddMoneyResultDto> AddMoneyAsync(AddMoneyDto dto)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == dto.UserId);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = dto.UserId,
                    Balance = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
            }

            var transaction = new WalletTransaction
            {
                Wallet = wallet,
                Amount = dto.Amount,
                TxnType = "Credit",
                TxnDate = DateTime.UtcNow,
                Description = "Wallet top-up",
                TransactionId = Guid.NewGuid().ToString(),
                PaymentMethod = dto.PaymentMethod,
                Status = dto.TransactionStatus
            };

            _context.WalletTransactions.Add(transaction);

            if (dto.TransactionStatus == "Successful")
            {
                wallet.Balance += dto.Amount;
                wallet.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new AddMoneyResultDto
            {
                UserId = dto.UserId,
                Balance = wallet.Balance,
                TransactionId = transaction.TransactionId,
                PaymentMethod = transaction.PaymentMethod,
                TransactionStatus = transaction.Status
            };
        }

        public async Task<decimal> GetWalletBalanceAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            return wallet?.Balance ?? 0m;
        }

        public async Task<List<Payment>> GetPaymentHistoryAsync(int userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Refund> RequestRefundAsync(RefundRequestDto dto)
        {
            var refund = new Refund
            {
                PaymentId = dto.PaymentId,
                Amount = dto.Amount,
                Reason = dto.Reason,
                RefundStatus = "Pending",
                ProcessedAt = DateTime.UtcNow
            };
            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();
            return refund;
        }

        public async Task<List<PaymentMethod>> GetPaymentMethodsAsync(int userId)
        {
            return await _context.PaymentMethods
                .Where(pm => pm.UserId == userId)
                .ToListAsync();
        }

        public async Task<PaymentMethod> SavePaymentMethodAsync(SavePaymentMethodDto dto)
        {
            var method = new PaymentMethod
            {
                UserId = dto.UserId,
                CardType = dto.CardType,
                CardholderName = dto.CardholderName,
                CardNumber = dto.CardNumber,
                ExpiryDate = dto.ExpiryDate,
                CVV = dto.CVV,
                CardTokenNo = Guid.NewGuid().ToString(),
                CardLast4Digit = dto.CardLast4Digit,
                CardBrand = dto.CardBrand
            };

            _context.PaymentMethods.Add(method);
            await _context.SaveChangesAsync();
            return method;
        }

        public async Task<PaymentMethod?> UpdatePaymentMethodAsync(PaymentMethodUpdateDto dto)
        {
            var method = await _context.PaymentMethods.FindAsync(dto.Id);
            if (method == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(dto.CardType))
            {
                method.CardType = dto.CardType;
            }

            if (!string.IsNullOrEmpty(dto.CardNumber) && dto.ExpiryDate.HasValue)
            {
                var token = await _gateway.TokenizeCardAsync(dto.CardNumber, dto.ExpiryDate.Value);
                method.CardTokenNo = token.CardTokenNo;
                method.CardLast4Digit = token.CardLast4Digit;
                method.CardBrand = token.CardBrand;
            }

            await _context.SaveChangesAsync();
            return method;
        }

        public async Task<bool> DeletePaymentMethodAsync(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null)
            {
                return false;
            }

            _context.PaymentMethods.Remove(method);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<PaymentGatewayResult> CreatePaymentLinkAsync(PaymentLinkRequestDto dto)
        {
            return _gateway.CreatePaymentLinkAsync(dto);
        }
    }
}
