namespace Common.Stock2
{
    public class PagedResult2<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        /// <summary>Named totals for the whole filtered set (not just the current page), e.g. "StockValue".</summary>
        public Dictionary<string, decimal> Totals { get; set; } = new();

        public decimal Total(string key) => Totals.TryGetValue(key, out var v) ? v : 0m;
    }
}
