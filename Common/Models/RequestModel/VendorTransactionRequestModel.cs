using System;
using System.Collections.Generic;
using System.Text;
using Common.Models;

namespace Common.Models.RequestModel
{
    public class VendorTransactionModel
    {
        public int Id { get; set; }

        public int? ClientId { get; set; }   // refers to VendorClient
        public ClientModel Client { get; set; }

        public int? ProductId { get; set; }
        public ProductModel Product { get; set; }

        public string? TransactionType { get; set; }  // BUY / SELL

        public int? Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? TotalAmount => UnitPrice * Quantity;

        public decimal? ClientAmount { get; set; }

        public DateTime? TransactionDate { get; set; } = DateTime.Now;

        public string? Remarks { get; set; }
    }
    public class AddEditTransactionRequestModel: VendorTransactionModel
    {
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAddress { get; set; }
    }
}
