using Common.Stock2;
using Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Service.Service
{
    /// <summary>
    /// Shared database helpers for the Stock2 services. Every method takes the caller's DbContext so the
    /// caller controls the transaction. The VendorStock table stays the single source of truth for on-hand
    /// quantity, so the existing Retail, Product and Stock pages keep working unchanged.
    /// </summary>
    public static class StockLedgerHelper2
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

        /// <summary>Returns the default location, creating "MAIN" on first use (no seed data is needed in the migration).</summary>
        public static async Task<StockLocation2> EnsureDefaultLocationAsync(InventoryDbContext db)
        {
            var def = await db.StockLocations2.FirstOrDefaultAsync(l => l.IsDefault);
            if (def != null) return def;

            var anyActive = await db.StockLocations2.OrderBy(l => l.Id).FirstOrDefaultAsync(l => l.IsActive);
            if (anyActive != null)
            {
                anyActive.IsDefault = true;
                await db.SaveChangesAsync();
                return anyActive;
            }

            def = new StockLocation2
            {
                Code = StockConstants2.DefaultLocationCode,
                Name = StockConstants2.DefaultLocationName,
                IsDefault = true,
                IsActive = true,
                CreatedBy = StockConstants2.SystemUserId
            };
            db.StockLocations2.Add(def);
            await db.SaveChangesAsync();
            return def;
        }

        /// <summary>Active ledger rows (optionally for some products only).</summary>
        public static async Task<List<StockLedgerRow2>> LoadActiveLedgerAsync(InventoryDbContext db, ICollection<int>? productIds = null)
        {
            var query = db.VendorStock.AsNoTracking()
                .Where(s => s.IsActive == true && s.ProductId != null);

            if (productIds != null)
                query = query.Where(s => productIds.Contains(s.ProductId.Value));

            return await query.Select(s => new StockLedgerRow2
            {
                Id = s.Id,
                ProductId = s.ProductId.Value,
                Quantity = s.Quantity ?? 0,
                TotalPurchasePrice = s.TotalPurchasePrice,
                CreatedOn = s.CreatedOn ?? DateTime.MinValue
            }).ToListAsync();
        }

        public static async Task<Dictionary<int, StockProductState2>> LoadStatesAsync(InventoryDbContext db, ICollection<int>? productIds = null)
        {
            var rows = await LoadActiveLedgerAsync(db, productIds);
            return StockLayerCalculator2.Build(rows);
        }

        /// <summary>Sum of non-default location balances per product.</summary>
        public static async Task<Dictionary<int, int>> LoadBalanceTotalsAsync(InventoryDbContext db, ICollection<int>? productIds = null)
        {
            var query = db.StockLocationBalances2.AsNoTracking().AsQueryable();
            if (productIds != null)
                query = query.Where(b => productIds.Contains(b.ProductId));

            return await query.GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
        }

        /// <summary>Quantity currently travelling between locations (transfers with status InTransit).</summary>
        public static async Task<Dictionary<int, int>> LoadInTransitAsync(InventoryDbContext db, ICollection<int>? productIds = null)
        {
            var query = db.StockTransferLines2.AsNoTracking()
                .Where(l => l.Transfer!.Status == StockConstants2.StatusInTransit);

            if (productIds != null)
                query = query.Where(l => productIds.Contains(l.ProductId));

            return await query.GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
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

        /// <summary>Quantity of one product at one location, computed inside the caller's transaction.</summary>
        public static async Task<int> GetQuantityAtLocationAsync(InventoryDbContext db, int productId, StockLocation2 location)
        {
            if (!location.IsDefault)
            {
                return await db.StockLocationBalances2
                    .Where(b => b.ProductId == productId && b.LocationId == location.Id)
                    .Select(b => b.Quantity).FirstOrDefaultAsync();
            }

            var ids = new List<int> { productId };
            var onHandRows = await LoadActiveLedgerAsync(db, ids);
            int onHand = onHandRows.Sum(r => r.Quantity);
            int balances = Get(await LoadBalanceTotalsAsync(db, ids), productId);
            int transit = Get(await LoadInTransitAsync(db, ids), productId);
            return QuantityAtLocation(onHand, balances, transit, true, 0);
        }

        /// <summary>Adds delta to the stored balance of a NON-default location. Throws when the result would be negative.</summary>
        public static async Task ChangeBalanceAsync(InventoryDbContext db, int productId, StockLocation2 location, int delta)
        {
            if (location.IsDefault || delta == 0) return;

            // Check rows added earlier in the same unit of work first (a receipt may list one product twice).
            var row = db.StockLocationBalances2.Local
                .FirstOrDefault(b => b.ProductId == productId && b.LocationId == location.Id)
                ?? await db.StockLocationBalances2
                    .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == location.Id);

            if (row == null)
            {
                row = new StockLocationBalance2 { ProductId = productId, LocationId = location.Id, Quantity = 0 };
                db.StockLocationBalances2.Add(row);
            }

            int result = row.Quantity + delta;
            if (result < 0)
                throw new InvalidOperationException($"Location '{location.Name}' cannot go below zero for product {productId}.");

            row.Quantity = result;
            row.ModifiedOn = DateTime.Now;
        }

        /// <summary>Creates a positive ledger row (stock in). TotalPurchasePrice may be null when the cost is unknown.</summary>
        public static VendorStock NewStockInRow(int productId, int quantity, decimal? totalPrice, int? vendorId, int userId)
        {
            return new VendorStock
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
        public static VendorStock NewStockOutRow(int productId, int quantity, int userId)
        {
            return new VendorStock
            {
                ProductId = productId,
                Quantity = -Math.Abs(quantity),
                CreatedOn = DateTime.Now,
                CreatedBy = userId,
                IsActive = true
            };
        }

        public static StockMovement2 NewMovement(int productId, int? locationId, string type, int signedQuantity, decimal? unitCost,
            string documentType, int? documentId, string documentNumber, VendorStock? ledgerRow, string? reason, string? notes, int userId)
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

        /// <summary>Validates the product ids of a document and returns them keyed by id (active products only).</summary>
        public static async Task<Dictionary<int, string>> LoadActiveProductNamesAsync(InventoryDbContext db, ICollection<int> productIds)
        {
            return await db.Products.AsNoTracking()
                .Where(p => p.IsActive && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name ?? p.ProductCode);
        }
    }
}
