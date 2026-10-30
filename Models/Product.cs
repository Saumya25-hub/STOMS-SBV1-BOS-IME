using System;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class Product : ObservableObject
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _sku = string.Empty;
        private string? _barcode;
        private string _category = "General Spares";
        private string _unit = "Pcs";
        private string _description = string.Empty;
        private decimal _minStockLevel;
        private decimal _reorderLevel;
        private string _defaultWarehouseId = string.Empty;
        private string _defaultWarehouseName = string.Empty;
        private string _defaultRackId = string.Empty;
        private string _defaultRackCode = string.Empty;
        private string _defaultSectionId = string.Empty;
        private string _defaultSectionCode = string.Empty;
        private ProductTrackingType _trackingType = ProductTrackingType.QuantityOnly;
        private bool _isActive = true;
        private bool _requiresExpiry;
        private decimal _physicalStock;
        private decimal _availableStock;
        private decimal _reservedStock;
        private decimal _damagedStock;
        private decimal _expiredStock;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Sku
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public string? Barcode
        {
            get => _barcode;
            set
            {
                if (SetProperty(ref _barcode, value))
                {
                    OnPropertyChanged(nameof(HasBarcode));
                    OnPropertyChanged(nameof(BarcodeDisplay));
                }
            }
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal MinStockLevel
        {
            get => _minStockLevel;
            set => SetProperty(ref _minStockLevel, value);
        }

        public decimal ReorderLevel
        {
            get => _reorderLevel;
            set => SetProperty(ref _reorderLevel, value);
        }

        public string DefaultWarehouseId
        {
            get => _defaultWarehouseId;
            set => SetProperty(ref _defaultWarehouseId, value);
        }

        public string DefaultWarehouseName
        {
            get => _defaultWarehouseName;
            set => SetProperty(ref _defaultWarehouseName, value);
        }

        public string DefaultRackId
        {
            get => _defaultRackId;
            set => SetProperty(ref _defaultRackId, value);
        }

        public string DefaultRackCode
        {
            get => _defaultRackCode;
            set => SetProperty(ref _defaultRackCode, value);
        }

        public string DefaultSectionId
        {
            get => _defaultSectionId;
            set => SetProperty(ref _defaultSectionId, value);
        }

        public string DefaultSectionCode
        {
            get => _defaultSectionCode;
            set => SetProperty(ref _defaultSectionCode, value);
        }

        public ProductTrackingType TrackingType
        {
            get => _trackingType;
            set
            {
                if (SetProperty(ref _trackingType, value))
                {
                    OnPropertyChanged(nameof(TrackingTypeDisplay));
                    OnPropertyChanged(nameof(IsBatchTracked));
                    OnPropertyChanged(nameof(IsSerialTracked));
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool RequiresExpiry
        {
            get => _requiresExpiry;
            set => SetProperty(ref _requiresExpiry, value);
        }

        public decimal PhysicalStock
        {
            get => _physicalStock;
            set
            {
                if (SetProperty(ref _physicalStock, value))
                {
                    OnPropertyChanged(nameof(StockSummaryDisplay));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public decimal AvailableStock
        {
            get => _availableStock;
            set
            {
                if (SetProperty(ref _availableStock, value))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public decimal ReservedStock
        {
            get => _reservedStock;
            set => SetProperty(ref _reservedStock, value);
        }

        public decimal DamagedStock
        {
            get => _damagedStock;
            set => SetProperty(ref _damagedStock, value);
        }

        public decimal ExpiredStock
        {
            get => _expiredStock;
            set => SetProperty(ref _expiredStock, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        // Computed Properties & Presentation Helpers
        public bool HasBarcode => !string.IsNullOrWhiteSpace(Barcode);
        public string BarcodeDisplay => HasBarcode ? Barcode! : "No Barcode";

        public bool IsBatchTracked => TrackingType == ProductTrackingType.BatchTracked || TrackingType == ProductTrackingType.BatchAndSerial;
        public bool IsSerialTracked => TrackingType == ProductTrackingType.SerialTracked || TrackingType == ProductTrackingType.BatchAndSerial;

        public string TrackingTypeDisplay => TrackingType switch
        {
            ProductTrackingType.QuantityOnly => "Quantity Only",
            ProductTrackingType.BatchTracked => "Batch / Lot Tracked",
            ProductTrackingType.SerialTracked => "Serial Tracked",
            ProductTrackingType.BatchAndSerial => "Batch + Serial Tracked",
            _ => TrackingType.ToString()
        };

        public StockStatus Status
        {
            get
            {
                if (ExpiredStock > 0) return StockStatus.Expired;
                if (DamagedStock > 0 && AvailableStock <= 0) return StockStatus.Damaged;
                if (AvailableStock <= 0) return StockStatus.LowStock;
                if (AvailableStock <= ReorderLevel) return StockStatus.LowStock;
                return StockStatus.Normal;
            }
        }

        public string StatusDisplay => Status switch
        {
            StockStatus.Normal => "Optimal",
            StockStatus.LowStock => AvailableStock <= 0 ? "Out of Stock" : "Low Stock",
            StockStatus.NearExpiry => "Near Expiry",
            StockStatus.Expired => "Expired Stock",
            StockStatus.Damaged => "Quarantined / Damaged",
            _ => "Unknown"
        };

        public string StockSummaryDisplay => $"{PhysicalStock:N0} {Unit} (Avail: {AvailableStock:N0})";
        public string DefaultLocationDisplay => !string.IsNullOrEmpty(DefaultWarehouseName)
            ? $"{DefaultWarehouseName} / {DefaultRackCode} / {DefaultSectionCode}"
            : "No Default Location";
    }
}
