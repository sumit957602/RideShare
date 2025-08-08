using System.Collections.Generic;
using RideShare_Connect.Models.PaymentManagement;

namespace RideShare_Connect.ViewModels
{
    public class FinanceViewModel
    {
        public decimal WalletBalance { get; set; }
        public decimal TotalSpentThisMonth { get; set; }
        public decimal TotalSavedOnRides { get; set; }
        public List<Payment> RecentTransactions { get; set; } = new();
        public List<Payment> PaymentHistory { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
    }
}
