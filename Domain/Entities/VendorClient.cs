using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class VendorClient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? VendorId { get; set; }
        public Vendor Vendor { get; set; }

        // One-to-One with details
        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public VendorClientDetail ClientDetail { get; set; }

        public bool? IsActive { get; set; } 

        // Navigation
        //public ICollection<VendorTransaction> VendorTransactions { get; set; }
    }
}
