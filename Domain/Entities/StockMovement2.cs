using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Audit row written for every movement posted by the Stock2 module.
    /// VendorStockId is set when the movement also created a VendorStock ledger row;
    /// it is null for location-only movements (transfer dispatch / receive).
    /// </summary>
    public class StockMovement2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int? LocationId { get; set; }
        [ForeignKey("LocationId")]
        public StockLocation2? Location { get; set; }

        [Required, MaxLength(30)]
        public string MovementType { get; set; } = string.Empty;

        /// <summary>Signed quantity: positive adds stock to the location, negative removes it.</summary>
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitCost { get; set; }

        [MaxLength(30)]
        public string? DocumentType { get; set; }
        public int? DocumentId { get; set; }
        [MaxLength(40)]
        public string? DocumentNumber { get; set; }

        public int? VendorStockId { get; set; }
        [ForeignKey("VendorStockId")]
        public Stock? VendorStock { get; set; }

        [MaxLength(100)]
        public string? Reason { get; set; }
        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
    }
}
