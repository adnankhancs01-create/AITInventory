namespace Service.Service
{
    /// <summary>
    /// Builds per-product on-hand quantity and FIFO cost layers from VendorStock ledger rows.
    /// The rule matches the existing GetFifoStockReport: every negative row consumes the oldest positive rows first.
    /// </summary>
    public static class StockLayerCalculator2
    {
        public static Dictionary<int, StockProductState2> Build(IEnumerable<StockLedgerRow2> rows)
        {
            var result = new Dictionary<int, StockProductState2>();

            foreach (var group in rows.GroupBy(r => r.ProductId))
            {
                var positives = group.Where(r => r.Quantity > 0)
                    .OrderBy(r => r.CreatedOn).ThenBy(r => r.Id).ToList();

                int consumption = group.Where(r => r.Quantity < 0).Sum(r => -r.Quantity);

                // Fallback cost = weighted average of every costed receipt of this product.
                var costed = positives.Where(r => r.TotalPurchasePrice.GetValueOrDefault() > 0).ToList();
                decimal costedQty = costed.Sum(r => (decimal)r.Quantity);
                decimal fallback = costedQty > 0 ? costed.Sum(r => r.TotalPurchasePrice!.Value) / costedQty : 0m;

                var state = new StockProductState2
                {
                    ProductId = group.Key,
                    OnHand = group.Sum(r => r.Quantity)
                };

                if (costed.Count > 0)
                {
                    var last = costed[costed.Count - 1];
                    state.LastUnitCost = last.TotalPurchasePrice!.Value / last.Quantity;
                }

                foreach (var row in positives)
                {
                    int take = Math.Min(row.Quantity, consumption);
                    consumption -= take;
                    int remaining = row.Quantity - take;
                    if (remaining <= 0) continue;

                    bool hasCost = row.TotalPurchasePrice.GetValueOrDefault() > 0;
                    state.Layers.Add(new StockLayer2
                    {
                        VendorStockId = row.Id,
                        ProductId = group.Key,
                        ReceivedOn = row.CreatedOn,
                        OriginalQuantity = row.Quantity,
                        RemainingQuantity = remaining,
                        UnitCost = hasCost ? row.TotalPurchasePrice!.Value / row.Quantity : fallback,
                        IsEstimatedCost = !hasCost
                    });
                }

                state.StockValue = state.Layers.Sum(l => l.RemainingQuantity * l.UnitCost);
                int remainingTotal = state.Layers.Sum(l => l.RemainingQuantity);
                state.AverageUnitCost = remainingTotal > 0 ? state.StockValue / remainingTotal : state.LastUnitCost;
                state.HasEstimatedCost = state.Layers.Any(l => l.IsEstimatedCost);

                if (state.LastUnitCost == 0m) state.LastUnitCost = fallback;

                result[group.Key] = state;
            }

            return result;
        }

        /// <summary>Weighted FIFO unit cost of the next <paramref name="quantity"/> units (used to value issues and write-offs).</summary>
        public static decimal EstimateIssueCost(StockProductState2? state, int quantity)
        {
            if (state == null || quantity <= 0) return 0m;

            int left = quantity;
            decimal cost = 0m;

            foreach (var layer in state.Layers)
            {
                if (left <= 0) break;
                int take = Math.Min(left, layer.RemainingQuantity);
                cost += take * layer.UnitCost;
                left -= take;
            }

            // Not enough layers (negative stock): value the rest at the last known cost.
            if (left > 0) cost += left * state.LastUnitCost;

            return Math.Round(cost / quantity, 4);
        }
    }
}
