using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class StockIssueLine2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int IssueId { get; set; }
        [ForeignKey("IssueId")]
        public StockIssue2? Issue { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        /// <summary>FIFO cost per unit at the time of issue (for write-off valuation).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        /// <summary>The negative VendorStock ledger row created for this line.</summary>
        public int? VendorStockId { get; set; }
        [ForeignKey("VendorStockId")]
        public Stock? VendorStock { get; set; }
    }
}
