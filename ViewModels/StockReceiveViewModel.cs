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
    public class StockReceiveViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly Action<StockReceiveResult> _onCompleted;
        private readonly Action _onCancelled;

        private Product? _selectedProduct;
        private decimal _quantity = 1;
        private string _batchNumber = string.Empty;
        private DateTime? _mfgDate = DateTime.Today.AddDays(-10);
        private DateTime? _expiryDate = DateTime.Today.AddMonths(12);
        private string _serialsInputText = string.Empty;
        private string _supplier = string.Empty;
        private string _referenceNumber = string.Empty;
        private string _notes = string.Empty;

        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;

        private string _errorMessage = string.Empty;
        private bool _isSubmitting;

        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();
        public ObservableCollection<RackSection> Sections { get; } = new();

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    OnPropertyChanged(nameof(IsBatchTracked));
                    OnPropertyChanged(nameof(IsSerialTracked));
                    OnPropertyChanged(nameof(RequiresExpiry));
                    OnPropertyChanged(nameof(ProductSku));
                    OnPropertyChanged(nameof(ProductUnit));
                    OnPropertyChanged(nameof(CurrentPhysicalStock));
                    OnPropertyChanged(nameof(CurrentAvailableStock));

                    if (value != null)
                    {
                        // Auto-fill default location
                        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == value.DefaultWarehouseId) ?? Warehouses.FirstOrDefault();
                    }
                }
            }
        }

        public string ProductSku => SelectedProduct?.Sku ?? "—";
        public string ProductUnit => SelectedProduct?.Unit ?? "Pcs";
        public decimal CurrentPhysicalStock => SelectedProduct?.PhysicalStock ?? 0;
        public decimal CurrentAvailableStock => SelectedProduct?.AvailableStock ?? 0;

        public bool IsBatchTracked => SelectedProduct?.IsBatchTracked ?? false;
        public bool IsSerialTracked => SelectedProduct?.IsSerialTracked ?? false;
        public bool RequiresExpiry => SelectedProduct?.RequiresExpiry ?? false;

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(SerialsCountLabel));
                }
            }
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public DateTime? MfgDate
        {
            get => _mfgDate;
            set => SetProperty(ref _mfgDate, value);
        }

        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set => SetProperty(ref _expiryDate, value);
        }

        public string SerialsInputText
        {
            get => _serialsInputText;
            set
            {
                if (SetProperty(ref _serialsInputText, value))
                {
                    OnPropertyChanged(nameof(EnteredSerialsCount));
                    OnPropertyChanged(nameof(SerialsCountLabel));
                }
            }
        }

        public int EnteredSerialsCount => ParseSerialNumbers().Count;

        public string SerialsCountLabel => IsSerialTracked
            ? $"Serial Numbers ({EnteredSerialsCount} / {(int)Quantity} entered)"
            : "Serial Numbers";

        public string Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value);
        }

        public string ReferenceNumber
        {
            get => _referenceNumber;
            set => SetProperty(ref _referenceNumber, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    _ = LoadRacksAsync(value?.Id);
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
                    _ = LoadSectionsAsync(value?.Id);
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

        public bool IsSubmitting
        {
            get => _isSubmitting;
            set => SetProperty(ref _isSubmitting, value);
        }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AutoGenerateSerialsCommand { get; }

        public StockReceiveViewModel(
            IInventoryService inventoryService,
            Action<StockReceiveResult> onCompleted,
            Action onCancelled,
            string? preselectedProductId = null)
        {
            _inventoryService = inventoryService;
            _onCompleted = onCompleted;
            _onCancelled = onCancelled;

            SubmitCommand = new AsyncRelayCommand(SubmitReceiveAsync);
            CancelCommand = new RelayCommand(() => _onCancelled());
            AutoGenerateSerialsCommand = new RelayCommand(AutoGenerateSerials);

            _ = InitializeAsync(preselectedProductId);
        }

        private async Task InitializeAsync(string? preselectedProductId)
        {
            // Load Products
            var prods = await _inventoryService.GetProductsAsync(new ProductFilterCriteria { IsActive = true });
            Products.Clear();
            foreach (var p in prods) Products.Add(p);

            // Load Warehouses
            var whs = await _inventoryService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in whs) Warehouses.Add(w);

            if (!string.IsNullOrEmpty(preselectedProductId))
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == preselectedProductId) ?? Products.FirstOrDefault();
            }
            else
            {
                SelectedProduct = Products.FirstOrDefault();
            }

            // Generate default batch number
            BatchNumber = $"BAT-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}";
            ReferenceNumber = $"GRN-PO-{DateTime.Now:MMdd}-{new Random().Next(10, 99)}";
        }

        private async Task LoadRacksAsync(string? warehouseId)
        {
            Racks.Clear();
            Sections.Clear();
            if (string.IsNullOrEmpty(warehouseId)) return;

            var racks = await _inventoryService.GetRacksByWarehouseAsync(warehouseId);
            foreach (var r in racks) Racks.Add(r);

            if (SelectedProduct != null && !string.IsNullOrEmpty(SelectedProduct.DefaultRackId))
            {
                SelectedRack = Racks.FirstOrDefault(r => r.Id == SelectedProduct.DefaultRackId) ?? Racks.FirstOrDefault();
            }
            else
            {
                SelectedRack = Racks.FirstOrDefault();
            }
        }

        private async Task LoadSectionsAsync(string? rackId)
        {
            Sections.Clear();
            if (string.IsNullOrEmpty(rackId)) return;

            var secs = await _inventoryService.GetSectionsByRackAsync(rackId);
            foreach (var s in secs) Sections.Add(s);

            if (SelectedProduct != null && !string.IsNullOrEmpty(SelectedProduct.DefaultSectionId))
            {
                SelectedSection = Sections.FirstOrDefault(s => s.Id == SelectedProduct.DefaultSectionId) ?? Sections.FirstOrDefault();
            }
            else
            {
                SelectedSection = Sections.FirstOrDefault();
            }
        }

        private void AutoGenerateSerials()
        {
            if (SelectedProduct == null || Quantity <= 0) return;
            int count = (int)Quantity;
            string prefix = SelectedProduct.Sku.Replace("SKU-", "SN-");
            var generated = new List<string>();
            int seed = new Random().Next(1000, 8000);

            for (int i = 0; i < count; i++)
            {
                generated.Add($"{prefix}-{(seed + i):D4}");
            }

            SerialsInputText = string.Join(Environment.NewLine, generated);
        }

        private List<string> ParseSerialNumbers()
        {
            if (string.IsNullOrWhiteSpace(SerialsInputText)) return new List<string>();

            return SerialsInputText
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private async Task SubmitReceiveAsync()
        {
            ErrorMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product to receive.";
                return;
            }

            if (Quantity <= 0)
            {
                ErrorMessage = "Receipt quantity must be greater than zero.";
                return;
            }

            if (SelectedWarehouse == null || SelectedRack == null || SelectedSection == null)
            {
                ErrorMessage = "Valid destination Warehouse, Rack, and Tier Section are required.";
                return;
            }

            if (IsBatchTracked && string.IsNullOrWhiteSpace(BatchNumber))
            {
                ErrorMessage = "Batch/Lot number is required for batch-tracked items.";
                return;
            }

            if (RequiresExpiry)
            {
                if (!ExpiryDate.HasValue)
                {
                    ErrorMessage = "Expiry Date is mandatory for this perishable product.";
                    return;
                }

                if (ExpiryDate.Value.Date <= DateTime.Today)
                {
                    ErrorMessage = "Expiry Date must be a future date.";
                    return;
                }
            }

            List<string> parsedSerials = new();
            if (IsSerialTracked)
            {
                parsedSerials = ParseSerialNumbers();
                int expectedCount = (int)Quantity;
                if (parsedSerials.Count != expectedCount)
                {
                    ErrorMessage = $"Expected {expectedCount} serial numbers for {Quantity} {ProductUnit}, but found {parsedSerials.Count}.";
                    return;
                }

                // Check for duplicates
                var duplicates = parsedSerials.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
                if (duplicates.Any())
                {
                    ErrorMessage = $"Duplicate serial numbers entered: {string.Join(", ", duplicates.Select(g => g.Key))}.";
                    return;
                }
            }

            IsSubmitting = true;

            try
            {
                var dto = new StockReceiveDto
                {
                    ProductId = SelectedProduct.Id,
                    Quantity = Quantity,
                    Unit = SelectedProduct.Unit,
                    BatchNumber = IsBatchTracked ? BatchNumber.Trim() : null,
                    MfgDate = MfgDate,
                    ExpiryDate = ExpiryDate,
                    SerialNumbers = parsedSerials,
                    WarehouseId = SelectedWarehouse.Id,
                    RackId = SelectedRack.Id,
                    SectionId = SelectedSection.Id,
                    Supplier = Supplier.Trim(),
                    ReferenceNumber = ReferenceNumber.Trim(),
                    Notes = Notes.Trim(),
                    ReceivedBy = "Saumya Chaudhari"
                };

                var result = await _inventoryService.ReceiveStockAsync(dto);
                if (result.Success)
                {
                    _onCompleted(result);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Receipt error: {ex.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
