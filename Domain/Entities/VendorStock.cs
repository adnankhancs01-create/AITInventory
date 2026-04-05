using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class VendorStock
    {
        public int Id { get; set; }

        public int? VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public int? Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }
        public decimal? SellPrice { get; set; }
    }
}
