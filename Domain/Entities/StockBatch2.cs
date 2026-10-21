using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Batch / expiry metadata attached to one positive VendorStock ledger row created by the Stock2 module.</summary>
    public class StockBatch2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int VendorStockId { get; set; }
        [ForeignKey("VendorStockId")]
        public Stock? VendorStock { get; set; }

        [MaxLength(50)]
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        public int? LocationId { get; set; }
        [ForeignKey("LocationId")]
        public StockLocation2? Location { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
