using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class GetProductResponseModel:ProductModel
    {
        public PricingModel? PricingModel { get; set; }
    }
}
