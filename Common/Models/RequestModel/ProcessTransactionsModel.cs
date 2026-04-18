using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{

    public class ProcessTransactionsModel
    {
        public List<ProductWidgetModel> Products { get; set; } = new();
        public decimal? TotalAmount { get => Math.Round(Products.Sum(x => x.Quantity * x.UnitPrice)); }
        public decimal? TotalDiscount { get => Math.Round(Products.Sum(x => x.Discount ?? 0)); }
        public string? Remarks { get; set; }
        public int? CreatedBy { get; set; }
        public string? ClientAddress { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string? TransactionType { get; set; } = "Sell";
        public decimal? NetAmount { get => Math.Round((TotalAmount - (TotalDiscount ?? 0))??0); }
        public DateTime? TransactionDate { get; set; }
        public int? TransactionId { get; set; }
    }
}
