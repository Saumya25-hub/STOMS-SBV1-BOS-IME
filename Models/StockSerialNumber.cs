using System;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public enum StockSerialStatus
    {
        Available,
        Reserved,
        Issued,
        Damaged,
        Quarantined
    }

    public class StockSerialNumber : ObservableObject
    {
        private string _id = string.Empty;
        private string _serialNumber = string.Empty;
        private string _productId = string.Empty;
        private string _productName = string.Empty;
        private string _sku = string.Empty;
        private string? _batchNumber;
        private string _warehouseId = string.Empty;
        private string _warehouseName = string.Empty;
        private string _rackId = string.Empty;
        private string _rackCode = string.Empty;
        private string _sectionId = string.Empty;
        private string _sectionCode = string.Empty;
        private StockSerialStatus _status = StockSerialStatus.Available;
        private DateTime _receivedDate = DateTime.Now;
        private DateTime? _issuedDate;
        private string? _issueTransactionId;
        private string? _notes;
        private bool _isSelected;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
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

        public string? BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
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

        public StockSerialStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public DateTime ReceivedDate
        {
            get => _receivedDate;
            set => SetProperty(ref _receivedDate, value);
        }

        public DateTime? IssuedDate
        {
            get => _issuedDate;
            set => SetProperty(ref _issuedDate, value);
        }

        public string? IssueTransactionId
        {
            get => _issueTransactionId;
            set => SetProperty(ref _issueTransactionId, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Computed properties
        public bool IsAvailable => Status == StockSerialStatus.Available;

        public string StatusDisplay => Status switch
        {
            StockSerialStatus.Available => "In Stock / Available",
            StockSerialStatus.Reserved => "Reserved for Issue",
            StockSerialStatus.Issued => "Dispatched / Issued",
            StockSerialStatus.Damaged => "Damaged",
            StockSerialStatus.Quarantined => "Quarantined",
            _ => Status.ToString()
        };

        public string LocationDisplay => !string.IsNullOrEmpty(WarehouseName)
            ? $"{WarehouseName} [{RackCode} / {SectionCode}]"
            : (Status == StockSerialStatus.Issued ? "Dispatched" : "Unassigned");
    }
}

