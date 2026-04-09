using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class VendorTransaction
    {
        public int Id { get; set; }

        public int? ClientId { get; set; }   // refers to VendorClient
        public VendorClient Client { get; set; }

        public int? ProductId { get; set; }
        public Product Product { get; set; }

        public string? TransactionType { get; set; }  // BUY / SELL

        public int? Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? TotalAmount => UnitPrice * Quantity;

        public decimal? ClientAmount { get; set; }

        public DateTime? TransactionDate { get; set; } = DateTime.Now;

        public string? Remarks { get; set; }
    }
}
