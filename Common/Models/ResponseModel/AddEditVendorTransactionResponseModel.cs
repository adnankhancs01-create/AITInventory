using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.ResponseModel
{
    public class AddEditTransactionResponseModel
    {
        public string? Slip { get; set; }
        public long? TransactionId { get; set; }
    }
}
