using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Contants
{
    public static class Enums
    {
        public enum TransactionType
        {
            Purchase,   // Vendor buys product from supplier (if applicable)
            Sell   ,// Vendor sells product to client
            Return
        }

        /// <summary>
        /// Status of a stock item
        /// </summary>
        public enum StockStatus
        {
            InStock,
            OutOfStock,
            Reserved
        }

        /// <summary>
        /// Client type classification
        /// </summary>
        public enum ClientType
        {
            Regular,
            VIP,
            Wholesale
        }

        /// <summary>
        /// Severity of exceptions
        /// </summary>
        public enum ExceptionSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        public struct Applications
        {
            public const string Api= "AIT_Inventory"; 
            public const string Desktop= "AIT_Inventory_Desktop"; 
        }
        public struct TransactionTypeStruct
        {
            public const string Sell= "Sell"; 
            public const string Return= "Return"; 
        }
    }
}
