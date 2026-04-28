using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{
    public class ReturnRequestModel
    {
        public int TransactionId { get; set; }
        public int? CreatedBy { get; set; }
        public List<ReturnProductModel> ReturnProducts { get; set; } = new();
    }

    public class ReturnProductModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

}
