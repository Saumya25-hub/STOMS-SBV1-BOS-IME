using System;
using System.Collections.Generic;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IThemeService _themeService;
        private readonly IAuthService _authService;
        private readonly IRealtimeUpdateService _realtimeService;
        private readonly ILocalizationService _localizationService;

        private object _currentViewModel;
        private string _activeNavKey = "Dashboard";
        private string _pageTitle = "Home Dashboard";
        private string _pageSubtitle = "Warehouse overview";
        private string _breadcrumb = "Dashboard";
        private bool _isSidebarCollapsed;
        private bool _isDarkMode = true;
        private bool _isUserMenuOpen;
        private bool _isNotificationsOpen;
        private string _globalSearchText = string.Empty;

        private readonly DashboardViewModel _dashboardViewModel;
        private readonly ProductManagementViewModel _productManagementViewModel;
        private readonly WarehouseRackViewModel _warehouseRackViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly SalesInvoiceViewModel _salesInvoiceViewModel;
        private readonly RackDesignerViewModel _rackDesignerViewModel;

        public object CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public string ActiveNavKey
        {
            get => _activeNavKey;
            private set => SetProperty(ref _activeNavKey, value);
        }

        public string PageTitle
        {
            get => _pageTitle;
            private set
            {
                if (SetProperty(ref _pageTitle, value))
                {
                    OnPropertyChanged(nameof(HeaderDisplayTitle));
                }
            }
        }

        public string HeaderDisplayTitle => _pageTitle == "Home Dashboard" ? "Dashboard" : _pageTitle;

        public string PageSubtitle
        {
            get => _pageSubtitle;
            private set => SetProperty(ref _pageSubtitle, value);
        }

        public string Breadcrumb
        {
            get => _breadcrumb;
            private set => SetProperty(ref _breadcrumb, value);
        }

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set => SetProperty(ref _isSidebarCollapsed, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        public bool IsUserMenuOpen
        {
            get => _isUserMenuOpen;
            set => SetProperty(ref _isUserMenuOpen, value);
        }

        public bool IsNotificationsOpen
        {
            get => _isNotificationsOpen;
            set => SetProperty(ref _isNotificationsOpen, value);
        }

        public string GlobalSearchText
        {
            get => _globalSearchText;
            set => SetProperty(ref _globalSearchText, value);
        }

        public string LanguageDisplay => _localizationService.CurrentLanguage switch
        {
            AppLanguage.English => "EN",
            AppLanguage.Hindi => "HI",
            AppLanguage.Hinglish => "HIN",
            _ => "EN"
        };

        public UserProfile CurrentUser => _authService.CurrentUser;
        public string RealtimeStatusDescription => _realtimeService.ConnectionStateDescription;

        // Navigation Commands
        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleLanguageCommand { get; }
        public ICommand BarcodeScanShortcutCommand { get; }
        public ICommand ToggleUserMenuCommand { get; }
        public ICommand ToggleNotificationsCommand { get; }
        public ICommand GlobalSearchCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainWindowViewModel(
            INavigationService? navigationService = null,
            IThemeService? themeService = null,
            IAuthService? authService = null,
            IRealtimeUpdateService? realtimeService = null,
            ILocalizationService? localizationService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
            _themeService = themeService ?? ThemeService.Instance;
            _authService = authService ?? AuthService.Instance;
            _realtimeService = realtimeService ?? RealtimeUpdateService.Instance;
            _localizationService = localizationService ?? LocalizationService.Instance;

            _isDarkMode = _themeService.IsDarkMode;
            _dashboardViewModel = new DashboardViewModel(dashboardService: null, _navigationService, _realtimeService);
            _productManagementViewModel = new ProductManagementViewModel(MockInventoryService.Instance, _navigationService);
            _warehouseRackViewModel = new WarehouseRackViewModel(null, MockInventoryService.Instance, _navigationService);
            _settingsViewModel = new SettingsViewModel(_themeService, _localizationService);
            _salesInvoiceViewModel = new SalesInvoiceViewModel(SalesService.Instance, MockInventoryService.Instance, BarcodeService.Instance, _navigationService);
            _rackDesignerViewModel = new RackDesignerViewModel(RackLayoutService.Instance, MockInventoryService.Instance, _navigationService);

            _currentViewModel = _dashboardViewModel;

            NavigateCommand = new RelayCommand<string>(NavigateToModule);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
            ToggleThemeCommand = new RelayCommand(() =>
            {
                _themeService.ToggleTheme();
                IsDarkMode = _themeService.IsDarkMode;
            });
            ToggleLanguageCommand = new RelayCommand(CycleLanguage);
            BarcodeScanShortcutCommand = new RelayCommand(() =>
            {
                NavigateToModule("Products");
            });
            ToggleUserMenuCommand = new RelayCommand(() => IsUserMenuOpen = !IsUserMenuOpen);
            ToggleNotificationsCommand = new RelayCommand(() => IsNotificationsOpen = !IsNotificationsOpen);
            GlobalSearchCommand = new RelayCommand(ExecuteGlobalSearch);
            LogoutCommand = new RelayCommand(() => _authService.Logout());

            _navigationService.NavigationChanged += (s, key) => NavigateToModule(key);
            _themeService.ThemeChanged += (s, isDark) => IsDarkMode = isDark;
            _localizationService.LanguageChanged += (s, lang) => OnPropertyChanged(nameof(LanguageDisplay));
        }

        private void CycleLanguage()
        {
            var next = _localizationService.CurrentLanguage switch
            {
                AppLanguage.English => AppLanguage.Hindi,
                AppLanguage.Hindi => AppLanguage.Hinglish,
                AppLanguage.Hinglish => AppLanguage.English,
                _ => AppLanguage.English
            };
            _localizationService.SetLanguage(next);
        }

        private void ExecuteGlobalSearch()
        {
            if (string.IsNullOrWhiteSpace(GlobalSearchText)) return;
            // Route global SKU/Product search directly to dashboard rack & inventory filters
            NavigateToModule("Dashboard");
            _dashboardViewModel.RackSearchQuery = GlobalSearchText.Trim();
        }

        public void NavigateToModule(string? moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey)) return;

            ActiveNavKey = moduleKey;
            IsUserMenuOpen = false;
            IsNotificationsOpen = false;

            switch (moduleKey)
            {
                case "Dashboard":
                    PageTitle = "Home Dashboard";
                    PageSubtitle = "Warehouse overview & executive telemetry";
                    Breadcrumb = "Dashboard";
                    CurrentViewModel = _dashboardViewModel;
                    break;

                case "Products":
                    PageTitle = "Product Master";
                    PageSubtitle = "Central catalog, SKU definitions & specifications";
                    Breadcrumb = "STOMS ERP / Master Data / Products & SKUs";
                    _productManagementViewModel.ActiveSection = "Catalog";
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Products_Add":
                    ActiveNavKey = "Products";
                    PageTitle = "Product Master";
                    PageSubtitle = "Configure master SKU & inventory tracking";
                    Breadcrumb = "STOMS ERP / Master Data / Add New SKU";
                    _productManagementViewModel.ActiveSection = "Catalog";
                    _productManagementViewModel.OpenAddProduct();
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Stock":
                    PageTitle = "Stock Management";
                    PageSubtitle = "Multi-warehouse real-time balance ledger";
                    Breadcrumb = "STOMS ERP / Operations / Stock Balances";
                    _productManagementViewModel.ActiveSection = "Balances";
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Stock_Receive":
                    ActiveNavKey = "Stock";
                    PageTitle = "Stock Management";
                    PageSubtitle = "Goods Inward Receipt (GRN) & Inspection";
                    Breadcrumb = "STOMS ERP / Operations / Stock Receive";
                    _productManagementViewModel.ActiveSection = "Catalog";
                    _productManagementViewModel.OpenReceiveStock();
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Stock_Issue":
                    ActiveNavKey = "Stock";
                    PageTitle = "Stock Management";
                    PageSubtitle = "Goods Outward Requisition & Dispatch";
                    Breadcrumb = "STOMS ERP / Operations / Stock Issue";
                    _productManagementViewModel.ActiveSection = "Catalog";
                    _productManagementViewModel.OpenIssueStock();
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Warehouse":
                    PageTitle = "Warehouse & Rack Topology";
                    PageSubtitle = "Interactive 2D visual layout studio & storage bays";
                    Breadcrumb = "STOMS ERP / Logistics / Warehouse & Rack Layout";
                    CurrentViewModel = _warehouseRackViewModel;
                    break;

                case "RackDesigner":
                    ActiveNavKey = "Warehouse";
                    PageTitle = "Visual Rack Designer";
                    PageSubtitle = "Layout canvas & visual storage topology builder";
                    Breadcrumb = "STOMS ERP / Logistics / Visual Rack Designer";
                    CurrentViewModel = _rackDesignerViewModel;
                    break;

                case "Sales":
                case "POS":
                    PageTitle = "Sales & POS Invoicing";
                    PageSubtitle = "Point of sale billing with automated FEFO batch allocation";
                    Breadcrumb = "STOMS ERP / Operations / Sales & POS";
                    CurrentViewModel = _salesInvoiceViewModel;
                    break;

                case "Movements":
                    PageTitle = "Stock Movements & Audit History";
                    PageSubtitle = "Material transfers, transactions & batch traceability";
                    Breadcrumb = "STOMS ERP / Operations / Movement History";
                    _productManagementViewModel.ActiveSection = "Batches";
                    CurrentViewModel = _productManagementViewModel;
                    break;

                case "Approvals":
                    PageTitle = "Approvals & Sign-Offs";
                    PageSubtitle = "Role-gated dual authorization for high-value issues";
                    Breadcrumb = "STOMS ERP / Governance / Pending Approvals";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Dual-Authorization & Approval Workflows",
                        "MOD-APP-06",
                        "Step 4 Engineering Milestone",
                        "Role-gated supervisor sign-offs for high-value issues, write-offs, stock adjustments, and transfers.",
                        new List<string>
                        {
                            "Multi-level approval threshold matrices",
                            "Audit trail with cryptographic hash signatures",
                            "Variance explanations & photo evidence attachments",
                            "Automated email & SignalR notification alerts"
                        });
                    break;

                case "Reports":
                    PageTitle = "Reports & Analytics";
                    PageSubtitle = "Inventory valuation, velocity & stock analytics";
                    Breadcrumb = "STOMS ERP / Executive / Reports & Analytics";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Warehouse Analytics & Executive Reporting",
                        "MOD-REP-07",
                        "Enterprise Reporting Suite",
                        "Inventory turnover, dead-stock analysis, SKU velocity, lead times, and financial valuations.",
                        new List<string>
                        {
                            "Stock aging & near-expiry risk forecasting",
                            "Fast-moving vs. slow-moving (FSN) classification",
                            "Export to Excel, PDF, CSV and STOMS ERP sync",
                            "Discrepancy and audit trend graphs"
                        });
                    break;

                case "Users":
                    PageTitle = "User Management & RBAC";
                    PageSubtitle = "Role-based access controls and workstation security";
                    Breadcrumb = "STOMS ERP / Administration / Users & Roles";
                    CurrentViewModel = new PlaceholderViewModel(
                        "User Management & RBAC Permissions",
                        "MOD-USR-08",
                        "Role & Access Management",
                        "Role-based access controls (RBAC), multi-workstation logins, and activity audit logging.",
                        new List<string>
                        {
                            "Custom permission groups (Admin, Supervisor, Clerk)",
                            "Single Sign-On (SSO) & Active Directory integration",
                            "Workstation binding & authorized IP restrictions",
                            "Full audit log of login events and data modifications"
                        });
                    break;

                case "Settings":
                    PageTitle = "System Settings & Configuration";
                    PageSubtitle = "Central appearance, language localization & network profile";
                    Breadcrumb = "STOMS ERP / Administration / Configuration";
                    CurrentViewModel = _settingsViewModel;
                    break;
            }
        }
    }
}
