using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    /// <summary>Non-sale stock issue (damage, expiry, loss, internal use, sample, return to supplier...).</summary>
    public class StockIssue2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string IssueNumber { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; } = DateTime.Now;

        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public StockLocation2? Location { get; set; }

        [Required, MaxLength(50)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Posted";

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        public ICollection<StockIssueLine2> Lines { get; set; } = new List<StockIssueLine2>();
    }
}
