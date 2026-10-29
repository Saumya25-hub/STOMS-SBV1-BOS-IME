using System;
using System.Collections.Generic;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class UnitStockBreakdown
    {
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Formatted => $"{Quantity:N0} {Unit}";
    }

    public class DashboardSummary
    {
        /// <summary>
        /// Distinct active product records count.
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// Primary aggregate stock quantity.
        /// </summary>
        public decimal TotalStockQuantity { get; set; }

        public string PrimaryUnit { get; set; } = "Items";

        /// <summary>
        /// Breakdown across different non-interchangeable units (e.g. Pcs, Boxes, Kg, Liters).
        /// Prevents combining incompatible units.
        /// </summary>
        public List<UnitStockBreakdown> UnitBreakdowns { get; set; } = new();

        /// <summary>
        /// Count of products whose available quantity <= configured reorder threshold.
        /// </summary>
        public int LowStockItemsCount { get; set; }

        /// <summary>
        /// Batches approaching configured expiry warning threshold (e.g., within 30 days).
        /// </summary>
        public int NearExpiryBatchesCount { get; set; }

        /// <summary>
        /// Batches whose expiry date has already passed.
        /// </summary>
        public int ExpiredStockBatchesCount { get; set; }

        /// <summary>
        /// Stock issue or adjustment requests waiting for supervisor sign-off.
        /// </summary>
        public int PendingApprovalsCount { get; set; }

        public DateTime LastRefreshed { get; set; } = DateTime.Now;

        public bool IsDemoData { get; set; } = false;

        public string DataOriginDescription => IsDemoData
            ? "Development Sandbox Demo Dataset (Offline Safe)"
            : "Central Web API (Live Sync)";
    }
}

