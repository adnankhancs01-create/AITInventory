using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class VendorStock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; }

        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public int? Quantity { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPurchasePrice { get; set; }
        public long? StockNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public int? TransactionId { get; set; }
        [ForeignKey("TransactionId")]
        public TransactionDetails TransactionDetails { get; set; }
        public bool IsActive { get; set; }
    }
}
