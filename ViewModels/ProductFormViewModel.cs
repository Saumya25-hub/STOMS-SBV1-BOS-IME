using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class ProductFormViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly Action<Product> _onSaved;
        private readonly Action _onCancelled;

        private bool _isEditMode;
        private string _productId = string.Empty;
        private string _name = string.Empty;
        private string _sku = string.Empty;
        private bool _isSkuUserModified;
        private string _barcode = string.Empty;
        private string _selectedCategory = "Mechanical Power Transmission";
        private string _unit = "Pcs";
        private string _description = string.Empty;
        private decimal _minStockLevel = 10;
        private decimal _reorderLevel = 25;
        private ProductTrackingType _selectedTrackingType = ProductTrackingType.QuantityOnly;
        private bool _requiresExpiry;
        private bool _isActive = true;

        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;

        private string _errorMessage = string.Empty;
        private bool _isSaving;

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public string FormTitle => IsEditMode ? $"Edit Product Master: {Sku}" : "Add New Industrial Product Master";

        public string ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    if (!IsEditMode && !_isSkuUserModified)
                    {
                        _ = AutoGenerateSkuAsync();
                    }
                }
            }
        }

        public string Sku
        {
            get => _sku;
            set
            {
                if (SetProperty(ref _sku, value))
                {
                    // If user typed directly into SKU field, don't overwrite automatically
                    if (!_isSaving && !string.IsNullOrWhiteSpace(value))
                    {
                        _isSkuUserModified = true;
                    }
                }
            }
        }

        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    if (!IsEditMode && !_isSkuUserModified)
                    {
                        _ = AutoGenerateSkuAsync();
                    }
                }
            }
        }

        private string _brand = "Standard";
        private string _productType = "Finished Goods";
        private decimal _mrp = 0m;
        private decimal _gstRate = 18m;
        private int _expiryAlertDays = 10;
        private string _packagingUnit = "Box";
        private int _unitsPerPackage = 24;
        private decimal _purchaseRate = 125.00m;
        private decimal _sellingRate = 175.00m;

        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        public string ProductType
        {
            get => _productType;
            set => SetProperty(ref _productType, value);
        }

        public decimal Mrp
        {
            get => _mrp;
            set => SetProperty(ref _mrp, value);
        }

        public decimal GstRate
        {
            get => _gstRate;
            set => SetProperty(ref _gstRate, value);
        }

        public int ExpiryAlertDays
        {
            get => _expiryAlertDays;
            set => SetProperty(ref _expiryAlertDays, Math.Max(1, value));
        }

        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    OnPropertyChanged(nameof(PackagingConversionPreview));
                }
            }
        }

        public string PackagingUnit
        {
            get => _packagingUnit;
            set
            {
                if (SetProperty(ref _packagingUnit, value))
                {
                    OnPropertyChanged(nameof(PackagingConversionPreview));
                }
            }
        }

        public int UnitsPerPackage
        {
            get => _unitsPerPackage;
            set
            {
                if (SetProperty(ref _unitsPerPackage, Math.Max(1, value)))
                {
                    OnPropertyChanged(nameof(PackagingConversionPreview));
                }
            }
        }

        public decimal PurchaseRate
        {
            get => _purchaseRate;
            set => SetProperty(ref _purchaseRate, value);
        }

        public decimal SellingRate
        {
            get => _sellingRate;
            set => SetProperty(ref _sellingRate, value);
        }

        public string PackagingConversionPreview => $"1 {PackagingUnit} = {UnitsPerPackage} {Unit} (Ratio: 1:{UnitsPerPackage})";

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

        public ProductTrackingType SelectedTrackingType
        {
            get => _selectedTrackingType;
            set
            {
                if (SetProperty(ref _selectedTrackingType, value))
                {
                    OnPropertyChanged(nameof(IsBatchTracked));
                    OnPropertyChanged(nameof(IsSerialTracked));
                    OnPropertyChanged(nameof(CanConfigureExpiry));

                    if (!IsBatchTracked && !IsSerialTracked)
                    {
                        RequiresExpiry = false;
                    }
                }
            }
        }

        public bool CanConfigureExpiry => IsBatchTracked || IsSerialTracked;
        public bool IsBatchTracked => SelectedTrackingType == ProductTrackingType.BatchTracked || SelectedTrackingType == ProductTrackingType.BatchAndSerial;
        public bool IsSerialTracked => SelectedTrackingType == ProductTrackingType.SerialTracked || SelectedTrackingType == ProductTrackingType.BatchAndSerial;

        public bool RequiresExpiry
        {
            get => _requiresExpiry;
            set => SetProperty(ref _requiresExpiry, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    _ = LoadRacksForWarehouseAsync(value?.Id);
                }
            }
        }

        public Rack? SelectedRack
        {
            get => _selectedRack;
            set
            {
                if (SetProperty(ref _selectedRack, value))
                {
                    _ = LoadSectionsForRackAsync(value?.Id);
                }
            }
        }

        public RackSection? SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Units { get; } = new() { "Pcs", "Drums", "Meters", "Kg", "Bars", "Kits", "Liters", "Boxes" };
        public ObservableCollection<ProductTrackingType> TrackingTypes { get; } = new()
        {
            ProductTrackingType.QuantityOnly,
            ProductTrackingType.BatchTracked,
            ProductTrackingType.SerialTracked,
            ProductTrackingType.BatchAndSerial
        };

        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();
        public ObservableCollection<RackSection> Sections { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RegenerateSkuCommand { get; }
        public ICommand GenerateBarcodeCommand { get; }

        public ProductFormViewModel(
            IInventoryService inventoryService,
            Action<Product> onSaved,
            Action onCancelled,
            Product? existingProduct = null)
        {
            _inventoryService = inventoryService;
            _onSaved = onSaved;
            _onCancelled = onCancelled;

            SaveCommand = new AsyncRelayCommand(SaveProductAsync);
            CancelCommand = new RelayCommand(() => _onCancelled());
            RegenerateSkuCommand = new AsyncRelayCommand(async () =>
            {
                _isSkuUserModified = false;
                await AutoGenerateSkuAsync();
            });
            GenerateBarcodeCommand = new RelayCommand(() =>
            {
                Barcode = BarcodeService.Instance.GenerateInternalBarcode(BarcodeFormat.Ean13);
            });

            _ = InitializeFormAsync(existingProduct);
        }

        private async Task InitializeFormAsync(Product? existing)
        {
            // Load Categories
            var cats = await _inventoryService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
            if (!Categories.Contains("Mechanical Power Transmission")) Categories.Add("Mechanical Power Transmission");
            if (!Categories.Contains("Fluid & Flow Control")) Categories.Add("Fluid & Flow Control");
            if (!Categories.Contains("Chemicals & Lubricants")) Categories.Add("Chemicals & Lubricants");
            if (!Categories.Contains("Fasteners & Hardware")) Categories.Add("Fasteners & Hardware");
            if (!Categories.Contains("Sensors & Automation")) Categories.Add("Sensors & Automation");
            if (!Categories.Contains("Pneumatics")) Categories.Add("Pneumatics");
            if (!Categories.Contains("Motors & Drives")) Categories.Add("Motors & Drives");
            if (!Categories.Contains("Raw Materials")) Categories.Add("Raw Materials");

            // Load Warehouses
            var whs = await _inventoryService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in whs) Warehouses.Add(w);

            if (existing != null)
            {
                IsEditMode = true;
                _isSkuUserModified = true;
                ProductId = existing.Id;
                Name = existing.Name;
                Sku = existing.Sku;
                Barcode = existing.Barcode ?? string.Empty;
                SelectedCategory = existing.Category;
                Unit = existing.Unit;
                Description = existing.Description;
                MinStockLevel = existing.MinStockLevel;
                ReorderLevel = existing.ReorderLevel;
                SelectedTrackingType = existing.TrackingType;
                RequiresExpiry = existing.RequiresExpiry;
                IsActive = existing.IsActive;

                SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == existing.DefaultWarehouseId) ?? Warehouses.FirstOrDefault();
                if (SelectedWarehouse != null)
                {
                    await LoadRacksForWarehouseAsync(SelectedWarehouse.Id);
                    SelectedRack = Racks.FirstOrDefault(r => r.Id == existing.DefaultRackId) ?? Racks.FirstOrDefault();
                    if (SelectedRack != null)
                    {
                        await LoadSectionsForRackAsync(SelectedRack.Id);
                        SelectedSection = Sections.FirstOrDefault(s => s.Id == existing.DefaultSectionId) ?? Sections.FirstOrDefault();
                    }
                }
            }
            else
            {
                IsEditMode = false;
                _isSkuUserModified = false;
                SelectedWarehouse = Warehouses.FirstOrDefault();
                if (SelectedWarehouse != null)
                {
                    await LoadRacksForWarehouseAsync(SelectedWarehouse.Id);
                    SelectedRack = Racks.FirstOrDefault();
                    if (SelectedRack != null)
                    {
                        await LoadSectionsForRackAsync(SelectedRack.Id);
                        SelectedSection = Sections.FirstOrDefault();
                    }
                }
                await AutoGenerateSkuAsync();
            }
        }

        private async Task AutoGenerateSkuAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCategory)) return;
            try
            {
                string genSku = await _inventoryService.GenerateSkuAsync(SelectedCategory, Name);
                _isSaving = true; // prevent setting _isSkuUserModified
                Sku = genSku;
                _isSaving = false;
            }
            catch
            {
                // Fallback local SKU generation
                Sku = $"SKU-{DateTime.Now:yyMM}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
            }
        }

        private async Task LoadRacksForWarehouseAsync(string? warehouseId)
        {
            Racks.Clear();
            Sections.Clear();
            if (string.IsNullOrEmpty(warehouseId)) return;

            var racks = await _inventoryService.GetRacksByWarehouseAsync(warehouseId);
            foreach (var r in racks) Racks.Add(r);
            SelectedRack = Racks.FirstOrDefault();
        }

        private async Task LoadSectionsForRackAsync(string? rackId)
        {
            Sections.Clear();
            if (string.IsNullOrEmpty(rackId)) return;

            var secs = await _inventoryService.GetSectionsByRackAsync(rackId);
            foreach (var s in secs) Sections.Add(s);
            SelectedSection = Sections.FirstOrDefault();
        }

        private async Task SaveProductAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Product Name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Sku))
            {
                ErrorMessage = "Product SKU is required.";
                return;
            }

            if (MinStockLevel < 0)
            {
                ErrorMessage = "Minimum Stock Level cannot be negative.";
                return;
            }

            if (ReorderLevel < MinStockLevel)
            {
                ErrorMessage = "Reorder Level should be greater than or equal to Minimum Stock Level.";
                return;
            }

            IsSaving = true;

            try
            {
                // Validate SKU uniqueness
                bool isSkuUnique = await _inventoryService.IsSkuUniqueAsync(Sku.Trim(), IsEditMode ? ProductId : null);
                if (!isSkuUnique)
                {
                    ErrorMessage = $"SKU '{Sku.Trim()}' is already in use by another product. Please specify a unique SKU.";
                    IsSaving = false;
                    return;
                }

                // Validate Barcode uniqueness if supplied
                if (!string.IsNullOrWhiteSpace(Barcode))
                {
                    bool isBarcodeUnique = await _inventoryService.IsBarcodeUniqueAsync(Barcode.Trim(), IsEditMode ? ProductId : null);
                    if (!isBarcodeUnique)
                    {
                        ErrorMessage = $"Barcode '{Barcode.Trim()}' is already assigned to another product.";
                        IsSaving = false;
                        return;
                    }
                }

                var product = new Product
                {
                    Id = IsEditMode ? ProductId : string.Empty,
                    Name = Name.Trim(),
                    Sku = Sku.Trim().ToUpperInvariant(),
                    Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
                    Category = SelectedCategory,
                    Brand = Brand,
                    ProductType = ProductType,
                    Unit = Unit,
                    PackagingUnit = PackagingUnit,
                    ConversionFactor = UnitsPerPackage > 0 ? UnitsPerPackage : 1m,
                    PurchaseRate = PurchaseRate,
                    SellingRate = SellingRate,
                    Mrp = Mrp,
                    GstRate = GstRate,
                    ExpiryAlertDays = ExpiryAlertDays,
                    Description = Description.Trim(),
                    MinStockLevel = MinStockLevel,
                    ReorderLevel = ReorderLevel,
                    DefaultWarehouseId = SelectedWarehouse?.Id ?? string.Empty,
                    DefaultWarehouseName = SelectedWarehouse?.Name ?? string.Empty,
                    DefaultRackId = SelectedRack?.Id ?? string.Empty,
                    DefaultRackCode = SelectedRack?.RackCode ?? string.Empty,
                    DefaultSectionId = SelectedSection?.Id ?? string.Empty,
                    DefaultSectionCode = SelectedSection?.SectionCode ?? string.Empty,
                    TrackingType = SelectedTrackingType,
                    RequiresExpiry = RequiresExpiry,
                    IsActive = IsActive
                };

                // Register packaging unit in product.Units list
                product.Units.Add(new ProductUnit
                {
                    UnitName = Unit,
                    ConversionFactor = 1m,
                    PurchaseRate = PurchaseRate,
                    SellingRate = SellingRate,
                    Mrp = Mrp,
                    IsBaseUnit = true
                });

                if (!string.IsNullOrWhiteSpace(PackagingUnit) && !string.Equals(PackagingUnit, Unit, StringComparison.OrdinalIgnoreCase))
                {
                    product.Units.Add(new ProductUnit
                    {
                        UnitName = PackagingUnit,
                        ConversionFactor = UnitsPerPackage > 0 ? UnitsPerPackage : 1m,
                        PurchaseRate = PurchaseRate * UnitsPerPackage,
                        SellingRate = SellingRate,
                        Mrp = Mrp * UnitsPerPackage,
                        IsBaseUnit = false
                    });
                }

                Product saved;
                if (IsEditMode)
                {
                    saved = await _inventoryService.UpdateProductAsync(product);
                }
                else
                {
                    saved = await _inventoryService.CreateProductAsync(product);
                }

                _onSaved(saved);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Save failed: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}

