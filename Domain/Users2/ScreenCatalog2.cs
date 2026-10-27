namespace Domain.Users2
{
    /// <summary>
    /// Every screen of the app with a stable key, plus the default roles. This list is synced into the
    /// AppScreen2 table at startup. When you add a new page, add one line here and restart the app.
    /// </summary>
    public static class ScreenCatalog2
    {
        public const string Home = "Home";
        public const string SalesOrder = "Sales.Order";
        public const string SalesReturn = "Sales.Return";
        public const string CatalogProduct = "Catalog.Product";
        public const string CatalogClient = "Catalog.Client";
        public const string CatalogCategory = "Catalog.Category";
        public const string StockClassic = "Stock.Classic";

        public const string StockDashboard = "Stock.Dashboard";
        public const string StockOverview = "Stock.Overview";
        public const string StockPurchaseOrders = "Stock.PurchaseOrders";
        public const string StockReceiving = "Stock.Receiving";
        public const string StockIssues = "Stock.Issues";
        public const string StockTransfers = "Stock.Transfers";
        public const string StockAdjustments = "Stock.Adjustments";
        public const string StockReorder = "Stock.Reorder";
        public const string StockLocations = "Stock.Locations";
        public const string StockSuppliers = "Stock.Suppliers";
        public const string StockReportMovements = "Stock.Reports.Movements";
        public const string StockReportValuation = "Stock.Reports.Valuation";
        public const string StockReportAging = "Stock.Reports.Aging";
        public const string StockReportExpiry = "Stock.Reports.Expiry";
        public const string StockReportReconciliation = "Stock.Reports.Reconciliation";

        public const string RetailPosCheckout = "RetailPos.Checkout";
        public const string RetailPosClients = "RetailPos.Clients";
        public const string RetailPosReceivePayment = "RetailPos.ReceivePayment";
        public const string RetailPosStatement = "RetailPos.Statement";
        public const string RetailPosReports = "RetailPos.Reports";

        public const string UsersManage = "Users.Manage";
        public const string RolesManage = "Roles.Manage";
        public const string BusinessProfile = "Business.Profile";

        private static readonly string[] PublicPaths = { "/", "/setup", "/access-denied", "/not-found" };

        public static IReadOnlyList<ScreenDefinition2> All { get; } = new List<ScreenDefinition2>
        {
            new(Home, "Home", "General", "/home", 10),

            new(SalesOrder, "Order", "Sales", "/retail", 20),
            new(SalesReturn, "Returns", "Sales", "/return", 21),

            new(CatalogProduct, "Product", "Catalog", "/Product", 30),
            new(CatalogClient, "Client", "Catalog", "/Client", 31),
            new(CatalogCategory, "Category", "Catalog", "/Category", 32),
            new(StockClassic, "Stock (classic)", "Catalog", "/Stock", 33),

            new(StockDashboard, "Stock Dashboard", "Stock Management", "/stock2", 40),
            new(StockOverview, "Stock Overview", "Stock Management", "/stock2/overview", 41),
            new(StockPurchaseOrders, "Purchase Orders", "Stock Management", "/stock2/purchase-orders", 42),
            new(StockReceiving, "Receiving", "Stock Management", "/stock2/receiving", 43),
            new(StockIssues, "Issues", "Stock Management", "/stock2/issues", 44),
            new(StockTransfers, "Transfers", "Stock Management", "/stock2/transfers", 45),
            new(StockAdjustments, "Adjustments / Count", "Stock Management", "/stock2/adjustments", 46),
            new(StockReorder, "Low Stock / Reorder", "Stock Management", "/stock2/reorder", 47),
            new(StockLocations, "Locations", "Stock Management", "/stock2/locations", 48),
            new(StockSuppliers, "Suppliers", "Stock Management", "/stock2/suppliers", 49),
            new(StockReportMovements, "Movement Report", "Stock Reports", "/stock2/reports/movements", 50),
            new(StockReportValuation, "Valuation Report", "Stock Reports", "/stock2/reports/valuation", 51),
            new(StockReportAging, "Aging Report", "Stock Reports", "/stock2/reports/aging", 52),
            new(StockReportExpiry, "Expiry Report", "Stock Reports", "/stock2/reports/expiry", 53),
            new(StockReportReconciliation, "Reconciliation", "Stock Reports", "/stock2/reports/reconciliation", 54),

            new(RetailPosCheckout, "POS Checkout", "Retail POS", "/retailpos/checkout", 60),
            new(RetailPosClients, "POS Clients", "Retail POS", "/retailpos/clients", 61),
            new(RetailPosReceivePayment, "Receive Payment", "Retail POS", "/retailpos/receive-payment", 62),
            new(RetailPosStatement, "Client Statement", "Retail POS", "/retailpos/statement", 63),
            new(RetailPosReports, "POS Reports", "Retail POS", "/retailpos/reports", 64),

            new(UsersManage, "Users", "Administration", "/users", 90, true),
            new(RolesManage, "Roles & Permissions", "Administration", "/roles", 91, true),
            new(BusinessProfile, "Business Profile", "Administration", "/business", 92, true),
        };

        /// <summary>Roles created on the very first start. After that the root user owns them.</summary>
        public static IReadOnlyList<DefaultRoleDefinition2> DefaultRoles { get; } = new List<DefaultRoleDefinition2>
        {
            new("Manager", "Everything except administration.",
                All.Where(s => !s.IsRootOnly).Select(s => s.ScreenKey).ToList()),

            new("Cashier", "Sales counter.",
                new[] { Home, SalesOrder, SalesReturn, CatalogClient, RetailPosCheckout, RetailPosClients, RetailPosReceivePayment }),

            new("StoreKeeper", "Receives, issues and counts stock.",
                new[]
                {
                    Home, CatalogProduct, CatalogCategory,
                    StockDashboard, StockOverview, StockPurchaseOrders, StockReceiving, StockIssues,
                    StockTransfers, StockAdjustments, StockReorder, StockLocations, StockSuppliers
                }),

            new("Accountant", "Stock values and reports.",
                new[]
                {
                    Home, StockDashboard, StockOverview, StockPurchaseOrders,
                    RetailPosClients, RetailPosStatement, RetailPosReports,
                    StockReportMovements, StockReportValuation, StockReportAging, StockReportExpiry, StockReportReconciliation
                }),
        };

        public static bool IsPublicPath(string path)
        {
            string p = Normalize(path);
            return PublicPaths.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The screen a URL path belongs to (longest matching route prefix), or null for an unknown path.</summary>
        public static ScreenDefinition2? FindByPath(string path)
        {
            string p = Normalize(path);
            ScreenDefinition2? best = null;

            foreach (var screen in All)
            {
                bool match = string.Equals(p, screen.Route, StringComparison.OrdinalIgnoreCase)
                    || p.StartsWith(screen.Route + "/", StringComparison.OrdinalIgnoreCase);
                if (match && (best == null || screen.Route.Length > best.Route.Length)) best = screen;
            }

            return best;
        }

        /// <summary>The first screen (by menu order) a user may open; used as the landing page after login.</summary>
        public static string FirstAllowedRoute(ICollection<string> allowedKeys, bool isRoot)
        {
            var first = All.OrderBy(s => s.SortOrder)
                .FirstOrDefault(s => isRoot || (!s.IsRootOnly && allowedKeys.Contains(s.ScreenKey)));
            return first?.Route ?? "/access-denied";
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            int q = path.IndexOfAny(new[] { '?', '#' });
            if (q >= 0) path = path.Substring(0, q);
            if (path.Length > 1) path = path.TrimEnd('/');
            return path.Length == 0 ? "/" : path;
        }
    }
}
