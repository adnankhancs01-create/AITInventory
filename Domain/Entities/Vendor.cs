using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Vendor
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public bool? IsActive { get; set; } 

        // Navigation properties
        public ICollection<VendorStock> VendorStocks { get; set; }
        public ICollection<VendorClient> VendorClients { get; set; }
        public ICollection<VendorTransaction> VendorTransactions { get; set; }
    }
}
