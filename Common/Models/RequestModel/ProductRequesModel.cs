using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{
    public class ProductRequesModel
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ProductCode { get; set; }
        public int CategoryId { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public long? StockNumber { get; set; }
        public decimal? TotalPurchasePrice { get; set; }
        public bool IsActive { get; set; } =true;
    }
    public class ProductCategoryRequesModel
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string?Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
