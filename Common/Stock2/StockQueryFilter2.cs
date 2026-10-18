namespace Common.Stock2
{
    /// <summary>One filter object used by every Stock2 list and report.</summary>
    public class StockQueryFilter2
    {
        public string? Search { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }
        public string? Status { get; set; }
        public string? MovementType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Expiry report: include batches expiring within this many days (default 30).</summary>
        public int DaysAhead { get; set; } = StockConstants2.ExpiryWarningDays;

        public bool OnlyLowStock { get; set; }

        /// <summary>Overview / valuation: include products with zero stock. Reconciliation: include products with no issue.</summary>
        public bool IncludeZero { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }

        /// <summary>Applies Radzen LoadDataArgs.Skip / Top.</summary>
        public void SetPaging(int? skip, int? top)
        {
            PageSize = top.GetValueOrDefault(20) <= 0 ? 20 : top.GetValueOrDefault(20);
            Page = (skip.GetValueOrDefault(0) / PageSize) + 1;
        }

        /// <summary>Applies Radzen LoadDataArgs.OrderBy, which looks like "ProductName desc".</summary>
        public void SetOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                SortBy = null;
                SortDescending = false;
                return;
            }

            var parts = orderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            SortBy = parts[0];
            SortDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A copy that returns every row (used by CSV export).</summary>
        public StockQueryFilter2 ForExport()
        {
            var copy = (StockQueryFilter2)MemberwiseClone();
            copy.Page = 1;
            copy.PageSize = int.MaxValue;
            return copy;
        }
    }
}
