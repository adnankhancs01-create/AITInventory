using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class PurchaseOrderLine2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder2? PurchaseOrder { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int OrderedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }
    }
}
