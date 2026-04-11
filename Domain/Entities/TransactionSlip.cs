using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class TransactionSlip
    {
        [Key]
        public int Id { get; set; }

        public int? TransactionId { get; set; }
        public VendorTransaction Transaction { get; set; }

        // Slip content (the formatted string you generate)
        [Required]
        public string SlipContent { get; set; }

        // Metadata
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

    }
}
