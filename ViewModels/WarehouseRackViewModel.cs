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
    public class WarehouseRackViewModel : ObservableObject
    {
        private readonly IDashboardService _dashboardService;
        private readonly IInventoryService _inventoryService;
        private readonly INavigationService _navigationService;

        private Warehouse? _selectedWarehouse;
        private Rack? _selectedRack;
        private RackSection? _selectedSection;
        private string _searchQuery = string.Empty;
        private double _zoomLevel = 1.0;
        private bool _isGridVisible = true;
        private bool _isAddRackOpen;
        private string _newRackCode = "RCK-C-01";
        private string _newRackName = "New Industrial Pallet Rack";
        private string _newRackZone = "Zone C - South Aisle";
        private decimal _newRackCapacity = 1000m;
        private string _feedbackMessage = string.Empty;

        public ObservableCollection<Warehouse> Warehouses { get; } = new();
        public ObservableCollection<Rack> Racks { get; } = new();

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    _ = LoadRacksAsync();
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

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = LoadRacksAsync();
                }
            }
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.5, 2.0));
        }

        public string ZoomDisplay => $"{(int)(_zoomLevel * 100)}%";

        public bool IsGridVisible
        {
            get => _isGridVisible;
            set => SetProperty(ref _isGridVisible, value);
        }

        public bool IsAddRackOpen
        {
            get => _isAddRackOpen;
            set => SetProperty(ref _isAddRackOpen, value);
        }

        public string NewRackCode
        {
            get => _newRackCode;
            set => SetProperty(ref _newRackCode, value);
        }

        public string NewRackName
        {
            get => _newRackName;
            set => SetProperty(ref _newRackName, value);
        }

        public string NewRackZone
        {
            get => _newRackZone;
            set => SetProperty(ref _newRackZone, value);
        }

        public decimal NewRackCapacity
        {
            get => _newRackCapacity;
            set => SetProperty(ref _newRackCapacity, value);
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SelectRackCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand OpenAddRackCommand { get; }
        public ICommand OpenRackDesignerCommand { get; }
        public ICommand SaveNewRackCommand { get; }
        public ICommand CancelNewRackCommand { get; }
        public ICommand DeleteRackCommand { get; }
        public ICommand ReceiveIntoBayCommand { get; }
        public ICommand IssueFromBayCommand { get; }

        public WarehouseRackViewModel(
            IDashboardService? dashboardService = null,
            IInventoryService? inventoryService = null,
            INavigationService? navigationService = null)
        {
            _dashboardService = dashboardService ?? new MockDashboardService();
            _inventoryService = inventoryService ?? MockInventoryService.Instance;
            _navigationService = navigationService ?? NavigationService.Instance;

            RefreshCommand = new AsyncRelayCommand(async () => await InitializeAsync());
            SelectRackCommand = new RelayCommand<Rack>(r => SelectedRack = r);
            SelectSectionCommand = new RelayCommand<RackSection>(s => SelectedSection = s);
            ZoomInCommand = new RelayCommand(() => ZoomLevel += 0.15);
            ZoomOutCommand = new RelayCommand(() => ZoomLevel -= 0.15);
            ResetZoomCommand = new RelayCommand(() => ZoomLevel = 1.0);

            OpenAddRackCommand = new RelayCommand(() =>
            {
                NewRackCode = $"RCK-{char.ConvertFromUtf32(65 + Racks.Count)}-01";
                IsAddRackOpen = true;
            });

            OpenRackDesignerCommand = new RelayCommand(() =>
            {
                _navigationService.NavigateTo("RackDesigner");
            });

            SaveNewRackCommand = new RelayCommand(SaveNewRack);
            CancelNewRackCommand = new RelayCommand(() => IsAddRackOpen = false);
            DeleteRackCommand = new RelayCommand<Rack>(DeleteRack);

            ReceiveIntoBayCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Stock_Receive"));

            IssueFromBayCommand = new RelayCommand(() =>
                _navigationService.NavigateTo("Stock_Issue"));

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var warehouses = await _dashboardService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var wh in warehouses)
            {
                Warehouses.Add(wh);
            }
            SelectedWarehouse = Warehouses.FirstOrDefault();
            await LoadRacksAsync();
        }

        private async Task LoadRacksAsync()
        {
            var racks = await _dashboardService.GetRacksAsync(
                warehouseId: SelectedWarehouse?.Id,
                searchQuery: SearchQuery);

            Racks.Clear();
            foreach (var r in racks)
            {
                Racks.Add(r);
            }

            if (SelectedRack == null || !Racks.Contains(SelectedRack))
            {
                SelectedRack = Racks.FirstOrDefault();
            }
        }

        private void SaveNewRack()
        {
            if (string.IsNullOrWhiteSpace(NewRackCode)) return;

            var newRack = new Rack
            {
                Id = $"RCK-NEW-{Guid.NewGuid():N}".Substring(0, 10),
                WarehouseId = SelectedWarehouse?.Id ?? "WH-01",
                WarehouseName = SelectedWarehouse?.Name ?? "Central Logistics Hub",
                RackCode = NewRackCode.Trim().ToUpperInvariant(),
                Name = NewRackName.Trim(),
                Zone = NewRackZone.Trim(),
                CanvasX = 40 + (Racks.Count * 280) % 840,
                CanvasY = 40 + (Racks.Count / 3) * 220,
                MaxCapacity = NewRackCapacity,
                OccupiedCapacity = 0,
                CapacityUnit = "kg",
                Sections = new System.Collections.Generic.List<RackSection>
                {
                    new RackSection
                    {
                        Id = $"SEC-{Guid.NewGuid():N}".Substring(0, 8),
                        SectionCode = $"{NewRackCode}-T1",
                        Name = "Tier 1: Ground Pallet Load",
                        TierLevel = 1,
                        MaxCapacity = NewRackCapacity / 2,
                        OccupiedCapacity = 0,
                        CapacityUnit = "kg"
                    },
                    new RackSection
                    {
                        Id = $"SEC-{Guid.NewGuid():N}".Substring(0, 8),
                        SectionCode = $"{NewRackCode}-T2",
                        Name = "Tier 2: Upper Heavy Shelf",
                        TierLevel = 2,
                        MaxCapacity = NewRackCapacity / 2,
                        OccupiedCapacity = 0,
                        CapacityUnit = "kg"
                    }
                }
            };

            Racks.Add(newRack);
            SelectedRack = newRack;
            IsAddRackOpen = false;
            FeedbackMessage = $"Rack {newRack.RackCode} successfully created in {newRack.WarehouseName}.";
        }

        private void DeleteRack(Rack? rack)
        {
            if (rack == null) return;
            Racks.Remove(rack);
            if (SelectedRack == rack)
            {
                SelectedRack = Racks.FirstOrDefault();
            }
        }
    }
}

