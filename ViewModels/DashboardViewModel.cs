using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly IDashboardService _dashboardService;
        private readonly INavigationService _navigationService;
        private readonly IRealtimeUpdateService _realtimeService;

        private bool _isLoading;
        private string? _errorMessage;
        private string _statusFeedbackMessage = string.Empty;
        private bool _showActionFeedbackDialog;
        private string _actionFeedbackTitle = string.Empty;
        private string _actionFeedbackBody = string.Empty;

        private DashboardSummary _summary = new();
        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;
        private string _rackSearchQuery = string.Empty;
        private StockStatus? _selectedStatusFilter;
        private MovementDateRange _selectedMovementRange = MovementDateRange.AllTime;
        private double _zoomLevel = 1.0;
        private bool _isEditingLayout = false;

        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();
        public ObservableCollection<StockMovement> RecentMovements { get; } = new();
        public ObservableCollection<InventoryAlert> ActiveAlerts { get; } = new();

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                var clamped = Math.Clamp(value, 0.6, 2.0);
                if (SetProperty(ref _zoomLevel, clamped))
                {
                    OnPropertyChanged(nameof(ZoomDisplay));
                }
            }
        }

        public string ZoomDisplay => $"{(int)(_zoomLevel * 100)}%";

        public bool IsEditingLayout
        {
            get => _isEditingLayout;
            set => SetProperty(ref _isEditingLayout, value);
        }

        public DashboardSummary Summary
        {
            get => _summary;
            private set => SetProperty(ref _summary, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool ShowActionFeedbackDialog
        {
            get => _showActionFeedbackDialog;
            set => SetProperty(ref _showActionFeedbackDialog, value);
        }

        public string ActionFeedbackTitle
        {
            get => _actionFeedbackTitle;
            set => SetProperty(ref _actionFeedbackTitle, value);
        }

        public string ActionFeedbackBody
        {
            get => _actionFeedbackBody;
            set => SetProperty(ref _actionFeedbackBody, value);
        }

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    _ = FilterRacksAsync();
                }
            }
        }

        public Rack? SelectedRack
        {
            get => _selectedRack;
            set
            {
                if (_selectedRack != null) _selectedRack.IsSelected = false;
                if (SetProperty(ref _selectedRack, value))
                {
                    if (_selectedRack != null) _selectedRack.IsSelected = true;
                    SelectedSection = value?.SelectedSection ?? value?.Sections.FirstOrDefault();
                }
            }
        }

        public RackSection? SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != null) _selectedSection.IsSelected = false;
                if (SetProperty(ref _selectedSection, value))
                {
                    if (_selectedSection != null) _selectedSection.IsSelected = true;
                }
            }
        }

        public string RackSearchQuery
        {
            get => _rackSearchQuery;
            set
            {
                if (SetProperty(ref _rackSearchQuery, value))
                {
                    _ = FilterRacksAsync();
                }
            }
        }

        public StockStatus? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    _ = FilterRacksAsync();
                }
            }
        }

        public MovementDateRange SelectedMovementRange
        {
            get => _selectedMovementRange;
            set
            {
                if (SetProperty(ref _selectedMovementRange, value))
                {
                    _ = LoadRecentMovementsAsync();
                }
            }
        }

        public bool HasActiveFilters =>
            SelectedWarehouse != null ||
            !string.IsNullOrWhiteSpace(RackSearchQuery) ||
            SelectedStatusFilter.HasValue;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand FilterByStatusCardCommand { get; }
        public ICommand SelectRackCommand { get; }
        public ICommand ToggleRackExpandCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand CloseFeedbackDialogCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand ToggleEditLayoutCommand { get; }

        // Quick Action Commands
        public ICommand ReceiveStockActionCommand { get; }
        public ICommand IssueStockActionCommand { get; }
        public ICommand TransferStockActionCommand { get; }
        public ICommand AddProductActionCommand { get; }
        public ICommand ViewWarehouseActionCommand { get; }
        public ICommand ReviewApprovalsActionCommand { get; }
        public ICommand ViewAllMovementsCommand { get; }
        public ICommand DismissAlertCommand { get; }

        public DashboardViewModel(
            IDashboardService? dashboardService = null,
            INavigationService? navigationService = null,
            IRealtimeUpdateService? realtimeService = null)
        {
            _dashboardService = dashboardService ?? new MockDashboardService();
            _navigationService = navigationService ?? NavigationService.Instance;
            _realtimeService = realtimeService ?? RealtimeUpdateService.Instance;

            RefreshCommand = new AsyncRelayCommand(async () => await LoadDashboardDataAsync());
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            FilterByStatusCardCommand = new RelayCommand<string>(FilterByCardCategory);
            SelectRackCommand = new RelayCommand<Rack>(rack => SelectedRack = rack);
            ToggleRackExpandCommand = new RelayCommand<Rack>(rack =>
            {
                if (rack != null)
                {
                    rack.IsExpanded = !rack.IsExpanded;
                    SelectedRack = rack;
                }
            });
            SelectSectionCommand = new RelayCommand<RackSection>(section => SelectedSection = section);
            CloseFeedbackDialogCommand = new RelayCommand(() => ShowActionFeedbackDialog = false);
            ZoomInCommand = new RelayCommand(() => ZoomLevel += 0.15);
            ZoomOutCommand = new RelayCommand(() => ZoomLevel -= 0.15);
            ResetZoomCommand = new RelayCommand(() => ZoomLevel = 1.0);
            ToggleEditLayoutCommand = new RelayCommand(() => IsEditingLayout = !IsEditingLayout);

            // Quick Actions with Honest Non-Destructive Dialogs
            ReceiveStockActionCommand = new RelayCommand(() =>
                ShowFeedback("Stock Inward Workflow", "Receiving stock requires validated supplier PO and Goods Received Note (GRN) verification. This workflow is scheduled for Step 2: Stock Management. Stock is never directly modified from the dashboard."));

            IssueStockActionCommand = new RelayCommand(() =>
                ShowFeedback("Stock Outward Workflow", "Stock issues require validated work orders or sales dispatch manifests with server-side authorization. This workflow will be available in Step 2: Stock Management."));

            TransferStockActionCommand = new RelayCommand(() =>
                ShowFeedback("Inter-Warehouse Transfer", "Transferring items between racks or warehouses triggers dual-custody audit logs and requires supervisor approval. This workflow will be implemented in Step 2: Stock Movements."));

            AddProductActionCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Products"));

            ViewWarehouseActionCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Warehouse"));

            ReviewApprovalsActionCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Approvals"));

            ViewAllMovementsCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Movements"));

            DismissAlertCommand = new RelayCommand<InventoryAlert>(alert =>
            {
                if (alert != null)
                {
                    ActiveAlerts.Remove(alert);
                }
            });

            // SignalR reactive subscription
            _realtimeService.DashboardRefreshRequested += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(async () => await LoadDashboardDataAsync());
            };

            // Initial load
            _ = LoadDashboardDataAsync();
        }

        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                // 1. Load Summary
                Summary = await _dashboardService.GetDashboardSummaryAsync();

                // 2. Load Warehouses
                var warehouses = await _dashboardService.GetWarehousesAsync();
                Warehouses.Clear();
                foreach (var wh in warehouses)
                {
                    Warehouses.Add(wh);
                }

                // 3. Load Racks
                await FilterRacksAsync();

                // 4. Load Recent Movements
                await LoadRecentMovementsAsync();

                // 5. Load Active Alerts
                var alerts = await _dashboardService.GetActiveAlertsAsync();
                ActiveAlerts.Clear();
                foreach (var alert in alerts)
                {
                    ActiveAlerts.Add(alert);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unable to refresh dashboard telemetry: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterRacksAsync()
        {
            try
            {
                var racks = await _dashboardService.GetRacksAsync(
                    warehouseId: SelectedWarehouse?.Id,
                    searchQuery: RackSearchQuery,
                    statusFilter: SelectedStatusFilter);

                Racks.Clear();
                foreach (var rack in racks)
                {
                    Racks.Add(rack);
                }

                if (SelectedRack == null || !Racks.Contains(SelectedRack))
                {
                    SelectedRack = Racks.FirstOrDefault();
                }
                else
                {
                    SelectedSection = SelectedRack.SelectedSection ?? SelectedRack.Sections.FirstOrDefault();
                }

                OnPropertyChanged(nameof(HasActiveFilters));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to filter warehouse racks: {ex.Message}";
            }
        }

        private async Task LoadRecentMovementsAsync()
        {
            try
            {
                var movements = await _dashboardService.GetRecentMovementsAsync(
                    limit: 10,
                    dateRange: SelectedMovementRange);

                RecentMovements.Clear();
                foreach (var move in movements)
                {
                    RecentMovements.Add(move);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load movement ledger: {ex.Message}";
            }
        }

        private void FilterByCardCategory(string? category)
        {
            switch (category?.ToLowerInvariant())
            {
                case "totalproducts":
                    _navigationService.NavigateTo("Products");
                    break;
                case "totalquantity":
                    ShowFeedback("Inventory Unit Breakdown",
                        $"Total stock comprises multiple non-interchangeable units:\n" +
                        string.Join("\n", Summary.UnitBreakdowns.Select(u => $"• {u.Quantity:N0} {u.Unit}")) +
                        "\n\nEach item is strictly tracked in its native unit to prevent erroneous conversions.");
                    break;
                case "lowstock":
                    SelectedStatusFilter = StockStatus.LowStock;
                    break;
                case "nearexpiry":
                    SelectedStatusFilter = StockStatus.NearExpiry;
                    break;
                case "expired":
                    SelectedStatusFilter = StockStatus.Expired;
                    break;
                case "pendingapprovals":
                    _navigationService.NavigateTo("Approvals");
                    break;
            }
        }

        private void ResetFilters()
        {
            _selectedWarehouse = null;
            OnPropertyChanged(nameof(SelectedWarehouse));
            _rackSearchQuery = string.Empty;
            OnPropertyChanged(nameof(RackSearchQuery));
            _selectedStatusFilter = null;
            OnPropertyChanged(nameof(SelectedStatusFilter));
            _selectedMovementRange = MovementDateRange.AllTime;
            OnPropertyChanged(nameof(SelectedMovementRange));

            _ = FilterRacksAsync();
            _ = LoadRecentMovementsAsync();
        }

        private void ShowFeedback(string title, string body)
        {
            ActionFeedbackTitle = title;
            ActionFeedbackBody = body;
            ShowActionFeedbackDialog = true;
        }

        public void UpdateRackPosition(Rack rack, double newX, double newY)
        {
            if (rack == null) return;
            var snappedX = Math.Max(20, Math.Min(900, Math.Round(newX / 20.0) * 20.0));
            var snappedY = Math.Max(20, Math.Min(600, Math.Round(newY / 20.0) * 20.0));
            rack.CanvasX = snappedX;
            rack.CanvasY = snappedY;
        }
    }
}

