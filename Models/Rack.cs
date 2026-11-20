using System;
using System.Collections.Generic;
using System.Linq;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class Rack : ObservableObject
    {
        private bool _isExpanded;
        private bool _isSelected;
        private RackSection? _selectedSection;
        private double _canvasX = 40;
        private double _canvasY = 40;
        private double _width = 250;
        private double _height = 150;
        private double _rotation = 0;
        private bool _isActive = true;

        public string Id { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Zone { get; set; } = "Aisle A";
        public string RackType { get; set; } = "Selective Pallet Rack";
        public string Description { get; set; } = string.Empty;
        public string Orientation { get; set; } = "Horizontal";
        public int NumberOfLevels { get; set; } = 4;

        // Physical Dimensions (meters)
        public double RackWidth { get; set; } = 2.5;
        public double RackHeight { get; set; } = 4.0;
        public double RackDepth { get; set; } = 1.0;

        public double Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public CapacityType CapacityType { get; set; } = CapacityType.Weight;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public double CanvasX
        {
            get => _canvasX;
            set => SetProperty(ref _canvasX, value);
        }

        public double CanvasY
        {
            get => _canvasY;
            set => SetProperty(ref _canvasY, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public decimal? MaxCapacity { get; set; }
        public decimal OccupiedCapacity { get; set; }
        public string CapacityUnit { get; set; } = "kg";

        public List<RackSection> Sections { get; set; } = new();
        public List<RackShelf> Shelves { get; set; } = new();

        public int ShelvesCount => Shelves.Count > 0 ? Shelves.Count : SectionsCount;

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public RackSection? SelectedSection
        {
            get => _selectedSection ?? Sections.FirstOrDefault();
            set => SetProperty(ref _selectedSection, value);
        }

        public int SectionsCount => Sections.Count;

        public int TotalProductsCount => Sections.Sum(s => s.Products.Count);

        public decimal TotalStockQuantity => Sections.Sum(s => s.TotalStockQuantity);

        public bool HasCapacityConfigured => MaxCapacity.HasValue && MaxCapacity.Value > 0;

        public double OccupancyPercentage
        {
            get => HasCapacityConfigured
                ? Math.Min(100.0, (double)(OccupiedCapacity / MaxCapacity!.Value) * 100.0)
                : 0;
            set { }
        }

        public string OccupancyDisplay => HasCapacityConfigured
            ? $"{OccupancyPercentage:F1}%"
            : "Not Configured";

        public string CapacityDetailsDisplay => HasCapacityConfigured
            ? $"{OccupiedCapacity:N0} / {MaxCapacity:N0} {CapacityUnit}"
            : "Capacity not configured";

        public StockStatus OverallStatus
        {
            get
            {
                if (Sections.Any(s => s.WorstStatus == StockStatus.Expired)) return StockStatus.Expired;
                if (Sections.Any(s => s.WorstStatus == StockStatus.Damaged)) return StockStatus.Damaged;
                if (Sections.Any(s => s.WorstStatus == StockStatus.NearExpiry)) return StockStatus.NearExpiry;
                if (Sections.Any(s => s.WorstStatus == StockStatus.LowStock)) return StockStatus.LowStock;
                return StockStatus.Normal;
            }
        }

        public string StatusBadgeText => OverallStatus switch
        {
            StockStatus.Normal => "Optimal Condition",
            StockStatus.LowStock => "Low Stock Alert",
            StockStatus.NearExpiry => "Near Expiry Alert",
            StockStatus.Expired => "Expired Stock Detected",
            StockStatus.Damaged => "Quarantined / Damaged",
            _ => "Status Unknown"
        };
    }
}

