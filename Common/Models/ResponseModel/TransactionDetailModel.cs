using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class TransactionDetailModel
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Total { get; set; }
        public decimal? Discount { get; set; }
        public string? CategoryName { get; set; }
        public string? ProductCode { get; set; }
        public long? TransactionNumber { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? TransactionDate { get; set; }
    }

    public class TransactionSummaryModel
    {
        public int TotalTransactions { get; set; }
        public int TotalItemSold { get; set; }
        public decimal? TotalAmountGenerated { get; set; }
        public decimal? TotalDiscount { get; set; }
        public decimal? TotalReceivedAmount { get; set; }
    }

    public class PagedTransactionResponse
    {
        public List<TransactionDetailModel> Data { get; set; }
        public TransactionSummaryModel Summary { get; set; }
        public int TotalCount { get; set; }
    }
}
