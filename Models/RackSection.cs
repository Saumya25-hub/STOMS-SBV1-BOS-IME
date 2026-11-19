using System;
using System.Collections.Generic;
using System.Linq;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class CompartmentBin
    {
        public string BinCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal? Capacity { get; set; }
        public decimal Occupied { get; set; }
        public List<StoredProductItem> Items { get; set; } = new();
    }

    public class RackSection : ObservableObject
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Id { get; set; } = string.Empty;
        public string RackId { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TierLevel { get; set; } = 1;
        
        /// <summary>
        /// Configured section weight or volumetric capacity (null if unconfigured).
        /// </summary>
        public decimal? MaxCapacity { get; set; }
        public decimal OccupiedCapacity { get; set; }
        public string CapacityUnit { get; set; } = "kg";

        public List<StoredProductItem> Products { get; set; } = new();
        public List<CompartmentBin> Compartments { get; set; } = new();

        public bool HasCapacityConfigured => MaxCapacity.HasValue && MaxCapacity.Value > 0;

        public double OccupancyPercentage => HasCapacityConfigured
            ? Math.Min(100.0, (double)(OccupiedCapacity / MaxCapacity!.Value) * 100.0)
            : 0;

        public string OccupancyDisplay => HasCapacityConfigured
            ? $"{OccupancyPercentage:F1}% ({OccupiedCapacity:N0} / {MaxCapacity:N0} {CapacityUnit})"
            : "Capacity not configured";

        public int ProductCount => Products.Count;

        public decimal TotalStockQuantity => Products.Sum(p => p.Quantity);

        public bool IsEmpty => Products.Count == 0;

        public StockStatus WorstStatus
        {
            get
            {
                if (Products.Count == 0) return StockStatus.Normal;
                if (Products.Any(p => p.Status == StockStatus.Expired)) return StockStatus.Expired;
                if (Products.Any(p => p.Status == StockStatus.Damaged)) return StockStatus.Damaged;
                if (Products.Any(p => p.Status == StockStatus.NearExpiry)) return StockStatus.NearExpiry;
                if (Products.Any(p => p.Status == StockStatus.LowStock)) return StockStatus.LowStock;
                return StockStatus.Normal;
            }
        }

        public string SummaryDescription
        {
            get
            {
                if (Products.Count == 0) return "Empty Section (Available for allocation)";
                var names = Products.Select(p => p.Name).Take(2);
                var summary = string.Join(", ", names);
                if (Products.Count > 2) summary += $" +{Products.Count - 2} more";
                return summary;
            }
        }
    }
}

