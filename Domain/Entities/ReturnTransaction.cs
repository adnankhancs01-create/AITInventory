using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class ReturnTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }

        public int? TransactionId { get; set; }
        public int? TransactionDetailId { get; set; }
        public int? ProductId { get; set; }

        public int? Quantity { get; set; }
        public decimal? Amount { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        [ForeignKey("TransactionDetailId")]
        public TransactionDetails TransactionDetail { get; set; }
    }
}
