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
    public class ProductManagementViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly INavigationService _navigationService;

        private string _activeSection = "Catalog"; // "Catalog", "Balances", "Batches", "Serials", "Approvals"
        private Product? _selectedProduct;
        private bool _isLoading;

        // Search & Filters
        private string _searchQuery = string.Empty;
        private string _selectedCategory = "All Categories";
        private string _selectedTrackingTypeFilter = "All Tracking";
        private string _selectedStatusFilter = "All Statuses";
        private string _selectedWarehouseFilter = "All Warehouses";
        private string _selectedActiveFilter = "Active Only";

        // Modal ViewModels
        private bool _isProductFormOpen;
        private ProductFormViewModel? _currentProductForm;

        private bool _isReceiveFormOpen;
        private StockReceiveViewModel? _currentReceiveForm;

        private bool _isIssueFormOpen;
        private StockIssueViewModel? _currentIssueForm;

        private bool _isDetailsOpen;
        private ProductDetailsViewModel? _currentProductDetails;

        // Feedback Banner
        private string _feedbackMessage = string.Empty;
        private string _feedbackSeverity = "Success"; // "Success", "Warning", "Danger", "Info"
        private bool _isFeedbackVisible;

        // Collections
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<StockBatch> Batches { get; } = new();
        public ObservableCollection<StockSerialNumber> SerialNumbers { get; } = new();
        public ObservableCollection<StockLocationBalance> LocationBalances { get; } = new();
        public ObservableCollection<StockApprovalRequest> PendingApprovals { get; } = new();

        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Warehouses { get; } = new();
        public ObservableCollection<string> TrackingTypeFilters { get; } = new()
        {
            "All Tracking",
            "Quantity Only",
            "Batch / Lot Tracked",
            "Serial Tracked",
            "Batch + Serial Tracked"
        };
        public ObservableCollection<string> StatusFilters { get; } = new()
        {
            "All Statuses",
            "Optimal",
            "Low Stock",
            "Near Expiry",
            "Expired",
            "Quarantined / Damaged"
        };
        public ObservableCollection<string> ActiveFilters { get; } = new()
        {
            "Active Only",
            "All Products",
            "Inactive Only"
        };

        public string ActiveSection
        {
            get => _activeSection;
            set
            {
                if (SetProperty(ref _activeSection, value))
                {
                    OnPropertyChanged(nameof(IsCatalogActive));
                    OnPropertyChanged(nameof(IsBalancesActive));
                    OnPropertyChanged(nameof(IsBatchesActive));
                    OnPropertyChanged(nameof(IsSerialsActive));
                    OnPropertyChanged(nameof(IsApprovalsActive));
                }
            }
        }

        public bool IsCatalogActive => ActiveSection == "Catalog";
        public bool IsBalancesActive => ActiveSection == "Balances";
        public bool IsBatchesActive => ActiveSection == "Batches";
        public bool IsSerialsActive => ActiveSection == "Serials";
        public bool IsApprovalsActive => ActiveSection == "Approvals";

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = RefreshCatalogAsync();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = RefreshCatalogAsync();
                }
            }
        }

        public string SelectedTrackingTypeFilter
        {
            get => _selectedTrackingTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTrackingTypeFilter, value))
                {
                    _ = RefreshCatalogAsync();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    _ = RefreshCatalogAsync();
                }
            }
        }

        public string SelectedWarehouseFilter
        {
            get => _selectedWarehouseFilter;
            set
            {
                if (SetProperty(ref _selectedWarehouseFilter, value))
                {
                    _ = RefreshCatalogAsync();
                    _ = RefreshBatchesAndSerialsAsync();
                }
            }
        }

        public string SelectedActiveFilter
        {
            get => _selectedActiveFilter;
            set
            {
                if (SetProperty(ref _selectedActiveFilter, value))
                {
                    _ = RefreshCatalogAsync();
                }
            }
        }

        // Modal Workflows
        public bool IsProductFormOpen
        {
            get => _isProductFormOpen;
            set => SetProperty(ref _isProductFormOpen, value);
        }

        public ProductFormViewModel? CurrentProductForm
        {
            get => _currentProductForm;
            set => SetProperty(ref _currentProductForm, value);
        }

        public bool IsReceiveFormOpen
        {
            get => _isReceiveFormOpen;
            set => SetProperty(ref _isReceiveFormOpen, value);
        }

        public StockReceiveViewModel? CurrentReceiveForm
        {
            get => _currentReceiveForm;
            set => SetProperty(ref _currentReceiveForm, value);
        }

        public bool IsIssueFormOpen
        {
            get => _isIssueFormOpen;
            set => SetProperty(ref _isIssueFormOpen, value);
        }

        public StockIssueViewModel? CurrentIssueForm
        {
            get => _currentIssueForm;
            set => SetProperty(ref _currentIssueForm, value);
        }

        public bool IsDetailsOpen
        {
            get => _isDetailsOpen;
            set => SetProperty(ref _isDetailsOpen, value);
        }

        public ProductDetailsViewModel? CurrentProductDetails
        {
            get => _currentProductDetails;
            set => SetProperty(ref _currentProductDetails, value);
        }

        // Feedback Banner
        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public string FeedbackSeverity
        {
            get => _feedbackSeverity;
            set => SetProperty(ref _feedbackSeverity, value);
        }

        public bool IsFeedbackVisible
        {
            get => _isFeedbackVisible;
            set => SetProperty(ref _isFeedbackVisible, value);
        }

        // Telemetry Counters
        public int TotalProductCount => Products.Count;
        public int LowStockCount => Products.Count(p => p.Status == StockStatus.LowStock);
        public int ExpiredCount => Products.Count(p => p.Status == StockStatus.Expired);
        public int PendingApprovalsCount => PendingApprovals.Count;

        // Commands
        public ICommand SetSectionCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand ReceiveStockCommand { get; }
        public ICommand IssueStockCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ApproveRequestCommand { get; }
        public ICommand RejectRequestCommand { get; }
        public ICommand DismissFeedbackCommand { get; }
        public ICommand CloseModalCommand { get; }

        public ProductManagementViewModel(
            IInventoryService? inventoryService = null,
            INavigationService? navigationService = null,
            string initialSection = "Catalog")
        {
            _inventoryService = inventoryService ?? MockInventoryService.Instance;
            _navigationService = navigationService ?? NavigationService.Instance;
            _activeSection = initialSection;

            SetSectionCommand = new RelayCommand<string>(sec => { if (!string.IsNullOrEmpty(sec)) ActiveSection = sec; });
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync);
            AddProductCommand = new RelayCommand(OpenAddProduct);
            EditProductCommand = new RelayCommand<Product>(OpenEditProduct);
            ViewDetailsCommand = new RelayCommand<Product>(OpenProductDetails);
            ReceiveStockCommand = new RelayCommand<Product>(OpenReceiveStock);
            IssueStockCommand = new RelayCommand<Product>(OpenIssueStock);
            ToggleActiveCommand = new AsyncRelayCommand<Product>(ToggleProductActiveAsync);
            ApproveRequestCommand = new AsyncRelayCommand<StockApprovalRequest>(ApproveRequestAsync);
            RejectRequestCommand = new AsyncRelayCommand<StockApprovalRequest>(RejectRequestAsync);
            DismissFeedbackCommand = new RelayCommand(() => IsFeedbackVisible = false);
            CloseModalCommand = new RelayCommand(CloseAllModals);

            _inventoryService.InventoryChanged += (s, e) => _ = RefreshAllAsync();

            _ = InitializeFiltersAndDataAsync();
        }

        private async Task InitializeFiltersAndDataAsync()
        {
            // Load Categories
            var cats = await _inventoryService.GetCategoriesAsync();
            Categories.Clear();
            Categories.Add("All Categories");
            foreach (var c in cats) Categories.Add(c);

            // Load Warehouses
            var whs = await _inventoryService.GetWarehousesAsync();
            Warehouses.Clear();
            Warehouses.Add("All Warehouses");
            foreach (var w in whs) Warehouses.Add(w.Code);

            await RefreshAllAsync();
        }

        public async Task RefreshAllAsync()
        {
            IsLoading = true;
            try
            {
                await RefreshCatalogAsync();
                await RefreshBatchesAndSerialsAsync();
                await RefreshApprovalsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshCatalogAsync()
        {
            var filter = BuildFilterCriteria();
            var list = await _inventoryService.GetProductsAsync(filter);
            Products.Clear();
            foreach (var p in list) Products.Add(p);

            // Also reload all location balances
            LocationBalances.Clear();
            foreach (var prod in list)
            {
                var locs = await _inventoryService.GetLocationBalancesForProductAsync(prod.Id);
                foreach (var loc in locs) LocationBalances.Add(loc);
            }

            OnPropertyChanged(nameof(TotalProductCount));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(ExpiredCount));
        }

        private async Task RefreshBatchesAndSerialsAsync()
        {
            var batches = await _inventoryService.GetAllBatchesAsync(
                SelectedWarehouseFilter != "All Warehouses" ? SelectedWarehouseFilter : null);
            Batches.Clear();
            foreach (var b in batches) Batches.Add(b);

            var serials = await _inventoryService.GetAllSerialNumbersAsync(
                SelectedWarehouseFilter != "All Warehouses" ? SelectedWarehouseFilter : null);
            SerialNumbers.Clear();
            foreach (var s in serials) SerialNumbers.Add(s);
        }

        private async Task RefreshApprovalsAsync()
        {
            var approvals = await _inventoryService.GetPendingApprovalsAsync();
            PendingApprovals.Clear();
            foreach (var a in approvals) PendingApprovals.Add(a);
            OnPropertyChanged(nameof(PendingApprovalsCount));
        }

        private ProductFilterCriteria BuildFilterCriteria()
        {
            var criteria = new ProductFilterCriteria
            {
                SearchQuery = SearchQuery,
                Category = SelectedCategory
            };

            if (SelectedTrackingTypeFilter != "All Tracking")
            {
                criteria.TrackingType = SelectedTrackingTypeFilter switch
                {
                    "Quantity Only" => ProductTrackingType.QuantityOnly,
                    "Batch / Lot Tracked" => ProductTrackingType.BatchTracked,
                    "Serial Tracked" => ProductTrackingType.SerialTracked,
                    "Batch + Serial Tracked" => ProductTrackingType.BatchAndSerial,
                    _ => null
                };
            }

            if (SelectedStatusFilter != "All Statuses")
            {
                criteria.Status = SelectedStatusFilter switch
                {
                    "Optimal" => StockStatus.Normal,
                    "Low Stock" => StockStatus.LowStock,
                    "Near Expiry" => StockStatus.NearExpiry,
                    "Expired" => StockStatus.Expired,
                    "Quarantined / Damaged" => StockStatus.Damaged,
                    _ => null
                };
            }

            if (SelectedActiveFilter == "Active Only") criteria.IsActive = true;
            else if (SelectedActiveFilter == "Inactive Only") criteria.IsActive = false;
            else criteria.IsActive = null;

            return criteria;
        }

        private void ClearFilters()
        {
            SearchQuery = string.Empty;
            SelectedCategory = "All Categories";
            SelectedTrackingTypeFilter = "All Tracking";
            SelectedStatusFilter = "All Statuses";
            SelectedWarehouseFilter = "All Warehouses";
            SelectedActiveFilter = "Active Only";
        }

        // Modals Management
        public void OpenAddProduct()
        {
            CloseAllModals();
            CurrentProductForm = new ProductFormViewModel(
                _inventoryService,
                savedProduct =>
                {
                    CloseAllModals();
                    ShowFeedback($"Product '{savedProduct.Sku}' ({savedProduct.Name}) created successfully.", "Success");
                    _ = RefreshCatalogAsync();
                },
                () => CloseAllModals());
            IsProductFormOpen = true;
        }

        public void OpenEditProduct(Product? product)
        {
            if (product == null) return;
            CloseAllModals();
            CurrentProductForm = new ProductFormViewModel(
                _inventoryService,
                savedProduct =>
                {
                    CloseAllModals();
                    ShowFeedback($"Product '{savedProduct.Sku}' updated successfully.", "Success");
                    _ = RefreshCatalogAsync();
                },
                () => CloseAllModals(),
                product);
            IsProductFormOpen = true;
        }

        public void OpenProductDetails(Product? product)
        {
            if (product == null) return;
            CloseAllModals();
            CurrentProductDetails = new ProductDetailsViewModel(
                product,
                _inventoryService,
                () => CloseAllModals(),
                p => OpenEditProduct(p),
                p => OpenReceiveStock(p),
                p => OpenIssueStock(p));
            IsDetailsOpen = true;
        }

        public void OpenReceiveStock(Product? product = null)
        {
            CloseAllModals();
            CurrentReceiveForm = new StockReceiveViewModel(
                _inventoryService,
                result =>
                {
                    CloseAllModals();
                    ShowFeedback(result.Message, "Success");
                    _ = RefreshAllAsync();
                },
                () => CloseAllModals(),
                product?.Id);
            IsReceiveFormOpen = true;
        }

        public void OpenIssueStock(Product? product = null)
        {
            CloseAllModals();
            CurrentIssueForm = new StockIssueViewModel(
                _inventoryService,
                result =>
                {
                    CloseAllModals();
                    string severity = result.IsPendingApproval ? "Warning" : "Success";
                    ShowFeedback(result.Message, severity);
                    _ = RefreshAllAsync();
                },
                () => CloseAllModals(),
                product?.Id);
            IsIssueFormOpen = true;
        }

        private async Task ToggleProductActiveAsync(Product? product)
        {
            if (product == null) return;
            bool success = await _inventoryService.ToggleProductActiveAsync(product.Id);
            if (success)
            {
                string state = product.IsActive ? "activated" : "deactivated";
                ShowFeedback($"Product '{product.Sku}' has been {state}.", "Info");
            }
        }

        private async Task ApproveRequestAsync(StockApprovalRequest? request)
        {
            if (request == null) return;
            bool success = await _inventoryService.ApproveIssueRequestAsync(request.RequestId, "Saumya Chaudhari");
            if (success)
            {
                ShowFeedback($"Stock issue request '{request.RequestId}' approved and finalized.", "Success");
                await RefreshAllAsync();
            }
            else
            {
                ShowFeedback($"Failed approving request '{request.RequestId}'.", "Danger");
            }
        }

        private async Task RejectRequestAsync(StockApprovalRequest? request)
        {
            if (request == null) return;
            bool success = await _inventoryService.RejectIssueRequestAsync(request.RequestId, "Saumya Chaudhari", "Declined by Operations Manager");
            if (success)
            {
                ShowFeedback($"Stock issue request '{request.RequestId}' rejected. Reserved stock released.", "Warning");
                await RefreshAllAsync();
            }
        }

        public void CloseAllModals()
        {
            IsProductFormOpen = false;
            CurrentProductForm = null;

            IsReceiveFormOpen = false;
            CurrentReceiveForm = null;

            IsIssueFormOpen = false;
            CurrentIssueForm = null;

            IsDetailsOpen = false;
            CurrentProductDetails = null;
        }

        private void ShowFeedback(string message, string severity)
        {
            FeedbackMessage = message;
            FeedbackSeverity = severity;
            IsFeedbackVisible = true;
        }
    }
}

