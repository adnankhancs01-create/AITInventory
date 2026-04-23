using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class StockFifoDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int PurchaseQty { get; set; }
        public int SoldQty { get; set; }
        public int RemainingQty { get; set; }
        public long StockNumber { get; set; }
        public decimal? TotalPurchasePrice { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? Category { get; set; }
    }
}
