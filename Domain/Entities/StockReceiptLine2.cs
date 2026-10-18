using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class StockReceiptLine2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ReceiptId { get; set; }
        [ForeignKey("ReceiptId")]
        public StockReceipt2? Receipt { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        [MaxLength(50)]
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public int? PurchaseOrderLineId { get; set; }
        [ForeignKey("PurchaseOrderLineId")]
        public PurchaseOrderLine2? PurchaseOrderLine { get; set; }

        /// <summary>The positive VendorStock ledger row created for this line.</summary>
        public int? VendorStockId { get; set; }
        [ForeignKey("VendorStockId")]
        public VendorStock? VendorStock { get; set; }
    }
}
