using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class GetStockResponseModel
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Category { get; set; }
        public long? StockNumber { get; set; }
        public int? Quantity { get; set; }
        public decimal? TotalPurchasePrice { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
