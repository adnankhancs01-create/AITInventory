using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Goods receipt header. Status: Posted or Cancelled.</summary>
    public class StockReceipt2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string ReceiptNumber { get; set; } = string.Empty;

        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        public int? VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor? Vendor { get; set; }

        [MaxLength(150)]
        public string? SupplierName { get; set; }
        [MaxLength(50)]
        public string? SupplierInvoiceNo { get; set; }

        public int? PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder2? PurchaseOrder { get; set; }

        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public StockLocation2? Location { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Posted";

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? CancelledOn { get; set; }
        public int? CancelledBy { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public ICollection<StockReceiptLine2> Lines { get; set; } = new List<StockReceiptLine2>();
    }
}
