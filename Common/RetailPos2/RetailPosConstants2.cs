namespace Common.RetailPos2
{
    /// <summary>
    /// Shared terms of the Retail POS module. Payment TYPE (how the sale is settled) and payment METHOD
    /// (cash, card...) are different concepts and are never mixed.
    /// </summary>
    public static class RetailPosConstants2
    {
        // ---- payment type (status of settlement)
        public const string Paid = "Paid";
        public const string PartiallyPaid = "PartiallyPaid";
        public const string Credit = "Credit";
        public static readonly string[] PaymentTypes = { Paid, PartiallyPaid, Credit };

        // ---- payment method
        public const string MethodCash = "Cash";
        public static readonly string[] PaymentMethods = { "Cash", "Card", "BankTransfer", "Cheque", "MobileWallet", "Other" };

        // ---- adjustment types
        public const string AdjustmentCreditNote = "CreditNote";
        public const string AdjustmentManual = "Adjustment";
        public static readonly string[] AdjustmentTypes = { AdjustmentCreditNote, AdjustmentManual };

        /// <summary>Ledger adjustment types written by the sale-return workflow (not offered in the manual adjustment screen).</summary>
        public const string AdjustmentSaleReturn = "SaleReturn";
        public const string AdjustmentRefund = "Refund";

        public const string StockReturnToStock = "ReturnToStock";
        public const string StatusReturned = "Returned";
        public const string StockReturnDocumentType = "RetailPosReturn";

        // ---- statement entry types
        public const string EntryOpening = "Opening balance";
        public const string EntrySale = "Sale";
        public const string EntryPayment = "Payment";
        public const string EntryPaymentReversal = "Payment reversal";
        public const string EntryCreditNote = "Credit note";
        public const string EntryAdjustment = "Adjustment";
        public const string EntrySaleReturn = "Sale return";
        public const string EntryRefund = "Refund paid";
        public const string EntryBroughtForward = "Balance brought forward";

        public const string InvoicePrefix = "INV";
        public const string ReceiptPrefix = "RCP";
        public const string ReturnPrefix = "RET";
        /// <summary>Stored on StockMovement2.DocumentType for the stock-out of a POS sale.</summary>
        public const string StockDocumentType = "RetailPosSale";

        public const string WalkInName = "Walk-in customer";

        public const decimal MaxAmount = 999_999_999_999.99m;
    }
}
