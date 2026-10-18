namespace Common.Stock2
{
    public class StockDashboardDto2
    {
        public int ProductCount { get; set; }
        public int InStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int NegativeStockCount { get; set; }
        public long TotalUnitsOnHand { get; set; }
        public decimal TotalStockValue { get; set; }
        public int ExpiringSoonBatches { get; set; }
        public int ExpiredBatches { get; set; }
        public int OpenPurchaseOrders { get; set; }
        public int InTransitTransfers { get; set; }
        public int MovementsToday { get; set; }

        public List<StockOverviewRowDto2> LowStock { get; set; } = new();
        public List<StockMovementRowDto2> RecentMovements { get; set; } = new();
        public List<StockExpiryRowDto2> ExpiringBatches { get; set; } = new();
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
    }
}
