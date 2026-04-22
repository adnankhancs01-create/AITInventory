using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Domain.Entities
{
    public class TransactionMst
    { 
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public string? ClientName { get; set; }
        public string? ClientAddress { get; set; }
        public string? TransactionType { get; set; } 
        public long? TransactionNumber { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Remarks { get; set; }
        public int? ClientId { get; set; }   // refers to VendorClient
        [ForeignKey("ClientId")]
        public VendorClientDetail Client { get; set; }
        public ICollection<TransactionDetails> TransactionDetails { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }

    }
}
