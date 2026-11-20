using System;
using System.Collections.Generic;
using System.Linq;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum StorageLocationType
    {
        Warehouse,
        Rack,
        Section,
        SubSection,
        Compartment,
        Bin
    }

    public enum CapacityType
    {
        Quantity,
        Weight,
        Volume,
        Unspecified
    }

    public class StorageLocation : ObservableObject
    {
        private bool _isSelected;
        private bool _isExpanded = true;
        private string _name = string.Empty;
        private string _locationCode = string.Empty;
        private decimal? _maxCapacity;
        private decimal _occupiedCapacity;
        private string _capacityUnit = "kg";
        private CapacityType _capacityType = CapacityType.Weight;
        private bool _isActive = true;

        public string LocationId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? RackId { get; set; }
        public string? RackCode { get; set; }
        public string? ParentLocationId { get; set; }
        public StorageLocationType LocationType { get; set; } = StorageLocationType.Section;

        public string LocationCode
        {
            get => _locationCode;
            set => SetProperty(ref _locationCode, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int LevelNumber { get; set; } = 1;

        // Physical Dimensions (mm / meters)
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Depth { get; set; }

        public CapacityType CapacityType
        {
            get => _capacityType;
            set => SetProperty(ref _capacityType, value);
        }

        public decimal? MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                if (SetProperty(ref _maxCapacity, value))
                {
                    OnPropertyChanged(nameof(HasCapacityConfigured));
                    OnPropertyChanged(nameof(OccupancyPercentage));
                    OnPropertyChanged(nameof(OccupancyDisplay));
                    OnPropertyChanged(nameof(RemainingCapacity));
                }
            }
        }

        public decimal OccupiedCapacity
        {
            get => _occupiedCapacity;
            set
            {
                if (SetProperty(ref _occupiedCapacity, value))
                {
                    OnPropertyChanged(nameof(OccupancyPercentage));
                    OnPropertyChanged(nameof(OccupancyDisplay));
                    OnPropertyChanged(nameof(RemainingCapacity));
                }
            }
        }

        public string CapacityUnit
        {
            get => _capacityUnit;
            set => SetProperty(ref _capacityUnit, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string Notes { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public List<StorageLocation> Children { get; set; } = new();
        public List<StoredProductItem> StoredItems { get; set; } = new();

        public bool HasCapacityConfigured => MaxCapacity.HasValue && MaxCapacity.Value > 0;

        public double OccupancyPercentage
        {
            get => HasCapacityConfigured
                ? Math.Min(100.0, (double)(OccupiedCapacity / MaxCapacity!.Value) * 100.0)
                : 0.0;
            set { }
        }

        public decimal RemainingCapacity => HasCapacityConfigured
            ? Math.Max(0m, MaxCapacity!.Value - OccupiedCapacity)
            : 0m;

        public string OccupancyDisplay => HasCapacityConfigured
            ? $"{OccupancyPercentage:F1}% ({OccupiedCapacity:N0} / {MaxCapacity:N0} {CapacityUnit})"
            : "Capacity Not Configured";

        public string OccupancyStatusText
        {
            get
            {
                if (!HasCapacityConfigured) return "Capacity Not Configured";
                if (OccupiedCapacity > MaxCapacity!.Value) return "Over Capacity";
                if (OccupancyPercentage >= 95.0) return "Full";
                if (OccupancyPercentage >= 70.0) return "High Occupancy";
                return "Normal";
            }
        }

        public StockStatus WorstStatus
        {
            get
            {
                if (StoredItems.Count == 0 && Children.Count == 0) return StockStatus.Normal;
                var allItems = GetAllContainedItems();
                if (allItems.Any(i => i.Status == StockStatus.Expired)) return StockStatus.Expired;
                if (allItems.Any(i => i.Status == StockStatus.Damaged)) return StockStatus.Damaged;
                if (allItems.Any(i => i.Status == StockStatus.NearExpiry)) return StockStatus.NearExpiry;
                if (allItems.Any(i => i.Status == StockStatus.LowStock)) return StockStatus.LowStock;
                return StockStatus.Normal;
            }
        }

        public List<StoredProductItem> GetAllContainedItems()
        {
            var list = new List<StoredProductItem>(StoredItems);
            foreach (var child in Children)
            {
                list.AddRange(child.GetAllContainedItems());
            }
            return list;
        }

        public string FullBreadcrumb => $"{WarehouseName} → {RackCode} → {LocationCode}";
    }
}
