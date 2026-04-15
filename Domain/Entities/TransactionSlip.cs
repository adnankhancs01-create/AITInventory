using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class TransactionSlip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public int? TransactionId { get; set; }
        [ForeignKey("TransactionId")]
        public TransactionMst Transaction { get; set; }

        // Slip content (the formatted string you generate)
        [Required]
        public string SlipContent { get; set; }

        // Metadata
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

    }
}
