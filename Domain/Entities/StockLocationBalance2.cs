using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>
    /// Quantity of a product held in a NON-default location.
    /// The default location quantity is never stored: it is derived as
    /// (ledger on-hand) - (sum of these balances) - (quantity in transit).
    /// </summary>
    public class StockLocationBalance2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public StockLocation2? Location { get; set; }

        public int Quantity { get; set; }
        public DateTime ModifiedOn { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
