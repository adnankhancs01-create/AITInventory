using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class Pricing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }// Sell Price
        public string? ProductCode { get; set; }
        public long? StockNumber { get; set; }
        public DateTime? CreatedOn { get; set; }=DateTime.Now;
        public int? CreatedBy { get; set; }
    }
}
