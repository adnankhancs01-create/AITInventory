using Domain.Entities;

namespace Domain.Stock2
{
    /// <summary>
    /// Pure stock rules (no database access): document numbers, quantity at a location and the shape of new
    /// ledger rows and movements. The Stock table stays the single source of truth for on-hand quantity,
    /// so the existing Retail, Product and Stock pages keep working unchanged.
    /// </summary>
    public static class StockLedgerRules2
    {
        private static long _lastStockNumber;
        private static readonly object StockNumberLock = new();

        /// <summary>Same format as the existing StockRepo (yyyyMMddHHmmssfff) but guaranteed unique inside this process.</summary>
        public static long NewStockNumber()
        {
            lock (StockNumberLock)
            {
                long n = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                if (n <= _lastStockNumber) n = _lastStockNumber + 1;
                _lastStockNumber = n;
                return n;
            }
        }

        /// <summary>Document number such as GRN-20261015-0003.</summary>
        public static string BuildDocumentNumber(string prefix, DateTime date, int existingToday)
        {
            return $"{prefix}-{date:yyyyMMdd}-{existingToday + 1:D4}";
        }

        /// <summary>The prefix shared by all documents of one day, e.g. "GRN-20261015-". Use it to count today's documents.</summary>
        public static string DocumentPrefix(string prefix, DateTime date)
        {
            return $"{prefix}-{date:yyyyMMdd}-";
        }

        public static int Get(Dictionary<int, int> map, int productId)
        {
            return map.TryGetValue(productId, out var v) ? v : 0;
        }

        /// <summary>
        /// Default location: ledger on-hand - other location balances - in transit (derived, never stored).
        /// Any other location: its stored balance.
        /// </summary>
        public static int QuantityAtLocation(int onHand, int nonDefaultBalances, int inTransit, bool isDefaultLocation, int balanceAtThisLocation)
        {
            return isDefaultLocation ? onHand - nonDefaultBalances - inTransit : balanceAtThisLocation;
        }

        /// <summary>Creates a positive ledger row (stock in). TotalPurchasePrice may be null when the cost is unknown.</summary>
        public static Stock NewStockInRow(int productId, int quantity, decimal? totalPrice, int? vendorId, int userId)
        {
            return new Stock
            {
                ProductId = productId,
                VendorId = vendorId,
                Quantity = quantity,
                TotalPurchasePrice = totalPrice,
                StockNumber = NewStockNumber(),
                CreatedOn = DateTime.Now,
                CreatedBy = userId,
                IsActive = true
            };
        }

        /// <summary>Creates a negative ledger row (stock out) in the same shape the Retail page uses for sales.</summary>
        public static Stock NewStockOutRow(int productId, int quantity, int userId)
        {
            return new Stock
            {
                ProductId = productId,
                Quantity = -Math.Abs(quantity),
                CreatedOn = DateTime.Now,
                CreatedBy = userId,
                IsActive = true
            };
        }

        public static StockMovement2 NewMovement(int productId, int? locationId, string type, int signedQuantity, decimal? unitCost,
            string documentType, int? documentId, string documentNumber, Stock? ledgerRow, string? reason, string? notes, int userId)
        {
            return new StockMovement2
            {
                ProductId = productId,
                LocationId = locationId,
                MovementType = type,
                Quantity = signedQuantity,
                UnitCost = unitCost,
                DocumentType = documentType,
                DocumentId = documentId,
                DocumentNumber = documentNumber,
                VendorStock = ledgerRow,
                Reason = reason,
                Notes = notes,
                CreatedOn = DateTime.Now,
                CreatedBy = userId
            };
        }
    }
}
