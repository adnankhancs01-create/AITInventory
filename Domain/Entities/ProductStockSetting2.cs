using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Per-product reorder policy. One row per product (unique index on ProductId).</summary>
    public class ProductStockSetting2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public int? MaxLevel { get; set; }

        public int? PreferredVendorId { get; set; }
        [ForeignKey("PreferredVendorId")]
        public Vendor? PreferredVendor { get; set; }

        public DateTime ModifiedOn { get; set; } = DateTime.Now;
        public int? ModifiedBy { get; set; }
    }
}
