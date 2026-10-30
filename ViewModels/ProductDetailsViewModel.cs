using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class ProductDetailsViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly Action _onClose;
        private readonly Action<Product> _onEdit;
        private readonly Action<Product> _onReceive;
        private readonly Action<Product> _onIssue;

        private Product _product;
        private string _activeTab = "Overview"; // Overview, Locations, Batches, Serials, History
        private bool _isLoading;

        public Product Product
        {
            get => _product;
            private set => SetProperty(ref _product, value);
        }

        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (SetProperty(ref _activeTab, value))
                {
                    OnPropertyChanged(nameof(IsOverviewTabActive));
                    OnPropertyChanged(nameof(IsLocationsTabActive));
                    OnPropertyChanged(nameof(IsBatchesTabActive));
                    OnPropertyChanged(nameof(IsSerialsTabActive));
                    OnPropertyChanged(nameof(IsHistoryTabActive));
                }
            }
        }

        public bool IsOverviewTabActive => ActiveTab == "Overview";
        public bool IsLocationsTabActive => ActiveTab == "Locations";
        public bool IsBatchesTabActive => ActiveTab == "Batches";
        public bool IsSerialsTabActive => ActiveTab == "Serials";
        public bool IsHistoryTabActive => ActiveTab == "History";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<StockLocationBalance> LocationBalances { get; } = new();
        public ObservableCollection<StockBatch> Batches { get; } = new();
        public ObservableCollection<StockSerialNumber> SerialNumbers { get; } = new();
        public ObservableCollection<StockMovement> Movements { get; } = new();

        public ICommand SetTabCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ReceiveCommand { get; }
        public ICommand IssueCommand { get; }
        public ICommand RefreshCommand { get; }

        public ProductDetailsViewModel(
            Product product,
            IInventoryService inventoryService,
            Action onClose,
            Action<Product> onEdit,
            Action<Product> onReceive,
            Action<Product> onIssue)
        {
            _product = product;
            _inventoryService = inventoryService;
            _onClose = onClose;
            _onEdit = onEdit;
            _onReceive = onReceive;
            _onIssue = onIssue;

            SetTabCommand = new RelayCommand<string>(tab => { if (!string.IsNullOrEmpty(tab)) ActiveTab = tab; });
            CloseCommand = new RelayCommand(() => _onClose());
            EditCommand = new RelayCommand(() => _onEdit(Product));
            ReceiveCommand = new RelayCommand(() => _onReceive(Product));
            IssueCommand = new RelayCommand(() => _onIssue(Product));
            RefreshCommand = new AsyncRelayCommand(LoadProductDetailsAsync);

            _ = LoadProductDetailsAsync();
        }

        public async Task LoadProductDetailsAsync()
        {
            IsLoading = true;
            try
            {
                // Refresh latest product state
                var latest = await _inventoryService.GetProductByIdAsync(_product.Id);
                if (latest != null) Product = latest;

                // Load Locations
                var locs = await _inventoryService.GetLocationBalancesForProductAsync(Product.Id);
                LocationBalances.Clear();
                foreach (var l in locs) LocationBalances.Add(l);

                // Load Batches if tracked
                if (Product.IsBatchTracked)
                {
                    var batches = await _inventoryService.GetBatchesForProductAsync(Product.Id);
                    Batches.Clear();
                    foreach (var b in batches) Batches.Add(b);
                }

                // Load Serials if tracked
                if (Product.IsSerialTracked)
                {
                    var serials = await _inventoryService.GetSerialNumbersForProductAsync(Product.Id);
                    SerialNumbers.Clear();
                    foreach (var s in serials) SerialNumbers.Add(s);
                }

                // Load Movements
                var moves = await _inventoryService.GetMovementsForProductAsync(Product.Id);
                Movements.Clear();
                foreach (var m in moves) Movements.Add(m);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
