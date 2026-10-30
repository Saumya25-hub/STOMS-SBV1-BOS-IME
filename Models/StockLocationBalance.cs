using STOMS_SBV1_BOS_IME.Common;

namespace STOMS_SBV1_BOS_IME.Models
{
    public class StockLocationBalance : ObservableObject
    {
        private string _productId = string.Empty;
        private string _productName = string.Empty;
        private string _sku = string.Empty;
        private string _warehouseId = string.Empty;
        private string _warehouseName = string.Empty;
        private string _rackId = string.Empty;
        private string _rackCode = string.Empty;
        private string _sectionId = string.Empty;
        private string _sectionCode = string.Empty;
        private decimal _physicalQuantity;
        private decimal _availableQuantity;
        private decimal _reservedQuantity;
        private decimal _damagedQuantity;
        private decimal _expiredQuantity;
        private string _unit = "Pcs";

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

        public decimal PhysicalQuantity
        {
            get => _physicalQuantity;
            set => SetProperty(ref _physicalQuantity, value);
        }

        public decimal AvailableQuantity
        {
            get => _availableQuantity;
            set => SetProperty(ref _availableQuantity, value);
        }

        public decimal ReservedQuantity
        {
            get => _reservedQuantity;
            set => SetProperty(ref _reservedQuantity, value);
        }

        public decimal DamagedQuantity
        {
            get => _damagedQuantity;
            set => SetProperty(ref _damagedQuantity, value);
        }

        public decimal ExpiredQuantity
        {
            get => _expiredQuantity;
            set => SetProperty(ref _expiredQuantity, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public string LocationDisplay => $"{WarehouseName} [{RackCode} / {SectionCode}]";
        public string PhysicalDisplay => $"{PhysicalQuantity:N0} {Unit}";
        public string AvailableDisplay => $"{AvailableQuantity:N0} {Unit}";
    }
}
