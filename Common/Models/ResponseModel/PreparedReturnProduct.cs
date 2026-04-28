using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class PreparedReturnProduct
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        //public decimal DiscountAmount { get; set; }
        public int StockQty { get; set; }
        public int CreatedBy { get; set; }
    }

}
