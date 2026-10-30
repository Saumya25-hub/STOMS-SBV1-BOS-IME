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

        public UserProfile CurrentUser => _authService.CurrentUser;

        public string RealtimeStatusDescription => _realtimeService.ConnectionStateDescription;

        // Navigation Commands
        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleUserMenuCommand { get; }
        public ICommand ToggleNotificationsCommand { get; }
        public ICommand GlobalSearchCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainWindowViewModel(
            INavigationService? navigationService = null,
            IThemeService? themeService = null,
            IAuthService? authService = null,
            IRealtimeUpdateService? realtimeService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
            _themeService = themeService ?? ThemeService.Instance;
            _authService = authService ?? AuthService.Instance;
            _realtimeService = realtimeService ?? RealtimeUpdateService.Instance;

            _isDarkMode = _themeService.IsDarkMode;
            _dashboardViewModel = new DashboardViewModel(dashboardService: null, _navigationService, _realtimeService);
            _currentViewModel = _dashboardViewModel;

            NavigateCommand = new RelayCommand<string>(NavigateToModule);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
            ToggleThemeCommand = new RelayCommand(() =>
            {
                _themeService.ToggleTheme();
                IsDarkMode = _themeService.IsDarkMode;
            });
            ToggleUserMenuCommand = new RelayCommand(() => IsUserMenuOpen = !IsUserMenuOpen);
            ToggleNotificationsCommand = new RelayCommand(() => IsNotificationsOpen = !IsNotificationsOpen);
            GlobalSearchCommand = new RelayCommand(ExecuteGlobalSearch);
            LogoutCommand = new RelayCommand(() => _authService.Logout());

            _navigationService.NavigationChanged += (s, key) => NavigateToModule(key);
            _themeService.ThemeChanged += (s, isDark) => IsDarkMode = isDark;
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
                    PageSubtitle = "Warehouse overview";
                    Breadcrumb = "Dashboard";
                    CurrentViewModel = _dashboardViewModel;
                    break;

                case "Products":
                    PageTitle = "Product Management";
                    Breadcrumb = "STOMS ERP / Master Data / Products & SKUs";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Product Management & Master SKU Catalog",
                        "MOD-PRD-02",
                        "Step 2 Engineering Milestone",
                        "Centralized SKU catalog, Bill of Materials (BOM), unit definition, supplier mapping, and barcodes.",
                        new List<string>
                        {
                            "Multi-tier category & brand classification",
                            "Dynamic reorder point & safety stock calculation",
                            "EAN-13, QR Code & custom barcode generation",
                            "Batch & serial number tracking definitions"
                        });
                    break;

                case "Stock":
                    PageTitle = "Stock Management";
                    Breadcrumb = "STOMS ERP / Operations / Real-Time Stock Ledger";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Stock Management & Multi-Warehouse Ledger",
                        "MOD-STK-03",
                        "Step 2 Engineering Milestone",
                        "Comprehensive inventory valuations, FIFO/LIFO batch tracking, and real-time ledger synchronization.",
                        new List<string>
                        {
                            "Air-gapped transaction verification",
                            "Unit-of-measurement cross-ledger protection",
                            "Cycle counts and variance reconciliations",
                            "Real-time SignalR distributed stock updates"
                        });
                    break;

                case "Warehouse":
                    PageTitle = "Warehouse & Rack Management";
                    Breadcrumb = "STOMS ERP / Logistics / Warehouse & Rack Layout";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Warehouse & Advanced Rack Topology",
                        "MOD-WH-04",
                        "Step 3 Engineering Milestone",
                        "Interactive 2D/3D bay visualizer, rack layout builder, capacity modeling, and bin slotting.",
                        new List<string>
                        {
                            "Drag-and-drop rack shelf & compartment builder",
                            "Weight load and volumetric distribution calculation",
                            "Dynamic bin-level QR tagging & forklift routing",
                            "Zone temperature and environmental condition logging"
                        });
                    break;

                case "Movements":
                    PageTitle = "Stock Movements & Inward/Outward";
                    Breadcrumb = "STOMS ERP / Operations / Movement History";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Stock Movements & Material Transfers",
                        "MOD-MOV-05",
                        "Step 3 Engineering Milestone",
                        "Complete inward receipt (GRN), dispatch work orders, inter-warehouse transit, and scrap records.",
                        new List<string>
                        {
                            "Dual-custody digital sign-off on transfers",
                            "Supplier Goods Receipt Note (GRN) verification",
                            "Automated pick-list & pack-slip generation",
                            "Defect and quarantine scrap isolation ledger"
                        });
                    break;

                case "Approvals":
                    PageTitle = "Approvals & Sign-Offs";
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
                    Breadcrumb = "STOMS ERP / Executive / Reports & Analytics";
                    CurrentViewModel = new PlaceholderViewModel(
                        "Warehouse Analytics & Executive Reporting",
                        "MOD-REP-07",
                        "Step 4 Engineering Milestone",
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
                    PageTitle = "User Management & Access Control";
                    Breadcrumb = "STOMS ERP / Administration / Users & Roles";
                    CurrentViewModel = new PlaceholderViewModel(
                        "User Management & RBAC Permissions",
                        "MOD-USR-08",
                        "Step 5 Engineering Milestone",
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
                    PageTitle = "System Settings";
                    Breadcrumb = "STOMS ERP / Administration / Configuration";
                    CurrentViewModel = new PlaceholderViewModel(
                        "System & Network Configuration",
                        "MOD-SET-09",
                        "Step 5 Engineering Milestone",
                        "Central API endpoint configuration, SignalR telemetry options, local cache policy, and printer setup.",
                        new List<string>
                        {
                            "Central ASP.NET Core API server URL & SSL certificates",
                            "Thermal barcode printer & zebra scanner integration",
                            "Local offline SQLite synchronization parameters",
                            "Theme and regional display formatting options"
                        });
                    break;
            }
        }
    }
}
