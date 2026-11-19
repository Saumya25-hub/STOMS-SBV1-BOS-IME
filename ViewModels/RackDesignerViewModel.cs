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
    public class RackDesignerViewModel : ObservableObject
    {
        private readonly IRackLayoutService _layoutService;
        private readonly IInventoryService _inventoryService;
        private readonly INavigationService _navigationService;

        private WarehouseRackLayout _currentLayout = new();
        private LayoutItemModel? _selectedItem;
        private string _activeTool = "Select"; // Select, UserPosition, Rack, Shelf, StorageArea, Label
        private double _zoomLevel = 1.0;
        private bool _isConfigPanelOpen;
        private bool _isPreviewMode;
        private bool _isWizardWelcomeVisible;
        private string _feedbackMessage = string.Empty;
        private readonly Stack<List<LayoutItemModel>> _undoStack = new();
        private readonly Stack<List<LayoutItemModel>> _redoStack = new();

        public ObservableCollection<LayoutItemModel> Items { get; } = new();

        public LayoutItemModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != null) _selectedItem.IsSelected = false;
                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem != null) _selectedItem.IsSelected = true;
                    OnPropertyChanged(nameof(HasSelectedItem));
                    OnPropertyChanged(nameof(IsRackSelected));
                    OnPropertyChanged(nameof(GeneratedShelvesPreview));
                }
            }
        }

        public bool HasSelectedItem => SelectedItem != null;
        public bool IsRackSelected => SelectedItem?.ElementType == LayoutElementType.Rack;

        public string ActiveTool
        {
            get => _activeTool;
            set => SetProperty(ref _activeTool, value);
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.5, 2.0));
        }

        public string ZoomDisplay => $"{(int)(_zoomLevel * 100)}%";

        public bool IsConfigPanelOpen
        {
            get => _isConfigPanelOpen;
            set => SetProperty(ref _isConfigPanelOpen, value);
        }

        public bool IsPreviewMode
        {
            get => _isPreviewMode;
            set => SetProperty(ref _isPreviewMode, value);
        }

        public bool IsWizardWelcomeVisible
        {
            get => _isWizardWelcomeVisible;
            set => SetProperty(ref _isWizardWelcomeVisible, value);
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public string GeneratedShelvesPreview
        {
            get
            {
                if (SelectedItem?.ElementType != LayoutElementType.Rack) return string.Empty;
                var list = SelectedItem.GenerateShelfCodes();
                return string.Join(", ", list.Take(12)) + (list.Count > 12 ? $"... ({list.Count} total shelves)" : "");
            }
        }

        // Commands
        public ICommand SelectToolCommand { get; }
        public ICommand CanvasClickCommand { get; }
        public ICommand SelectItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand CloseConfigCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand SaveLayoutCommand { get; }
        public ICommand TogglePreviewCommand { get; }
        public ICommand StartDesignerFromWelcomeCommand { get; }
        public ICommand CompleteWizardCommand { get; }

        public RackDesignerViewModel(
            IRackLayoutService? layoutService = null,
            IInventoryService? inventoryService = null,
            INavigationService? navigationService = null)
        {
            _layoutService = layoutService ?? RackLayoutService.Instance;
            _inventoryService = inventoryService ?? MockInventoryService.Instance;
            _navigationService = navigationService ?? NavigationService.Instance;

            SelectToolCommand = new RelayCommand<string>(tool =>
            {
                ActiveTool = tool ?? "Select";
                if (tool != "Select" && tool != "Delete")
                {
                    AddNewItemFromTool(tool!);
                }
            });

            CanvasClickCommand = new RelayCommand(() =>
            {
                if (ActiveTool == "Select")
                {
                    SelectedItem = null;
                    IsConfigPanelOpen = false;
                }
            });

            SelectItemCommand = new RelayCommand<LayoutItemModel>(item =>
            {
                if (ActiveTool == "Delete")
                {
                    if (item != null) DeleteItem(item);
                    return;
                }

                SelectedItem = item;
                if (item != null && item.ElementType == LayoutElementType.Rack)
                {
                    IsConfigPanelOpen = true;
                }
            });

            DeleteItemCommand = new RelayCommand<LayoutItemModel>(DeleteItem);
            OpenConfigCommand = new RelayCommand(() => IsConfigPanelOpen = true);
            CloseConfigCommand = new RelayCommand(() =>
            {
                IsConfigPanelOpen = false;
                OnPropertyChanged(nameof(GeneratedShelvesPreview));
            });

            ZoomInCommand = new RelayCommand(() => ZoomLevel += 0.15);
            ZoomOutCommand = new RelayCommand(() => ZoomLevel -= 0.15);
            ResetZoomCommand = new RelayCommand(() => ZoomLevel = 1.0);

            UndoCommand = new RelayCommand(Undo);
            RedoCommand = new RelayCommand(Redo);
            SaveLayoutCommand = new AsyncRelayCommand(async () => await SaveLayoutAsync());
            TogglePreviewCommand = new RelayCommand(() => IsPreviewMode = !IsPreviewMode);
            StartDesignerFromWelcomeCommand = new RelayCommand(() => IsWizardWelcomeVisible = false);
            CompleteWizardCommand = new AsyncRelayCommand(async () =>
            {
                await SaveLayoutAsync();
                _layoutService.IsSetupCompleted = true;
                _navigationService.NavigateTo("Products");
            });

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _currentLayout = await _layoutService.GetLayoutAsync("WH-01");
            Items.Clear();
            foreach (var item in _currentLayout.Items)
            {
                Items.Add(item);
            }

            // If initial setup is not marked completed, display the Welcome banner
            if (!_layoutService.IsSetupCompleted)
            {
                IsWizardWelcomeVisible = true;
            }
        }

        private void SnapshotState()
        {
            var clone = Items.Select(i => new LayoutItemModel
            {
                Id = i.Id,
                ElementType = i.ElementType,
                Code = i.Code,
                Name = i.Name,
                Description = i.Description,
                X = i.X,
                Y = i.Y,
                Width = i.Width,
                Height = i.Height,
                ColorHex = i.ColorHex,
                RowsCount = i.RowsCount,
                ShelvesPerRow = i.ShelvesPerRow,
                IsAdvancedNaming = i.IsAdvancedNaming,
                HasRows = i.HasRows,
                Zone = i.Zone
            }).ToList();

            _undoStack.Push(clone);
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(Items.ToList());
                var state = _undoStack.Pop();
                Items.Clear();
                foreach (var s in state) Items.Add(s);
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(Items.ToList());
                var state = _redoStack.Pop();
                Items.Clear();
                foreach (var s in state) Items.Add(s);
            }
        }

        private void AddNewItemFromTool(string tool)
        {
            SnapshotState();

            double defaultX = 200 + (Items.Count * 25) % 400;
            double defaultY = 150 + (Items.Count * 20) % 300;

            LayoutItemModel newItem = tool switch
            {
                "UserPosition" => new LayoutItemModel
                {
                    ElementType = LayoutElementType.UserPosition,
                    Code = $"STF-{Items.Count(i => i.ElementType == LayoutElementType.UserPosition) + 1:D2}",
                    Name = "Staff Station",
                    X = defaultX,
                    Y = defaultY,
                    Width = 80,
                    Height = 80,
                    ColorHex = "#10B981"
                },
                "Rack" => new LayoutItemModel
                {
                    ElementType = LayoutElementType.Rack,
                    Code = $"RCK-{char.ConvertFromUtf32(65 + Items.Count(i => i.ElementType == LayoutElementType.Rack))}",
                    Name = $"Rack {char.ConvertFromUtf32(65 + Items.Count(i => i.ElementType == LayoutElementType.Rack))}",
                    Description = "Standard Industrial Pallet Rack",
                    X = defaultX,
                    Y = defaultY,
                    Width = 200,
                    Height = 110,
                    ColorHex = "#2563EB",
                    RowsCount = 4,
                    ShelvesPerRow = 2,
                    IsAdvancedNaming = false,
                    HasRows = true
                },
                "Shelf" => new LayoutItemModel
                {
                    ElementType = LayoutElementType.Shelf,
                    Code = $"SHF-{Items.Count + 1}",
                    Name = $"Shelf Bay {Items.Count + 1}",
                    X = defaultX,
                    Y = defaultY,
                    Width = 140,
                    Height = 60,
                    ColorHex = "#38BDF8"
                },
                "StorageArea" => new LayoutItemModel
                {
                    ElementType = LayoutElementType.StorageArea,
                    Code = $"AREA-{Items.Count(i => i.ElementType == LayoutElementType.StorageArea) + 1:D2}",
                    Name = "Floor Staging Zone",
                    X = defaultX,
                    Y = defaultY,
                    Width = 220,
                    Height = 120,
                    ColorHex = "#F59E0B"
                },
                "Label" => new LayoutItemModel
                {
                    ElementType = LayoutElementType.Label,
                    Code = "LBL",
                    Name = "Zone A - North Aisle",
                    X = defaultX,
                    Y = defaultY,
                    Width = 160,
                    Height = 40,
                    ColorHex = "#64748B"
                },
                _ => new LayoutItemModel()
            };

            Items.Add(newItem);
            SelectedItem = newItem;
            if (newItem.ElementType == LayoutElementType.Rack)
            {
                IsConfigPanelOpen = true;
            }
            ActiveTool = "Select";
        }

        private async void DeleteItem(LayoutItemModel? item)
        {
            if (item == null) return;

            try
            {
                SnapshotState();
                var deleted = await _layoutService.DeleteLayoutItemAsync(_currentLayout.WarehouseId, item.Id);
                if (deleted)
                {
                    Items.Remove(item);
                    if (SelectedItem == item)
                    {
                        SelectedItem = null;
                        IsConfigPanelOpen = false;
                    }
                    ShowFeedback($"Deleted {item.Name} successfully.");
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Deletion Blocked: {ex.Message}");
            }
        }

        public async Task SaveLayoutAsync()
        {
            _currentLayout.Items = Items.ToList();
            var success = await _layoutService.SaveLayoutAsync(_currentLayout);
            if (success)
            {
                ShowFeedback("Warehouse layout topology saved successfully!");
            }
            else
            {
                ShowFeedback("Error saving layout to local storage.");
            }
        }

        private void ShowFeedback(string message)
        {
            FeedbackMessage = message;
            _ = Task.Delay(4000).ContinueWith(_ => FeedbackMessage = string.Empty);
        }
    }
}
