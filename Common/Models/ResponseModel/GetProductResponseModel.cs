using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class GetProductResponseModel:ProductModel
    {
        public PricingModel? PricingModel { get; set; }
        public int? TotalStock { get; set; }
        public decimal? TotalPurchasePrice { get; set; }
    }
}
