using Common.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class GetTransactionResponseModel: VendorTransactionModel
    {
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAddress { get; set; }
        public string? ProductName { get; set; }
    }
}
