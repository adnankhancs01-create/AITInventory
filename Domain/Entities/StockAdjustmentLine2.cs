using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class StockAdjustmentLine2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AdjustmentId { get; set; }
        [ForeignKey("AdjustmentId")]
        public StockAdjustment2? Adjustment { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        /// <summary>Quantity the system expected at the location when the adjustment was posted.</summary>
        public int SystemQuantity { get; set; }
        public int CountedQuantity { get; set; }

        /// <summary>CountedQuantity - SystemQuantity (negative = shortage, positive = surplus).</summary>
        public int Difference { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        [MaxLength(250)]
        public string? Remarks { get; set; }

        /// <summary>The VendorStock ledger row created for a non-zero difference.</summary>
        public int? VendorStockId { get; set; }
        [ForeignKey("VendorStockId")]
        public VendorStock? VendorStock { get; set; }
    }
}
