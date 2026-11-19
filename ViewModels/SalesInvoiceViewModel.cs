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
    public class SalesInvoiceViewModel : ObservableObject
    {
        private readonly ISalesService _salesService;
        private readonly IInventoryService _inventoryService;
        private readonly IBarcodeService _barcodeService;
        private readonly INavigationService _navigationService;

        private SaleInvoice _currentInvoice = new();
        private string _barcodeInput = string.Empty;
        private string _productSearchQuery = string.Empty;
        private Product? _selectedCatalogProduct;
        private string _selectedUnit = "PCS";
        private decimal _enteredQuantity = 1m;
        private string _customerName = "Walk-in Retail Customer";
        private string _customerPhone = string.Empty;
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
        private decimal _discountAmount = 0m;
        private string _feedbackMessage = string.Empty;
        private bool _isSuccessDialogOpen;
        private FinalizeSaleResult? _lastSaleResult;
        private bool _isInvoiceHistoryOpen;

        public ObservableCollection<Product> CatalogProducts { get; } = new();
        public ObservableCollection<string> AvailableUnitsForSelectedProduct { get; } = new();
        public ObservableCollection<SaleInvoice> PastInvoices { get; } = new();

        public SaleInvoice CurrentInvoice
        {
            get => _currentInvoice;
            set => SetProperty(ref _currentInvoice, value);
        }

        public string BarcodeInput
        {
            get => _barcodeInput;
            set => SetProperty(ref _barcodeInput, value);
        }

        public string ProductSearchQuery
        {
            get => _productSearchQuery;
            set
            {
                if (SetProperty(ref _productSearchQuery, value))
                {
                    _ = FilterCatalogAsync();
                }
            }
        }

        public Product? SelectedCatalogProduct
        {
            get => _selectedCatalogProduct;
            set
            {
                if (SetProperty(ref _selectedCatalogProduct, value))
                {
                    UpdateUnitsForSelectedProduct();
                    OnPropertyChanged(nameof(SelectedProductAvailableStockDisplay));
                    OnPropertyChanged(nameof(SelectedProductPriceDisplay));
                }
            }
        }

        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (SetProperty(ref _selectedUnit, value))
                {
                    OnPropertyChanged(nameof(SelectedProductPriceDisplay));
                }
            }
        }

        public decimal EnteredQuantity
        {
            get => _enteredQuantity;
            set => SetProperty(ref _enteredQuantity, Math.Max(0.001m, value));
        }

        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    CurrentInvoice.CustomerName = value;
                }
            }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set
            {
                if (SetProperty(ref _customerPhone, value))
                {
                    CurrentInvoice.CustomerPhone = value;
                }
            }
        }

        public PaymentMethod SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (SetProperty(ref _selectedPaymentMethod, value))
                {
                    CurrentInvoice.PaymentMethod = value;
                }
            }
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    CurrentInvoice.DiscountAmount = value;
                    OnPropertyChanged(nameof(GrandTotalDisplay));
                }
            }
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public bool IsSuccessDialogOpen
        {
            get => _isSuccessDialogOpen;
            set => SetProperty(ref _isSuccessDialogOpen, value);
        }

        public FinalizeSaleResult? LastSaleResult
        {
            get => _lastSaleResult;
            set => SetProperty(ref _lastSaleResult, value);
        }

        public bool IsInvoiceHistoryOpen
        {
            get => _isInvoiceHistoryOpen;
            set => SetProperty(ref _isInvoiceHistoryOpen, value);
        }

        public string SelectedProductAvailableStockDisplay
        {
            get
            {
                if (SelectedCatalogProduct == null) return string.Empty;
                var breakdown = SelectedCatalogProduct.GetPackagingBreakdown();
                return $"In Stock: {breakdown.FormattedDescription}";
            }
        }

        public string SelectedProductPriceDisplay
        {
            get
            {
                if (SelectedCatalogProduct == null) return string.Empty;
                var unit = SelectedCatalogProduct.FindUnit(SelectedUnit);
                var rate = unit?.SellingRate > 0 ? unit.SellingRate : SelectedCatalogProduct.SellingRate;
                return $"Price: ₹ {rate:N2} / {SelectedUnit}";
            }
        }

        public string GrandTotalDisplay => $"₹ {CurrentInvoice.GrandTotal:N2}";

        // Commands
        public ICommand ScanBarcodeCommand { get; }
        public ICommand AddSelectedProductCommand { get; }
        public ICommand RemoveLineItemCommand { get; }
        public ICommand FinalizeSaleCommand { get; }
        public ICommand NewInvoiceCommand { get; }
        public ICommand CloseSuccessDialogCommand { get; }
        public ICommand ToggleInvoiceHistoryCommand { get; }
        public ICommand CancelInvoiceCommand { get; }

        public SalesInvoiceViewModel(
            ISalesService? salesService = null,
            IInventoryService? inventoryService = null,
            IBarcodeService? barcodeService = null,
            INavigationService? navigationService = null)
        {
            _salesService = salesService ?? SalesService.Instance;
            _inventoryService = inventoryService ?? MockInventoryService.Instance;
            _barcodeService = barcodeService ?? BarcodeService.Instance;
            _navigationService = navigationService ?? NavigationService.Instance;

            CurrentInvoice = _salesService.CreateDraftInvoice();

            ScanBarcodeCommand = new AsyncRelayCommand(async () => await ProcessBarcodeScanAsync());
            AddSelectedProductCommand = new AsyncRelayCommand(async () => await AddSelectedProductAsync());
            RemoveLineItemCommand = new RelayCommand<SaleInvoiceItem>(item =>
            {
                if (item != null)
                {
                    _salesService.RemoveItem(CurrentInvoice, item);
                    OnPropertyChanged(nameof(GrandTotalDisplay));
                }
            });

            FinalizeSaleCommand = new AsyncRelayCommand(async () => await FinalizeSaleAsync());
            NewInvoiceCommand = new RelayCommand(StartNewInvoice);
            CloseSuccessDialogCommand = new RelayCommand(() =>
            {
                IsSuccessDialogOpen = false;
                StartNewInvoice();
            });

            ToggleInvoiceHistoryCommand = new AsyncRelayCommand(async () =>
            {
                IsInvoiceHistoryOpen = !IsInvoiceHistoryOpen;
                if (IsInvoiceHistoryOpen)
                {
                    await LoadPastInvoicesAsync();
                }
            });

            CancelInvoiceCommand = new AsyncRelayCommand<SaleInvoice>(async inv =>
            {
                if (inv != null)
                {
                    var ok = await _salesService.CancelInvoiceAsync(inv.InvoiceId, "Customer Return");
                    if (ok)
                    {
                        ShowFeedback($"Invoice {inv.InvoiceNumber} cancelled. Stock restored to original batches.");
                        await LoadPastInvoicesAsync();
                    }
                }
            });

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await FilterCatalogAsync();
            await LoadPastInvoicesAsync();
        }

        private async Task FilterCatalogAsync()
        {
            var filter = new ProductFilterCriteria
            {
                SearchQuery = ProductSearchQuery,
                IsActive = true
            };
            var list = await _inventoryService.GetProductsAsync(filter, pageIndex: 1, pageSize: 50);
            CatalogProducts.Clear();
            foreach (var p in list) CatalogProducts.Add(p);
            if (SelectedCatalogProduct == null && CatalogProducts.Count > 0)
            {
                SelectedCatalogProduct = CatalogProducts.First();
            }
        }

        private void UpdateUnitsForSelectedProduct()
        {
            AvailableUnitsForSelectedProduct.Clear();
            if (SelectedCatalogProduct == null) return;

            AvailableUnitsForSelectedProduct.Add(SelectedCatalogProduct.Unit);
            if (!string.IsNullOrWhiteSpace(SelectedCatalogProduct.PackagingUnit) &&
                !string.Equals(SelectedCatalogProduct.PackagingUnit, SelectedCatalogProduct.Unit, StringComparison.OrdinalIgnoreCase))
            {
                AvailableUnitsForSelectedProduct.Add(SelectedCatalogProduct.PackagingUnit);
            }

            foreach (var u in SelectedCatalogProduct.Units)
            {
                if (!AvailableUnitsForSelectedProduct.Contains(u.UnitName))
                {
                    AvailableUnitsForSelectedProduct.Add(u.UnitName);
                }
            }

            // Default to packaging unit if configured, otherwise base unit
            SelectedUnit = !string.IsNullOrWhiteSpace(SelectedCatalogProduct.PackagingUnit)
                ? SelectedCatalogProduct.PackagingUnit
                : SelectedCatalogProduct.Unit;
        }

        public async Task ProcessBarcodeScanAsync()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput)) return;
            var barcode = BarcodeInput.Trim();
            BarcodeInput = string.Empty;

            var item = await _salesService.ScanBarcodeToInvoiceAsync(CurrentInvoice, barcode, 1m);
            if (item != null)
            {
                ShowFeedback($"Added {item.ProductName} ({item.Quantity} {item.SelectedUnit}) via barcode.");
                OnPropertyChanged(nameof(GrandTotalDisplay));
            }
            else
            {
                ShowFeedback($"Barcode '{barcode}' not found in Product Master.");
            }
        }

        public async Task AddSelectedProductAsync()
        {
            if (SelectedCatalogProduct == null || EnteredQuantity <= 0) return;

            var item = await _salesService.AddProductToInvoiceAsync(
                CurrentInvoice,
                SelectedCatalogProduct,
                EnteredQuantity,
                SelectedUnit);

            ShowFeedback($"Added {item.Quantity} {item.SelectedUnit} of {item.ProductName}.");
            OnPropertyChanged(nameof(GrandTotalDisplay));
            EnteredQuantity = 1m;
        }

        public async Task FinalizeSaleAsync()
        {
            if (CurrentInvoice.Items.Count == 0)
            {
                ShowFeedback("Please add at least one product before confirming the sale.");
                return;
            }

            CurrentInvoice.CustomerName = CustomerName;
            CurrentInvoice.CustomerPhone = CustomerPhone;
            CurrentInvoice.PaymentMethod = SelectedPaymentMethod;
            CurrentInvoice.DiscountAmount = DiscountAmount;

            var result = await _salesService.FinalizeSaleAsync(CurrentInvoice);
            LastSaleResult = result;

            if (result.Success)
            {
                IsSuccessDialogOpen = true;
                await FilterCatalogAsync();
                await LoadPastInvoicesAsync();
            }
            else
            {
                ShowFeedback($"Sale Blocked: {result.ErrorMessage}");
            }
        }

        private void StartNewInvoice()
        {
            CurrentInvoice = _salesService.CreateDraftInvoice();
            CustomerName = "Walk-in Retail Customer";
            CustomerPhone = string.Empty;
            DiscountAmount = 0m;
            OnPropertyChanged(nameof(GrandTotalDisplay));
        }

        private async Task LoadPastInvoicesAsync()
        {
            var list = await _salesService.GetInvoicesAsync();
            PastInvoices.Clear();
            foreach (var inv in list) PastInvoices.Add(inv);
        }

        private void ShowFeedback(string message)
        {
            FeedbackMessage = message;
            _ = Task.Delay(4000).ContinueWith(_ => FeedbackMessage = string.Empty);
        }
    }
}

