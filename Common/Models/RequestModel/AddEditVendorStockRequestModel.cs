using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{
    public class AddEditVendorStockRequestModel:VendorStockModel
    {
        public int? UserId { get; set; }
    }
}
