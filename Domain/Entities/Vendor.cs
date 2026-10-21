using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class Vendor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        public string? Code { get; set; }//Auto Generated Vendor Code

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        public DateTime? CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; } 
        public int? ModifiedBy { get; set; }
        public bool? IsActive { get; set; } 

        // Navigation properties
        public ICollection<Stock> VendorStocks { get; set; }
        //public ICollection<VendorTransaction> VendorTransactions { get; set; }
    }
}
