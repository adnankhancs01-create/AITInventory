using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class DashboardResponse
    {
        public DashboardSummary Summary { get; set; } = new();
        public List<TransactionSummaryData> RecentTransactions { get; set; } = new();
    }
    public class TransactionSummaryData
    {
        public int TransactionId { get; set; }
        public string TransactionType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
    public class DashboardSummary
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetRevenue { get; set; }

        public int TotalTransactions { get; set; }
    }
    public class ChartItem
    {
        public string Label { get; set; } = "";
        public decimal Value { get; set; }
    }
}
