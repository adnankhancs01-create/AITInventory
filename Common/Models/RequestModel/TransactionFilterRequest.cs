using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models.RequestModel
{
    public class TransactionFilterRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? ClientId { get; set; }
        public string? TransactionType { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
