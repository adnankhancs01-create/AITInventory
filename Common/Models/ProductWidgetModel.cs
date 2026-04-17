using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models
{

    public class ProductWidgetModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; } = 0;
        public int ProductId { get; set; } = 0;
        public decimal? DiscountPercentValue { get; set; } = 0;
        public decimal? Discount { get; set; }
    }
}
