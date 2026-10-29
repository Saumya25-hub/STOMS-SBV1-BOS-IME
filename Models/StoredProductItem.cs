using System;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StoredProductItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Pcs";
        public DateTime? ExpiryDate { get; set; }
        public decimal ReorderThreshold { get; set; }
        public StockStatus Status { get; set; } = StockStatus.Normal;
        public string? CompartmentCode { get; set; }

        public string QuantityDisplay => $"{Quantity:N0} {Unit}";

        public string ExpiryDisplay => ExpiryDate.HasValue
            ? ExpiryDate.Value.ToString("yyyy-MM-dd")
            : "N/A (Non-perishable)";

        public string StatusDisplay => Status switch
        {
            StockStatus.Normal => "Optimal",
            StockStatus.LowStock => "Low Stock",
            StockStatus.NearExpiry => "Near Expiry",
            StockStatus.Expired => "Expired",
            StockStatus.Damaged => "Damaged / Quarantined",
            _ => "Unknown"
        };
    }
}

