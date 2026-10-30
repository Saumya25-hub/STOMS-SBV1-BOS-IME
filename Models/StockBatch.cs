using System;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StockBatch : ObservableObject
    {
        private string _batchId = string.Empty;
        private string _batchNumber = string.Empty;
        private string _productId = string.Empty;
        private string _productName = string.Empty;
        private string _sku = string.Empty;
        private string _warehouseId = string.Empty;
        private string _warehouseName = string.Empty;
        private string _rackId = string.Empty;
        private string _rackCode = string.Empty;
        private string _sectionId = string.Empty;
        private string _sectionCode = string.Empty;
        private decimal _receivedQuantity;
        private decimal _remainingQuantity;
        private string _unit = "Pcs";
        private DateTime? _mfgDate;
        private DateTime? _expiryDate;
        private DateTime _receivedDate = DateTime.Now;
        private string _supplier = string.Empty;
        private StockStatus _status = StockStatus.Normal;

        public string BatchId
        {
            get => _batchId;
            set => SetProperty(ref _batchId, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public string ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string Sku
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public string WarehouseId
        {
            get => _warehouseId;
            set => SetProperty(ref _warehouseId, value);
        }

        public string WarehouseName
        {
            get => _warehouseName;
            set => SetProperty(ref _warehouseName, value);
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

        public string SectionId
        {
            get => _sectionId;
            set => SetProperty(ref _sectionId, value);
        }

        public string SectionCode
        {
            get => _sectionCode;
            set => SetProperty(ref _sectionCode, value);
        }

        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set => SetProperty(ref _receivedQuantity, value);
        }

        public decimal RemainingQuantity
        {
            get => _remainingQuantity;
            set
            {
                if (SetProperty(ref _remainingQuantity, value))
                {
                    OnPropertyChanged(nameof(RemainingQuantityDisplay));
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public DateTime? MfgDate
        {
            get => _mfgDate;
            set
            {
                if (SetProperty(ref _mfgDate, value))
                {
                    OnPropertyChanged(nameof(MfgDisplay));
                }
            }
        }

        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set
            {
                if (SetProperty(ref _expiryDate, value))
                {
                    OnPropertyChanged(nameof(ExpiryDisplay));
                    OnPropertyChanged(nameof(IsExpired));
                    OnPropertyChanged(nameof(IsNearExpiry));
                    OnPropertyChanged(nameof(DaysRemaining));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public DateTime ReceivedDate
        {
            get => _receivedDate;
            set => SetProperty(ref _receivedDate, value);
        }

        public string Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        public StockStatus Status
        {
            get
            {
                if (IsExpired) return StockStatus.Expired;
                if (IsNearExpiry) return StockStatus.NearExpiry;
                return _status;
            }
            set => SetProperty(ref _status, value);
        }

        // Computed Properties
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
        public bool IsNearExpiry => ExpiryDate.HasValue && !IsExpired && (ExpiryDate.Value.Date - DateTime.Today).TotalDays <= 30;
        
        public int? DaysRemaining => ExpiryDate.HasValue
            ? (int)(ExpiryDate.Value.Date - DateTime.Today).TotalDays
            : null;

        public string ExpiryDisplay => ExpiryDate.HasValue
            ? ExpiryDate.Value.ToString("yyyy-MM-dd")
            : "No Expiry";

        public string MfgDisplay => MfgDate.HasValue
            ? MfgDate.Value.ToString("yyyy-MM-dd")
            : "N/A";

        public string RemainingQuantityDisplay => $"{RemainingQuantity:N0} / {ReceivedQuantity:N0} {Unit}";

        public string LocationDisplay => !string.IsNullOrEmpty(WarehouseName)
            ? $"{WarehouseName} [{RackCode} / {SectionCode}]"
            : "Unassigned";

        public string StatusDisplay => Status switch
        {
            StockStatus.Expired => $"Expired ({Math.Abs(DaysRemaining ?? 0)}d ago)",
            StockStatus.NearExpiry => $"Near Expiry ({DaysRemaining}d left)",
            StockStatus.Damaged => "Quarantined",
            _ => "Valid"
        };
    }
}

