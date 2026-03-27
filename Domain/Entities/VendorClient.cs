using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class VendorClient
    {
        public int Id { get; set; }

        public int VendorId { get; set; }
        public Vendor Vendor { get; set; }

        // One-to-One with details
        public int ClientId { get; set; }
        public VendorClientDetail ClientDetail { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<VendorTransaction> VendorTransactions { get; set; }
    }
}
