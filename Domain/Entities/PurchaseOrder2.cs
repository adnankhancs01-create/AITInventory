using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Purchase order header. Status: Open, PartiallyReceived, Received, Cancelled.</summary>
    public class PurchaseOrder2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string PoNumber { get; set; } = string.Empty;

        public DateTime PoDate { get; set; } = DateTime.Now;
        public DateTime? ExpectedDate { get; set; }

        public int? VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor? Vendor { get; set; }

        [MaxLength(150)]
        public string? SupplierName { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Open";

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public ICollection<PurchaseOrderLine2> Lines { get; set; } = new List<PurchaseOrderLine2>();
    }
}
