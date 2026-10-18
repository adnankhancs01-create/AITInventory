using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Transfer between two locations. Status: InTransit, Received, Cancelled.</summary>
    public class StockTransfer2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string TransferNumber { get; set; } = string.Empty;

        public DateTime TransferDate { get; set; } = DateTime.Now;

        public int FromLocationId { get; set; }
        [ForeignKey("FromLocationId")]
        public StockLocation2? FromLocation { get; set; }

        public int ToLocationId { get; set; }
        [ForeignKey("ToLocationId")]
        public StockLocation2? ToLocation { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "InTransit";

        public DateTime? ReceivedOn { get; set; }
        public int? ReceivedBy { get; set; }
        public DateTime? CancelledOn { get; set; }
        public int? CancelledBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public ICollection<StockTransferLine2> Lines { get; set; } = new List<StockTransferLine2>();
    }
}
