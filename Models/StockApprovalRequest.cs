using System;
using System.Collections.Generic;
using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StockApprovalRequest : ObservableObject
    {
        private string _requestId = string.Empty;
        private MovementType _transactionType = MovementType.StockOut;
        private string _productId = string.Empty;
        private string _productName = string.Empty;
        private string _sku = string.Empty;
        private decimal _quantity;
        private string _unit = "Pcs";
        private string _warehouseId = string.Empty;
        private string _warehouseName = string.Empty;
        private string _rackId = string.Empty;
        private string _rackCode = string.Empty;
        private string _sectionId = string.Empty;
        private string _sectionCode = string.Empty;
        private string? _batchNumber;
        private List<string> _serialNumbers = new();
        private string _requestedBy = string.Empty;
        private DateTime _requestedAt = DateTime.Now;
        private string _reason = "Production Requisition";
        private string? _referenceNumber;
        private MovementStatus _status = MovementStatus.PendingApproval;
        private string? _reviewedBy;
        private DateTime? _reviewedAt;
        private string? _reviewRemarks;

        public string RequestId
        {
            get => _requestId;
            set => SetProperty(ref _requestId, value);
        }

        public MovementType TransactionType
        {
            get => _transactionType;
            set => SetProperty(ref _transactionType, value);
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

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
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

        public string? BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public List<string> SerialNumbers
        {
            get => _serialNumbers;
            set => SetProperty(ref _serialNumbers, value);
        }

        public string RequestedBy
        {
            get => _requestedBy;
            set => SetProperty(ref _requestedBy, value);
        }

        public DateTime RequestedAt
        {
            get => _requestedAt;
            set => SetProperty(ref _requestedAt, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string? ReferenceNumber
        {
            get => _referenceNumber;
            set => SetProperty(ref _referenceNumber, value);
        }

        public MovementStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string? ReviewedBy
        {
            get => _reviewedBy;
            set => SetProperty(ref _reviewedBy, value);
        }

        public DateTime? ReviewedAt
        {
            get => _reviewedAt;
            set => SetProperty(ref _reviewedAt, value);
        }

        public string? ReviewRemarks
        {
            get => _reviewRemarks;
            set => SetProperty(ref _reviewRemarks, value);
        }

        public string QuantityDisplay => $"{Quantity:N0} {Unit}";
        public string RequestedAtDisplay => RequestedAt.ToString("yyyy-MM-dd HH:mm");
        public string LocationDisplay => $"{WarehouseName} [{RackCode} / {SectionCode}]";
    }
}

