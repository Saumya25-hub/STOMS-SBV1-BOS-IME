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
    public class LayoutStateSnapshot
    {
        public List<(string RackId, double X, double Y, double Width, double Height, double Rotation)> RackPositions { get; set; } = new();
    }

    public class WarehouseRackViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDashboardService _dashboardService;
        private readonly INavigationService _navigationService;

        // Active Navigation & Studio Tab
        private string _activeStudioTab = "FloorPlan"; // "FloorPlan", "Warehouses", "Hierarchy", "StockInspector"
        private bool _isEditLayoutMode = false;
        private bool _hasUnsavedLayoutChanges = false;

        // Selection State
        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;
        private StorageLocation? _selectedHierarchyLocation;
        private StoredProductItem? _selectedStoredItem;

        // Canvas & Studio Controls
        private double _zoomLevel = 1.0;
        private bool _isGridVisible = true;
        private bool _isSnapToGrid = true;
        private string _searchQuery = string.Empty;
        private string _statusFilter = "All"; // All, Optimal, LowStock, NearExpiry, Expired
        private string _occupancyFilter = "All"; // All, Normal, High, Full, OverCapacity
        private string _feedbackMessage = string.Empty;
        private bool _isFeedbackSuccess = true;

        // Modal Form Open Flags
        private bool _isAddWarehouseOpen;
        private bool _isEditWarehouseOpen;
        private bool _isAddRackOpen;
        private bool _isEditRackOpen;
        private bool _isAddSectionOpen;
        private bool _isRelocationOpen;
        private bool _isSuggestionOpen;

        // Form Binding Models
        private Warehouse _warehouseForm = new();
        private Rack _rackForm = new();
        private RackSection _sectionForm = new();
        private StockRelocationDto _relocationForm = new();
        private LocationSuggestionRequest _suggestionRequest = new();

        // Undo / Redo Stacks for Layout
        private readonly Stack<LayoutStateSnapshot> _undoStack = new();
        private readonly Stack<LayoutStateSnapshot> _redoStack = new();

        // Collections
        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();
        public ObservableCollection<RackSection> Sections { get; } = new();
        public ObservableCollection<StoredProductItem> LocationInventory { get; } = new();
        public ObservableCollection<LocationSuggestion> LocationSuggestions { get; } = new();
        public ObservableCollection<StorageLocation> HierarchyTree { get; } = new();

        // Summary Properties
        public decimal TotalPhysicalStockInSelection => LocationInventory.Sum(i => i.Quantity);
        public int TotalSkusInSelection => LocationInventory.Select(i => i.ProductId).Distinct().Count();
        public decimal TotalAvailableStockInSelection => LocationInventory.Where(i => i.Status != StockStatus.Expired && i.Status != StockStatus.Damaged).Sum(i => i.Quantity);
        public decimal TotalExpiredStockInSelection => LocationInventory.Where(i => i.Status == StockStatus.Expired).Sum(i => i.Quantity);
        public decimal TotalDamagedStockInSelection => LocationInventory.Where(i => i.Status == StockStatus.Damaged).Sum(i => i.Quantity);

        // Properties
        public string ActiveStudioTab
        {
            get => _activeStudioTab;
            set
            {
                if (SetProperty(ref _activeStudioTab, value))
                {
                    OnPropertyChanged(nameof(IsFloorPlanActive));
                    OnPropertyChanged(nameof(IsWarehousesActive));
                    OnPropertyChanged(nameof(IsHierarchyActive));
                    OnPropertyChanged(nameof(IsStockInspectorActive));
                }
            }
        }

        public bool IsFloorPlanActive => ActiveStudioTab == "FloorPlan";
        public bool IsWarehousesActive => ActiveStudioTab == "Warehouses";
        public bool IsHierarchyActive => ActiveStudioTab == "Hierarchy";
        public bool IsStockInspectorActive => ActiveStudioTab == "StockInspector";

        public bool IsEditLayoutMode
        {
            get => _isEditLayoutMode;
            set
            {
                if (SetProperty(ref _isEditLayoutMode, value))
                {
                    OnPropertyChanged(nameof(EditModeBadgeText));
                }
            }
        }

        public string EditModeBadgeText => IsEditLayoutMode ? "Layout Editing Active" : "View & Inspection Mode";

        public bool HasUnsavedLayoutChanges
        {
            get => _hasUnsavedLayoutChanges;
            set => SetProperty(ref _hasUnsavedLayoutChanges, value);
        }

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    _ = LoadWarehouseTopologyAsync();
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
                    OnPropertyChanged(nameof(HasSelectedRack));
                    SelectedSection = value?.Sections.FirstOrDefault();
                    _ = RefreshLocationInventoryAsync();
                }
            }
        }

        public bool HasSelectedRack => SelectedRack != null;

        public RackSection? SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != null) _selectedSection.IsSelected = false;
                if (SetProperty(ref _selectedSection, value))
                {
                    if (_selectedSection != null) _selectedSection.IsSelected = true;
                    OnPropertyChanged(nameof(HasSelectedSection));
                    _ = RefreshLocationInventoryAsync();
                }
            }
        }

        public bool HasSelectedSection => SelectedSection != null;

        public StorageLocation? SelectedHierarchyLocation
        {
            get => _selectedHierarchyLocation;
            set
            {
                if (SetProperty(ref _selectedHierarchyLocation, value))
                {
                    _ = RefreshLocationInventoryAsync();
                }
            }
        }

        public StoredProductItem? SelectedStoredItem
        {
            get => _selectedStoredItem;
            set => SetProperty(ref _selectedStoredItem, value);
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (SetProperty(ref _zoomLevel, Math.Clamp(value, 0.4, 2.5)))
                {
                    OnPropertyChanged(nameof(ZoomDisplay));
                }
            }
        }

        public string ZoomDisplay => $"{(int)(_zoomLevel * 100)}%";

        public bool IsGridVisible
        {
            get => _isGridVisible;
            set => SetProperty(ref _isGridVisible, value);
        }

        public bool IsSnapToGrid
        {
            get => _isSnapToGrid;
            set => SetProperty(ref _isSnapToGrid, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = FilterTopologyAsync();
                }
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    _ = FilterTopologyAsync();
                }
            }
        }

        public string OccupancyFilter
        {
            get => _occupancyFilter;
            set
            {
                if (SetProperty(ref _occupancyFilter, value))
                {
                    _ = FilterTopologyAsync();
                }
            }
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public bool IsFeedbackSuccess
        {
            get => _isFeedbackSuccess;
            set => SetProperty(ref _isFeedbackSuccess, value);
        }

        // Modals
        public bool IsAddWarehouseOpen
        {
            get => _isAddWarehouseOpen;
            set => SetProperty(ref _isAddWarehouseOpen, value);
        }

        public bool IsEditWarehouseOpen
        {
            get => _isEditWarehouseOpen;
            set => SetProperty(ref _isEditWarehouseOpen, value);
        }

        public bool IsAddRackOpen
        {
            get => _isAddRackOpen;
            set => SetProperty(ref _isAddRackOpen, value);
        }

        public bool IsEditRackOpen
        {
            get => _isEditRackOpen;
            set => SetProperty(ref _isEditRackOpen, value);
        }

        public bool IsAddSectionOpen
        {
            get => _isAddSectionOpen;
            set => SetProperty(ref _isAddSectionOpen, value);
        }

        public bool IsRelocationOpen
        {
            get => _isRelocationOpen;
            set => SetProperty(ref _isRelocationOpen, value);
        }

        public bool IsSuggestionOpen
        {
            get => _isSuggestionOpen;
            set => SetProperty(ref _isSuggestionOpen, value);
        }

        // Form Objects
        public Warehouse WarehouseForm
        {
            get => _warehouseForm;
            set => SetProperty(ref _warehouseForm, value);
        }

        public Rack RackForm
        {
            get => _rackForm;
            set => SetProperty(ref _rackForm, value);
        }

        public RackSection SectionForm
        {
            get => _sectionForm;
            set => SetProperty(ref _sectionForm, value);
        }

        public StockRelocationDto RelocationForm
        {
            get => _relocationForm;
            set => SetProperty(ref _relocationForm, value);
        }

        public LocationSuggestionRequest SuggestionRequest
        {
            get => _suggestionRequest;
            set => SetProperty(ref _suggestionRequest, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SetStudioTabCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand SaveLayoutCommand { get; }
        public ICommand CancelLayoutEditsCommand { get; }
        public ICommand UndoLayoutCommand { get; }
        public ICommand RedoLayoutCommand { get; }

        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand FitToScreenCommand { get; }

        public ICommand SelectRackCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand MoveRackVisualCommand { get; }
        public ICommand RotateRackCommand { get; }
        public ICommand ResizeRackCommand { get; }
        public ICommand DuplicateRackCommand { get; }
        public ICommand DeleteRackCommand { get; }

        // Warehouse Master Commands
        public ICommand OpenAddWarehouseCommand { get; }
        public ICommand OpenEditWarehouseCommand { get; }
        public ICommand SaveWarehouseCommand { get; }
        public ICommand ToggleWarehouseStatusCommand { get; }
        public ICommand DeleteWarehouseCommand { get; }

        // Rack Master Commands
        public ICommand OpenAddRackCommand { get; }
        public ICommand OpenEditRackCommand { get; }
        public ICommand SaveRackCommand { get; }

        // Section Master Commands
        public ICommand OpenAddSectionCommand { get; }
        public ICommand SaveSectionCommand { get; }
        public ICommand DeleteSectionCommand { get; }

        // Relocation & Suggestions
        public ICommand OpenRelocationCommand { get; }
        public ICommand ExecuteRelocationCommand { get; }
        public ICommand OpenSuggestionCommand { get; }
        public ICommand ExecuteSuggestionQueryCommand { get; }
        public ICommand ApplySuggestionCommand { get; }
        public ICommand NavigateToProductDetailsCommand { get; }

        // Modals & Feedback Commands
        public ICommand ClearFeedbackCommand { get; }

        public WarehouseRackViewModel(
            IDashboardService? dashboardService = null,
            IInventoryService? inventoryService = null,
            INavigationService? navigationService = null)
        {
            _dashboardService = dashboardService ?? new MockDashboardService();
            _inventoryService = inventoryService ?? MockInventoryService.Instance;
            _navigationService = navigationService ?? NavigationService.Instance;

            RefreshCommand = new AsyncRelayCommand(async () => await InitializeAsync());
            SetStudioTabCommand = new RelayCommand<string>(tab => { if (tab != null) ActiveStudioTab = tab; });
            ToggleEditModeCommand = new RelayCommand(() => IsEditLayoutMode = !IsEditLayoutMode);
            SaveLayoutCommand = new AsyncRelayCommand(SaveLayoutAsync);
            CancelLayoutEditsCommand = new AsyncRelayCommand(CancelLayoutEditsAsync);
            UndoLayoutCommand = new RelayCommand(UndoLayout);
            RedoLayoutCommand = new RelayCommand(RedoLayout);

            ZoomInCommand = new RelayCommand(() => ZoomLevel += 0.15);
            ZoomOutCommand = new RelayCommand(() => ZoomLevel -= 0.15);
            ResetZoomCommand = new RelayCommand(() => ZoomLevel = 1.0);
            FitToScreenCommand = new RelayCommand(FitToScreen);

            SelectRackCommand = new RelayCommand<Rack>(r => SelectedRack = r);
            SelectSectionCommand = new RelayCommand<RackSection>(s => SelectedSection = s);
            MoveRackVisualCommand = new RelayCommand<(Rack rack, double dx, double dy)>(tuple => MoveRack(tuple.rack, tuple.dx, tuple.dy));
            RotateRackCommand = new RelayCommand<Rack>(RotateRack);
            ResizeRackCommand = new RelayCommand<(Rack rack, double dw, double dh)>(tuple => ResizeRack(tuple.rack, tuple.dw, tuple.dh));
            DuplicateRackCommand = new AsyncRelayCommand(async () => await DuplicateSelectedRackAsync());
            DeleteRackCommand = new AsyncRelayCommand(async () => await DeleteSelectedRackAsync());

            OpenAddWarehouseCommand = new RelayCommand(PrepareAddWarehouse);
            OpenEditWarehouseCommand = new RelayCommand(PrepareEditWarehouse);
            SaveWarehouseCommand = new AsyncRelayCommand(SaveWarehouseAsync);
            ToggleWarehouseStatusCommand = new AsyncRelayCommand(async () => await ToggleSelectedWarehouseStatusAsync());
            DeleteWarehouseCommand = new AsyncRelayCommand(async () => await DeleteSelectedWarehouseAsync());

            OpenAddRackCommand = new RelayCommand(PrepareAddRack);
            OpenEditRackCommand = new RelayCommand(PrepareEditRack);
            SaveRackCommand = new AsyncRelayCommand(SaveRackAsync);

            OpenAddSectionCommand = new RelayCommand(PrepareAddSection);
            SaveSectionCommand = new AsyncRelayCommand(SaveSectionAsync);
            DeleteSectionCommand = new AsyncRelayCommand(async () => await DeleteSelectedSectionAsync());

            OpenRelocationCommand = new RelayCommand(PrepareRelocation);
            ExecuteRelocationCommand = new AsyncRelayCommand(ExecuteRelocationAsync);
            OpenSuggestionCommand = new RelayCommand(PrepareSuggestion);
            ExecuteSuggestionQueryCommand = new AsyncRelayCommand(ExecuteSuggestionQueryAsync);
            ApplySuggestionCommand = new RelayCommand<LocationSuggestion>(ApplySuggestion);
            NavigateToProductDetailsCommand = new RelayCommand<StoredProductItem>(NavigateToProductDetails);

            ClearFeedbackCommand = new RelayCommand(() =>
            {
                FeedbackMessage = string.Empty;
                IsAddWarehouseOpen = false;
                IsEditWarehouseOpen = false;
                IsAddRackOpen = false;
                IsEditRackOpen = false;
                IsAddSectionOpen = false;
                IsRelocationOpen = false;
                IsSuggestionOpen = false;
            });

            _inventoryService.InventoryChanged += (s, e) =>
            {
                if (App.Current != null && App.Current.Dispatcher != null)
                {
                    App.Current.Dispatcher.Invoke(async () => await LoadWarehouseTopologyAsync());
                }
                else
                {
                    _ = LoadWarehouseTopologyAsync();
                }
            };

            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            var warehouses = await _inventoryService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var wh in warehouses)
            {
                Warehouses.Add(wh);
            }

            if (SelectedWarehouse == null || !Warehouses.Any(w => w.Id == SelectedWarehouse.Id))
            {
                SelectedWarehouse = Warehouses.FirstOrDefault();
            }
            else
            {
                await LoadWarehouseTopologyAsync();
            }
        }

        public async Task LoadWarehouseTopologyAsync()
        {
            if (SelectedWarehouse == null) return;

            var racks = await _inventoryService.GetRacksAsync(SelectedWarehouse.Id, SearchQuery);
            Racks.Clear();
            foreach (var r in racks)
            {
                Racks.Add(r);
            }

            if (SelectedRack == null || !Racks.Any(r => r.Id == SelectedRack.Id))
            {
                SelectedRack = Racks.FirstOrDefault();
            }
            else
            {
                SelectedRack = Racks.First(r => r.Id == SelectedRack.Id);
            }

            BuildHierarchyTree();
            await RefreshLocationInventoryAsync();
            _undoStack.Clear();
            _redoStack.Clear();
            HasUnsavedLayoutChanges = false;
        }

        private async Task FilterTopologyAsync()
        {
            if (SelectedWarehouse == null) return;
            var allRacks = await _inventoryService.GetRacksAsync(SelectedWarehouse.Id);

            var filtered = allRacks.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim();
                filtered = filtered.Where(r => r.RackCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                               r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                               r.Zone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                               r.Sections.Any(s => s.SectionCode.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            if (StatusFilter != "All")
            {
                if (Enum.TryParse<StockStatus>(StatusFilter, out var st))
                {
                    filtered = filtered.Where(r => r.OverallStatus == st);
                }
            }

            if (OccupancyFilter != "All")
            {
                filtered = OccupancyFilter switch
                {
                    "Normal" => filtered.Where(r => r.OccupancyPercentage < 70),
                    "High" => filtered.Where(r => r.OccupancyPercentage >= 70 && r.OccupancyPercentage < 95),
                    "Full" => filtered.Where(r => r.OccupancyPercentage >= 95 && r.OccupancyPercentage <= 100),
                    "OverCapacity" => filtered.Where(r => r.OccupancyPercentage > 100),
                    _ => filtered
                };
            }

            Racks.Clear();
            foreach (var r in filtered)
            {
                Racks.Add(r);
            }

            if (SelectedRack == null || !Racks.Contains(SelectedRack))
            {
                SelectedRack = Racks.FirstOrDefault();
            }
        }

        private void BuildHierarchyTree()
        {
            HierarchyTree.Clear();
            if (SelectedWarehouse == null) return;

            var whNode = new StorageLocation
            {
                LocationId = SelectedWarehouse.Id,
                LocationCode = SelectedWarehouse.Code,
                Name = SelectedWarehouse.Name,
                WarehouseId = SelectedWarehouse.Id,
                WarehouseName = SelectedWarehouse.Name,
                LocationType = StorageLocationType.Warehouse,
                MaxCapacity = SelectedWarehouse.TotalCapacity,
                OccupiedCapacity = SelectedWarehouse.OccupiedCapacity,
                CapacityUnit = SelectedWarehouse.Unit,
                IsActive = SelectedWarehouse.IsActive
            };

            foreach (var rack in Racks)
            {
                var rackNode = new StorageLocation
                {
                    LocationId = rack.Id,
                    LocationCode = rack.RackCode,
                    Name = rack.Name,
                    WarehouseId = SelectedWarehouse.Id,
                    WarehouseName = SelectedWarehouse.Name,
                    RackId = rack.Id,
                    RackCode = rack.RackCode,
                    LocationType = StorageLocationType.Rack,
                    MaxCapacity = rack.MaxCapacity,
                    OccupiedCapacity = rack.OccupiedCapacity,
                    CapacityUnit = rack.CapacityUnit,
                    IsActive = rack.IsActive
                };

                foreach (var sec in rack.Sections)
                {
                    var secNode = new StorageLocation
                    {
                        LocationId = sec.Id,
                        LocationCode = sec.SectionCode,
                        Name = sec.Name,
                        WarehouseId = SelectedWarehouse.Id,
                        WarehouseName = SelectedWarehouse.Name,
                        RackId = rack.Id,
                        RackCode = rack.RackCode,
                        ParentLocationId = rack.Id,
                        LocationType = StorageLocationType.Section,
                        MaxCapacity = sec.MaxCapacity,
                        OccupiedCapacity = sec.OccupiedCapacity,
                        CapacityUnit = sec.CapacityUnit,
                        LevelNumber = sec.TierLevel,
                        IsActive = sec.IsActive,
                        StoredItems = sec.Products.ToList()
                    };

                    foreach (var comp in sec.Compartments)
                    {
                        var compNode = new StorageLocation
                        {
                            LocationId = comp.Id,
                            LocationCode = comp.BinCode,
                            Name = comp.Name,
                            WarehouseId = SelectedWarehouse.Id,
                            WarehouseName = SelectedWarehouse.Name,
                            RackId = rack.Id,
                            RackCode = rack.RackCode,
                            ParentLocationId = sec.Id,
                            LocationType = StorageLocationType.Compartment,
                            MaxCapacity = comp.Capacity,
                            OccupiedCapacity = comp.Occupied,
                            CapacityUnit = comp.CapacityUnit,
                            IsActive = comp.IsActive,
                            StoredItems = comp.Items.ToList()
                        };
                        secNode.Children.Add(compNode);
                    }

                    rackNode.Children.Add(secNode);
                }

                whNode.Children.Add(rackNode);
            }

            HierarchyTree.Add(whNode);
        }

        private async Task RefreshLocationInventoryAsync()
        {
            LocationInventory.Clear();
            if (SelectedWarehouse == null) return;

            var items = await _inventoryService.GetInventoryByLocationAsync(
                SelectedWarehouse.Id,
                SelectedRack?.RackCode,
                SelectedSection?.SectionCode);

            foreach (var it in items)
            {
                LocationInventory.Add(it);
            }

            OnPropertyChanged(nameof(TotalPhysicalStockInSelection));
            OnPropertyChanged(nameof(TotalSkusInSelection));
            OnPropertyChanged(nameof(TotalAvailableStockInSelection));
            OnPropertyChanged(nameof(TotalExpiredStockInSelection));
            OnPropertyChanged(nameof(TotalDamagedStockInSelection));
        }

        // Layout Canvas Manipulation
        private void CaptureLayoutSnapshot()
        {
            var snap = new LayoutStateSnapshot
            {
                RackPositions = Racks.Select(r => (r.Id, r.CanvasX, r.CanvasY, r.Width, r.Height, r.Rotation)).ToList()
            };
            _undoStack.Push(snap);
            _redoStack.Clear();
            HasUnsavedLayoutChanges = true;
        }

        public void MoveRack(Rack? rack, double newX, double newY)
        {
            if (rack == null || !IsEditLayoutMode) return;

            CaptureLayoutSnapshot();

            if (IsSnapToGrid)
            {
                newX = Math.Round(newX / 20.0) * 20.0;
                newY = Math.Round(newY / 20.0) * 20.0;
            }

            rack.CanvasX = Math.Max(0, newX);
            rack.CanvasY = Math.Max(0, newY);
            HasUnsavedLayoutChanges = true;
        }

        public void RotateRack(Rack? rack)
        {
            if (rack == null || !IsEditLayoutMode) return;

            CaptureLayoutSnapshot();
            rack.Rotation = (rack.Rotation + 90) % 360;
            HasUnsavedLayoutChanges = true;
            ShowFeedback($"Rack {rack.RackCode} rotated to {rack.Rotation}°", true);
        }

        public void ResizeRack(Rack? rack, double dw, double dh)
        {
            if (rack == null || !IsEditLayoutMode) return;

            CaptureLayoutSnapshot();
            rack.Width = Math.Clamp(rack.Width + dw, 140, 500);
            rack.Height = Math.Clamp(rack.Height + dh, 100, 400);
            HasUnsavedLayoutChanges = true;
        }

        public void UndoLayout()
        {
            if (_undoStack.Count == 0) return;

            var current = new LayoutStateSnapshot
            {
                RackPositions = Racks.Select(r => (r.Id, r.CanvasX, r.CanvasY, r.Width, r.Height, r.Rotation)).ToList()
            };
            _redoStack.Push(current);

            var previous = _undoStack.Pop();
            foreach (var item in previous.RackPositions)
            {
                var r = Racks.FirstOrDefault(rk => rk.Id == item.RackId);
                if (r != null)
                {
                    r.CanvasX = item.X;
                    r.CanvasY = item.Y;
                    r.Width = item.Width;
                    r.Height = item.Height;
                    r.Rotation = item.Rotation;
                }
            }
            HasUnsavedLayoutChanges = true;
            ShowFeedback("Layout action undone.", true);
        }

        public void RedoLayout()
        {
            if (_redoStack.Count == 0) return;

            var current = new LayoutStateSnapshot
            {
                RackPositions = Racks.Select(r => (r.Id, r.CanvasX, r.CanvasY, r.Width, r.Height, r.Rotation)).ToList()
            };
            _undoStack.Push(current);

            var next = _redoStack.Pop();
            foreach (var item in next.RackPositions)
            {
                var r = Racks.FirstOrDefault(rk => rk.Id == item.RackId);
                if (r != null)
                {
                    r.CanvasX = item.X;
                    r.CanvasY = item.Y;
                    r.Width = item.Width;
                    r.Height = item.Height;
                    r.Rotation = item.Rotation;
                }
            }
            HasUnsavedLayoutChanges = true;
            ShowFeedback("Layout action redone.", true);
        }

        private async Task SaveLayoutAsync()
        {
            if (SelectedWarehouse == null) return;

            try
            {
                await _inventoryService.SaveRackLayoutCoordinatesAsync(SelectedWarehouse.Id, Racks.ToList());
                HasUnsavedLayoutChanges = false;
                IsEditLayoutMode = false;
                ShowFeedback($"Warehouse layout revision #{SelectedWarehouse.LayoutRevision} successfully persisted.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback($"Failed to save layout: {ex.Message}", false);
            }
        }

        private async Task CancelLayoutEditsAsync()
        {
            await LoadWarehouseTopologyAsync();
            IsEditLayoutMode = false;
            HasUnsavedLayoutChanges = false;
            ShowFeedback("Layout changes cancelled. Reverted to last saved layout.", true);
        }

        private void FitToScreen()
        {
            ZoomLevel = 1.0;
        }

        // Warehouse CRUD
        private void PrepareAddWarehouse()
        {
            WarehouseForm = new Warehouse
            {
                Code = $"WH-0{Warehouses.Count + 1}",
                Name = "New Industrial Logistics Hub",
                CityLocation = "Gujarat Industrial Corridor",
                WarehouseType = "Main Distribution Center",
                TotalFloorArea = 5000,
                TotalCapacity = 1000,
                Unit = "Tons",
                IsActive = true
            };
            IsAddWarehouseOpen = true;
        }

        private void PrepareEditWarehouse()
        {
            if (SelectedWarehouse == null) return;
            WarehouseForm = new Warehouse
            {
                Id = SelectedWarehouse.Id,
                Code = SelectedWarehouse.Code,
                Name = SelectedWarehouse.Name,
                Description = SelectedWarehouse.Description,
                Address = SelectedWarehouse.Address,
                CityLocation = SelectedWarehouse.CityLocation,
                WarehouseType = SelectedWarehouse.WarehouseType,
                TotalFloorArea = SelectedWarehouse.TotalFloorArea,
                TotalCapacity = SelectedWarehouse.TotalCapacity,
                OccupiedCapacity = SelectedWarehouse.OccupiedCapacity,
                Unit = SelectedWarehouse.Unit,
                IsActive = SelectedWarehouse.IsActive,
                Notes = SelectedWarehouse.Notes
            };
            IsEditWarehouseOpen = true;
        }

        private async Task SaveWarehouseAsync()
        {
            try
            {
                if (IsAddWarehouseOpen)
                {
                    var created = await _inventoryService.CreateWarehouseAsync(WarehouseForm);
                    Warehouses.Add(created);
                    SelectedWarehouse = created;
                    IsAddWarehouseOpen = false;
                    ShowFeedback($"Warehouse '{created.Name}' created successfully.", true);
                }
                else if (IsEditWarehouseOpen)
                {
                    var updated = await _inventoryService.UpdateWarehouseAsync(WarehouseForm);
                    var idx = Warehouses.ToList().FindIndex(w => w.Id == updated.Id);
                    if (idx >= 0) Warehouses[idx] = updated;
                    SelectedWarehouse = updated;
                    IsEditWarehouseOpen = false;
                    ShowFeedback($"Warehouse '{updated.Name}' updated successfully.", true);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        private async Task ToggleSelectedWarehouseStatusAsync()
        {
            if (SelectedWarehouse == null) return;
            try
            {
                bool newStatus = !SelectedWarehouse.IsActive;
                await _inventoryService.ToggleWarehouseStatusAsync(SelectedWarehouse.Id, newStatus);
                SelectedWarehouse.IsActive = newStatus;
                ShowFeedback($"Warehouse status changed to {(newStatus ? "Active" : "Deactivated")}.", true);
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        private async Task DeleteSelectedWarehouseAsync()
        {
            if (SelectedWarehouse == null) return;
            try
            {
                await _inventoryService.DeleteWarehouseAsync(SelectedWarehouse.Id);
                var toRemove = SelectedWarehouse;
                Warehouses.Remove(toRemove);
                SelectedWarehouse = Warehouses.FirstOrDefault();
                ShowFeedback($"Warehouse '{toRemove.Name}' deleted successfully.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        // Rack CRUD
        private void PrepareAddRack()
        {
            if (SelectedWarehouse == null) return;
            var letter = char.ConvertFromUtf32(65 + (Racks.Count % 26));
            RackForm = new Rack
            {
                RackCode = $"RCK-{letter}-{Racks.Count + 1:D2}",
                Name = $"High-Bay Heavy Rack {letter}",
                WarehouseId = SelectedWarehouse.Id,
                WarehouseName = SelectedWarehouse.Name,
                Zone = $"Zone {letter} - Pallet Bay",
                RackType = "Selective Pallet Rack",
                MaxCapacity = 1200,
                CapacityUnit = "kg",
                CanvasX = 40 + (Racks.Count * 260) % 780,
                CanvasY = 40 + (Racks.Count / 3) * 200,
                Width = 250,
                Height = 150,
                NumberOfLevels = 4,
                IsActive = true
            };
            IsAddRackOpen = true;
        }

        private void PrepareEditRack()
        {
            if (SelectedRack == null) return;
            RackForm = new Rack
            {
                Id = SelectedRack.Id,
                RackCode = SelectedRack.RackCode,
                Name = SelectedRack.Name,
                WarehouseId = SelectedRack.WarehouseId,
                WarehouseName = SelectedRack.WarehouseName,
                Zone = SelectedRack.Zone,
                RackType = SelectedRack.RackType,
                MaxCapacity = SelectedRack.MaxCapacity,
                OccupiedCapacity = SelectedRack.OccupiedCapacity,
                CapacityUnit = SelectedRack.CapacityUnit,
                NumberOfLevels = SelectedRack.NumberOfLevels,
                CanvasX = SelectedRack.CanvasX,
                CanvasY = SelectedRack.CanvasY,
                Width = SelectedRack.Width,
                Height = SelectedRack.Height,
                Rotation = SelectedRack.Rotation,
                IsActive = SelectedRack.IsActive
            };
            IsEditRackOpen = true;
        }

        private async Task SaveRackAsync()
        {
            try
            {
                if (IsAddRackOpen)
                {
                    // Add standard section levels
                    for (int i = 1; i <= RackForm.NumberOfLevels; i++)
                    {
                        RackForm.Sections.Add(new RackSection
                        {
                            Id = $"SEC-{Guid.NewGuid():N}"[..8],
                            RackId = RackForm.Id,
                            RackCode = RackForm.RackCode,
                            SectionCode = $"{RackForm.RackCode}-T{i}",
                            Name = $"Tier {i}: Heavy Pallet Slot",
                            TierLevel = i,
                            MaxCapacity = (RackForm.MaxCapacity ?? 1000m) / RackForm.NumberOfLevels,
                            CapacityUnit = RackForm.CapacityUnit,
                            IsActive = true
                        });
                    }

                    var created = await _inventoryService.CreateRackAsync(RackForm);
                    Racks.Add(created);
                    SelectedRack = created;
                    IsAddRackOpen = false;
                    BuildHierarchyTree();
                    ShowFeedback($"Rack '{created.RackCode}' added to warehouse.", true);
                }
                else if (IsEditRackOpen)
                {
                    var updated = await _inventoryService.UpdateRackAsync(RackForm);
                    var idx = Racks.ToList().FindIndex(r => r.Id == updated.Id);
                    if (idx >= 0) Racks[idx] = updated;
                    SelectedRack = updated;
                    IsEditRackOpen = false;
                    BuildHierarchyTree();
                    ShowFeedback($"Rack '{updated.RackCode}' updated.", true);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        private async Task DuplicateSelectedRackAsync()
        {
            if (SelectedRack == null) return;
            try
            {
                var newCode = $"{SelectedRack.RackCode}-CPY";
                var dup = await _inventoryService.DuplicateRackAsync(SelectedRack.Id, newCode);
                Racks.Add(dup);
                SelectedRack = dup;
                BuildHierarchyTree();
                ShowFeedback($"Rack cloned as '{dup.RackCode}'.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        private async Task DeleteSelectedRackAsync()
        {
            if (SelectedRack == null) return;
            try
            {
                await _inventoryService.DeleteRackAsync(SelectedRack.Id);
                var toRemove = SelectedRack;
                Racks.Remove(toRemove);
                SelectedRack = Racks.FirstOrDefault();
                BuildHierarchyTree();
                ShowFeedback($"Rack '{toRemove.RackCode}' successfully deleted.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        // Section Master
        private void PrepareAddSection()
        {
            if (SelectedRack == null) return;
            int nextTier = SelectedRack.Sections.Count + 1;
            SectionForm = new RackSection
            {
                RackId = SelectedRack.Id,
                RackCode = SelectedRack.RackCode,
                SectionCode = $"{SelectedRack.RackCode}-T{nextTier}",
                Name = $"Tier {nextTier}: Standard Storage Section",
                TierLevel = nextTier,
                MaxCapacity = 300,
                CapacityUnit = SelectedRack.CapacityUnit,
                IsActive = true
            };
            IsAddSectionOpen = true;
        }

        private async Task SaveSectionAsync()
        {
            if (SelectedRack == null) return;
            try
            {
                var created = await _inventoryService.CreateSectionAsync(SectionForm);
                SelectedRack.Sections.Add(created);
                SelectedSection = created;
                IsAddSectionOpen = false;
                BuildHierarchyTree();
                ShowFeedback($"Section '{created.SectionCode}' created.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        private async Task DeleteSelectedSectionAsync()
        {
            if (SelectedRack == null || SelectedSection == null) return;
            try
            {
                await _inventoryService.DeleteSectionAsync(SelectedRack.Id, SelectedSection.Id);
                SelectedRack.Sections.Remove(SelectedSection);
                SelectedSection = SelectedRack.Sections.FirstOrDefault();
                BuildHierarchyTree();
                ShowFeedback("Section successfully deleted.", true);
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        // Stock Relocation
        private void PrepareRelocation()
        {
            if (SelectedStoredItem != null)
            {
                RelocationForm = new StockRelocationDto
                {
                    ProductId = SelectedStoredItem.ProductId,
                    ProductName = SelectedStoredItem.Name,
                    Sku = SelectedStoredItem.Sku,
                    Quantity = SelectedStoredItem.Quantity,
                    Unit = SelectedStoredItem.Unit,
                    BatchNumber = SelectedStoredItem.BatchNumber,
                    SourceWarehouseId = SelectedWarehouse?.Id ?? "WH-01",
                    SourceRackCode = SelectedRack?.RackCode ?? "RCK-A-01",
                    SourceLocationCode = SelectedSection?.SectionCode ?? "A1-T1",
                    DestinationWarehouseId = SelectedWarehouse?.Id ?? "WH-01",
                    DestinationRackCode = SelectedRack?.RackCode ?? "RCK-A-01",
                    DestinationLocationCode = SelectedRack?.Sections.LastOrDefault()?.SectionCode ?? "A1-T2",
                    Reason = "Warehouse Slotting Optimization",
                    Reference = $"REL-MANUAL-{DateTime.Now:yyyyMMdd}"
                };
            }
            else
            {
                RelocationForm = new StockRelocationDto
                {
                    SourceWarehouseId = SelectedWarehouse?.Id ?? "WH-01",
                    SourceRackCode = SelectedRack?.RackCode ?? "RCK-A-01",
                    SourceLocationCode = SelectedSection?.SectionCode ?? "A1-T1",
                    DestinationWarehouseId = SelectedWarehouse?.Id ?? "WH-01",
                    DestinationRackCode = SelectedRack?.RackCode ?? "RCK-B-04",
                    DestinationLocationCode = "B4-T1",
                    Quantity = 10m
                };
            }
            IsRelocationOpen = true;
        }

        private async Task ExecuteRelocationAsync()
        {
            try
            {
                var result = await _inventoryService.RelocateStockAsync(RelocationForm);
                if (result.Success)
                {
                    IsRelocationOpen = false;
                    ShowFeedback(result.Message, true);
                    await LoadWarehouseTopologyAsync();
                }
                else
                {
                    ShowFeedback(result.Message, false);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback(ex.Message, false);
            }
        }

        // Smart Suggestions
        private void PrepareSuggestion()
        {
            SuggestionRequest = new LocationSuggestionRequest
            {
                ProductId = SelectedStoredItem?.ProductId ?? "PRD-101",
                Quantity = 50m,
                Unit = SelectedStoredItem?.Unit ?? "Pcs",
                TargetWarehouseId = SelectedWarehouse?.Id,
                IsPerishable = false
            };
            LocationSuggestions.Clear();
            IsSuggestionOpen = true;
            _ = ExecuteSuggestionQueryAsync();
        }

        private async Task ExecuteSuggestionQueryAsync()
        {
            try
            {
                var suggestions = await _inventoryService.SuggestStorageLocationsAsync(SuggestionRequest);
                LocationSuggestions.Clear();
                foreach (var s in suggestions)
                {
                    LocationSuggestions.Add(s);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Suggestion query error: {ex.Message}", false);
            }
        }

        private void ApplySuggestion(LocationSuggestion? suggestion)
        {
            if (suggestion == null) return;
            RelocationForm.DestinationWarehouseId = suggestion.WarehouseId;
            RelocationForm.DestinationRackCode = suggestion.RackCode;
            RelocationForm.DestinationLocationCode = suggestion.SectionCode;
            IsSuggestionOpen = false;
            IsRelocationOpen = true;
            ShowFeedback($"Applied suggested location: {suggestion.RackCode} → {suggestion.SectionCode}", true);
        }

        private void NavigateToProductDetails(StoredProductItem? item)
        {
            if (item == null) return;
            _navigationService.NavigateTo("Products");
        }

        private void ShowFeedback(string msg, bool success)
        {
            FeedbackMessage = msg;
            IsFeedbackSuccess = success;
        }
    }
}
