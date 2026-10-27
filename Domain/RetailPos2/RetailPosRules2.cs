using Common.RetailPos2;

namespace Domain.RetailPos2
{
    /// <summary>The money rules of the Retail POS module. Pure functions, no database.</summary>
    public static class RetailPosRules2
    {
        public static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>OutstandingAmount = TotalAmount (+ linked adjustments) - payments received. Never below zero.</summary>
        public static decimal Outstanding(decimal totalAmount, decimal adjustmentAmount, decimal paidAmount)
        {
            return Math.Max(0m, Round(totalAmount + adjustmentAmount - paidAmount));
        }

        /// <summary>The current status of an invoice, calculated from its payments: Paid, PartiallyPaid or Credit.</summary>
        public static string StatusFor(decimal outstanding, decimal paid)
        {
            if (outstanding <= 0m) return RetailPosConstants2.Paid;
            return paid > 0m ? RetailPosConstants2.PartiallyPaid : RetailPosConstants2.Credit;
        }

        public static bool IsOverdue(DateTime? dueDate, decimal outstanding, DateTime today)
        {
            return dueDate.HasValue && outstanding > 0m && dueDate.Value.Date < today.Date;
        }

        /// <summary>
        /// Checks the payment type against the amounts and returns the amount received at checkout, or an error.
        /// Paid = whole total, PartiallyPaid = more than 0 and less than the total, Credit = nothing.
        /// Partial and credit sales need a client, so a walk-in sale can never create a debt.
        /// </summary>
        public static string? CheckoutPayment(string paymentType, decimal total, decimal paidNow, bool hasClient, out decimal paidAtCheckout)
        {
            paidAtCheckout = 0m;

            if (!RetailPosConstants2.PaymentTypes.Contains(paymentType))
                return "Choose a valid payment type (Paid, PartiallyPaid or Credit).";
            if (total <= 0m)
                return "The sale total must be greater than zero.";

            if (paymentType == RetailPosConstants2.Paid)
            {
                paidAtCheckout = total;
                return null;
            }

            if (!hasClient)
                return "Select a client for a partially paid or credit sale. A walk-in sale must be paid in full.";

            if (paymentType == RetailPosConstants2.Credit)
            {
                paidAtCheckout = 0m;
                return null;
            }

            paidNow = Round(paidNow);
            if (paidNow <= 0m)
                return "Enter the amount received now. Use Credit if nothing is paid.";
            if (paidNow >= total)
                return "The amount received equals or exceeds the total. Use Paid in full instead.";

            paidAtCheckout = paidNow;
            return null;
        }

        /// <summary>True when the new debt would push the client above the limit. A null limit means no limit.</summary>
        public static bool ExceedsCreditLimit(decimal? creditLimit, decimal currentBalance, decimal newOutstanding)
        {
            return creditLimit.HasValue && newOutstanding > 0m && currentBalance + newOutstanding > creditLimit.Value;
        }


        /// <summary>Completes an invoice row read from the database: outstanding, current status and overdue flag.</summary>
        public static RetailPosSaleRowDto2 FinishSaleRow(RetailPosSaleRowDto2 row, DateTime today)
        {
            row.OutstandingAmount = Outstanding(row.TotalAmount, row.AdjustmentAmount, row.PaidAmount);
            row.PaymentStatus = StatusFor(row.OutstandingAmount, row.PaidAmount);
            row.IsOverdue = IsOverdue(row.DueDate, row.OutstandingAmount, today);
            return row;
        }

        public static string DocumentPrefix(string prefix, DateTime date)
        {
            return $"{prefix}-{date:yyyyMMdd}-";
        }

        /// <summary>Document number such as INV-20261026-0007.</summary>
        public static string BuildDocumentNumber(string prefix, DateTime date, int existingToday)
        {
            return $"{prefix}-{date:yyyyMMdd}-{existingToday + 1:D4}";
        }
    }
}
