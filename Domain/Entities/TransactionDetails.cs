using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class TransactionDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        public int Quantity { get; set; }
        public int? ReturnedQuantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; } = default(decimal?);
        public int? TransMstId { get; set; }   // refers to VendorClient

        [ForeignKey("TransMstId")]
        public TransactionMst TransMst { get; set; }
        //public long? StockNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discount { get; set; }
        //public ICollection<VendorStock> Stocks{ get; set; }
    }
}
