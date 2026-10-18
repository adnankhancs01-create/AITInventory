namespace Common.Stock2
{
    /// <summary>Shared constants for the Stock2 module (movement types, statuses, reasons, defaults).</summary>
    public static class StockConstants2
    {
        /// <summary>The existing code base has no logged-in user object; it hard-codes user 1. Keep the same convention.</summary>
        public const int SystemUserId = 1;

        public const string DefaultLocationCode = "MAIN";
        public const string DefaultLocationName = "Main Store";

        public const int ExpiryWarningDays = 30;

        // Movement types shown in the movement report
        public const string MoveSale = "Sale";
        public const string MovePurchase = "Purchase";
        public const string MoveReturn = "Return";
        public const string MoveOpening = "Opening stock";
        public const string MoveReceipt = "Receipt";
        public const string MoveIssue = "Issue";
        public const string MoveAdjustmentIn = "Adjustment +";
        public const string MoveAdjustmentOut = "Adjustment -";
        public const string MoveTransferOut = "Transfer out";
        public const string MoveTransferIn = "Transfer in";
        public const string MoveTransferCancel = "Transfer cancelled";
        public const string MoveOther = "Other";

        // Document statuses
        public const string StatusOpen = "Open";
        public const string StatusPartiallyReceived = "PartiallyReceived";
        public const string StatusReceived = "Received";
        public const string StatusCancelled = "Cancelled";
        public const string StatusPosted = "Posted";
        public const string StatusInTransit = "InTransit";

        // Document types stored on StockMovement2.DocumentType
        public const string DocReceipt = "Receipt";
        public const string DocIssue = "Issue";
        public const string DocTransfer = "Transfer";
        public const string DocAdjustment = "Adjustment";

        // Product stock status labels
        public const string StockOut = "Out of stock";
        public const string StockLow = "Low stock";
        public const string StockOk = "In stock";
        public const string StockNegative = "Negative stock";

        public static readonly string[] IssueReasons =
        {
            "Damaged", "Expired", "Lost / Stolen", "Internal use", "Sample / Promotion", "Returned to supplier", "Other"
        };

        public static readonly string[] AdjustmentReasons =
        {
            "Physical count", "Damaged", "Expired", "Lost / Stolen", "Found", "Data correction", "Other"
        };

        public static readonly string[] PurchaseOrderStatuses =
        {
            StatusOpen, StatusPartiallyReceived, StatusReceived, StatusCancelled
        };

        public static readonly string[] TransferStatuses =
        {
            StatusInTransit, StatusReceived, StatusCancelled
        };
    }
}
