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
    public class StockIssueViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly Action<StockIssueResult> _onCompleted;
        private readonly Action _onCancelled;

        private Product? _selectedProduct;
        private decimal _quantity = 1;
        private StockBatch? _selectedBatch;
        private string _selectedReason = "Production Issue";
        private string _referenceNumber = string.Empty;
        private string _notes = string.Empty;
        private bool _requiresApproval;

        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;

        private string _errorMessage = string.Empty;
        private bool _isSubmitting;

        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();
        public ObservableCollection<RackSection> Sections { get; } = new();
        public ObservableCollection<StockBatch> AvailableBatches { get; } = new();
        public ObservableCollection<StockSerialNumber> AvailableSerials { get; } = new();

        public ObservableCollection<string> IssueReasons { get; } = new()
        {
            "Production Issue",
            "Customer Dispatch",
            "Maintenance & Overhaul",
            "Internal Testing / R&D",
            "Scrap / Damaged Isolation"
        };

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    OnPropertyChanged(nameof(IsBatchTracked));
                    OnPropertyChanged(nameof(IsSerialTracked));
                    OnPropertyChanged(nameof(ProductSku));
                    OnPropertyChanged(nameof(ProductUnit));
                    OnPropertyChanged(nameof(AvailableStock));
                    OnPropertyChanged(nameof(PhysicalStock));
                    OnPropertyChanged(nameof(ReservedStock));

                    if (value != null)
                    {
                        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == value.DefaultWarehouseId) ?? Warehouses.FirstOrDefault();
                        _ = LoadBatchesAndSerialsAsync();
                    }
                }
            }
        }

        public string ProductSku => SelectedProduct?.Sku ?? "—";
        public string ProductUnit => SelectedProduct?.Unit ?? "Pcs";
        public decimal AvailableStock => SelectedProduct?.AvailableStock ?? 0;
        public decimal PhysicalStock => SelectedProduct?.PhysicalStock ?? 0;
        public decimal ReservedStock => SelectedProduct?.ReservedStock ?? 0;

        public bool IsBatchTracked => SelectedProduct?.IsBatchTracked ?? false;
        public bool IsSerialTracked => SelectedProduct?.IsSerialTracked ?? false;

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(SelectedSerialsCountLabel));
                }
            }
        }

        public StockBatch? SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public string SelectedReason
        {
            get => _selectedReason;
            set => SetProperty(ref _selectedReason, value);
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

        public bool RequiresApproval
        {
            get => _requiresApproval;
            set => SetProperty(ref _requiresApproval, value);
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
            set
            {
                if (SetProperty(ref _selectedSection, value))
                {
                    _ = LoadBatchesAndSerialsAsync();
                }
            }
        }

        public int SelectedSerialsCount => AvailableSerials.Count(s => s.IsSelected);

        public string SelectedSerialsCountLabel => IsSerialTracked
            ? $"Select Serials ({SelectedSerialsCount} / {(int)Quantity} selected)"
            : "Select Serials";

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
        public ICommand AutoSelectFefoBatchCommand { get; }
        public ICommand ToggleSerialSelectionCommand { get; }

        public StockIssueViewModel(
            IInventoryService inventoryService,
            Action<StockIssueResult> onCompleted,
            Action onCancelled,
            string? preselectedProductId = null)
        {
            _inventoryService = inventoryService;
            _onCompleted = onCompleted;
            _onCancelled = onCancelled;

            SubmitCommand = new AsyncRelayCommand(SubmitIssueAsync);
            CancelCommand = new RelayCommand(() => _onCancelled());
            AutoSelectFefoBatchCommand = new RelayCommand(AutoSelectFefoBatch);
            ToggleSerialSelectionCommand = new RelayCommand<StockSerialNumber>(ToggleSerial);

            ReferenceNumber = $"REQ-ISS-{DateTime.Now:MMdd}-{new Random().Next(10, 99)}";

            _ = InitializeAsync(preselectedProductId);
        }

        private async Task InitializeAsync(string? preselectedProductId)
        {
            var prods = await _inventoryService.GetProductsAsync(new ProductFilterCriteria { IsActive = true });
            Products.Clear();
            foreach (var p in prods) Products.Add(p);

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

        private async Task LoadBatchesAndSerialsAsync()
        {
            AvailableBatches.Clear();
            AvailableSerials.Clear();

            if (SelectedProduct == null) return;

            if (IsBatchTracked)
            {
                var batches = await _inventoryService.GetBatchesForProductAsync(SelectedProduct.Id, SelectedWarehouse?.Id);
                // Exclude expired batches from standard selection
                var validBatches = batches.Where(b => !b.IsExpired && b.RemainingQuantity > 0).ToList();
                foreach (var b in validBatches) AvailableBatches.Add(b);

                AutoSelectFefoBatch();
            }

            if (IsSerialTracked)
            {
                var serials = await _inventoryService.GetSerialNumbersForProductAsync(
                    SelectedProduct.Id,
                    SelectedWarehouse?.Id,
                    StockSerialStatus.Available);

                foreach (var s in serials)
                {
                    s.PropertyChanged += (o, e) =>
                    {
                        if (e.PropertyName == nameof(StockSerialNumber.IsSelected))
                        {
                            OnPropertyChanged(nameof(SelectedSerialsCount));
                            OnPropertyChanged(nameof(SelectedSerialsCountLabel));
                        }
                    };
                    AvailableSerials.Add(s);
                }

                // Preselect first N serials to match Quantity
                AutoSelectSerials();
            }
        }

        private void AutoSelectFefoBatch()
        {
            // FEFO: nearest expiry date first
            SelectedBatch = AvailableBatches
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .FirstOrDefault();
        }

        private void AutoSelectSerials()
        {
            int target = (int)Quantity;
            for (int i = 0; i < AvailableSerials.Count; i++)
            {
                AvailableSerials[i].IsSelected = (i < target);
            }
            OnPropertyChanged(nameof(SelectedSerialsCount));
            OnPropertyChanged(nameof(SelectedSerialsCountLabel));
        }

        private void ToggleSerial(StockSerialNumber? serial)
        {
            if (serial == null) return;
            serial.IsSelected = !serial.IsSelected;
            OnPropertyChanged(nameof(SelectedSerialsCount));
            OnPropertyChanged(nameof(SelectedSerialsCountLabel));
        }

        private async Task SubmitIssueAsync()
        {
            ErrorMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product to issue.";
                return;
            }

            if (Quantity <= 0)
            {
                ErrorMessage = "Issue quantity must be greater than zero.";
                return;
            }

            if (Quantity > SelectedProduct.AvailableStock)
            {
                ErrorMessage = $"Requested quantity ({Quantity:N0} {ProductUnit}) exceeds total available stock ({SelectedProduct.AvailableStock:N0} {ProductUnit}). Negative inventory is not permitted.";
                return;
            }

            if (SelectedWarehouse == null || SelectedRack == null || SelectedSection == null)
            {
                ErrorMessage = "Source storage location (Warehouse, Rack, Section) must be selected.";
                return;
            }

            if (IsBatchTracked)
            {
                if (SelectedBatch == null)
                {
                    ErrorMessage = "Please select a valid batch to issue.";
                    return;
                }

                if (SelectedBatch.RemainingQuantity < Quantity)
                {
                    ErrorMessage = $"Selected batch '{SelectedBatch.BatchNumber}' only has {SelectedBatch.RemainingQuantity:N0} remaining, which is insufficient for requested {Quantity:N0}.";
                    return;
                }
            }

            List<string> selectedSerials = new();
            if (IsSerialTracked)
            {
                selectedSerials = AvailableSerials.Where(s => s.IsSelected).Select(s => s.SerialNumber).ToList();
                int expected = (int)Quantity;
                if (selectedSerials.Count != expected)
                {
                    ErrorMessage = $"Please select exactly {expected} serial numbers ({selectedSerials.Count} currently selected).";
                    return;
                }
            }

            IsSubmitting = true;

            try
            {
                var dto = new StockIssueDto
                {
                    ProductId = SelectedProduct.Id,
                    Quantity = Quantity,
                    Unit = SelectedProduct.Unit,
                    WarehouseId = SelectedWarehouse.Id,
                    RackId = SelectedRack.Id,
                    SectionId = SelectedSection.Id,
                    BatchNumber = SelectedBatch?.BatchNumber,
                    SelectedSerialNumbers = selectedSerials,
                    Reason = SelectedReason,
                    ReferenceNumber = ReferenceNumber.Trim(),
                    Notes = Notes.Trim(),
                    IssuedBy = "Saumya Chaudhari",
                    RequiresApproval = RequiresApproval
                };

                var result = await _inventoryService.RequestOrIssueStockAsync(dto);
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
                ErrorMessage = $"Issue error: {ex.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
