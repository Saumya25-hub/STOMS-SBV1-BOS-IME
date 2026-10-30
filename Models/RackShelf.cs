using System;
using System.Collections.Generic;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class RackShelf : ObservableObject
    {
        private string _shelfId = string.Empty;
        private string _shelfCode = string.Empty;
        private string _rackId = string.Empty;
        private string _rackCode = string.Empty;
        private string _warehouseId = string.Empty;
        private int _rowNumber = 1;
        private int _shelfIndex = 1;
        private decimal _maxCapacity = 500m;
        private decimal _occupiedCapacity;
        private string _capacityUnit = "kg";
        private bool _isSelected;

        public string ShelfId
        {
            get => _shelfId;
            set => SetProperty(ref _shelfId, value);
        }

        public string ShelfCode
        {
            get => _shelfCode;
            set => SetProperty(ref _shelfCode, value);
        }

        public string RackId
        {
            get => _rackId;
            set => SetProperty(ref _rackId, value);
        }

        public string RackCode
        {
            get => _rackCode;
            set => SetProperty(ref _rackCode, value);
        }

        public string WarehouseId
        {
            get => _warehouseId;
            set => SetProperty(ref _warehouseId, value);
        }

        public int RowNumber
        {
            get => _rowNumber;
            set => SetProperty(ref _rowNumber, value);
        }

        public int ShelfIndex
        {
            get => _shelfIndex;
            set => SetProperty(ref _shelfIndex, value);
        }

        public decimal MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                if (SetProperty(ref _maxCapacity, Math.Max(1m, value)))
                {
                    OnPropertyChanged(nameof(OccupancyPercentage));
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
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public string CapacityUnit
        {
            get => _capacityUnit;
            set => SetProperty(ref _capacityUnit, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public double OccupancyPercentage => MaxCapacity > 0
            ? Math.Min(100.0, (double)(OccupiedCapacity / MaxCapacity) * 100.0)
            : 0.0;

        public bool IsAvailable => OccupiedCapacity < MaxCapacity;

        public List<ShelfStoredItem> StoredItems { get; set; } = new();

        public string LocationTag => $"{RackCode} → {ShelfCode}";
    }

    public class ShelfStoredItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "PCS";
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;

        public string FormattedExpiry => ExpiryDate.HasValue
            ? ExpiryDate.Value.ToString("dd/MM/yyyy")
            : "No Expiry";
    }
}
