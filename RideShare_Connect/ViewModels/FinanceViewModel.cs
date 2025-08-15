using System.Collections.Generic;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.UserManagement;

namespace RideShare_Connect.ViewModels
{
    public class FinanceViewModel
    {
        public decimal WalletBalance { get; set; }
        public decimal TotalSpentThisMonth { get; set; }
        public decimal TotalSavedOnRides { get; set; }
        // Holds the most recent wallet transactions
        public List<WalletTransaction> RecentTransactions { get; set; } = new();
        // Complete history of wallet transactions
        public List<WalletTransaction> TransactionHistory { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public UserProfile Profile { get; set; }
    }
}
