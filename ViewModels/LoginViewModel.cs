using System.Collections.ObjectModel;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;

        private string _username = "admin";
        private string _password = "admin";
        private string _selectedWarehouse = "WH-01: Central Regional Hub";
        private bool _rememberMe = true;
        private bool _isBusy = false;
        private string? _statusMessage;
        private bool _isError = false;
        private bool _isSuccess = false;
        private OperatorSession? _session;

        public event EventHandler? LoginSucceeded;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string SelectedWarehouse
        {
            get => _selectedWarehouse;
            set => SetProperty(ref _selectedWarehouse, value);
        }

        public ObservableCollection<string> Warehouses { get; } = new()
        {
            "WH-01: Central Regional Hub",
            "WH-02: Fast-Transit Logistics Depot",
            "WH-03: Raw Materials & Assembly Center",
            "WH-04: High-Value SKU Air-Gapped Vault"
        };

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public OperatorSession? Session
        {
            get => _session;
            private set => SetProperty(ref _session, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand SetDemoRoleCommand { get; }

        public LoginViewModel(IAuthenticationService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), () => !IsBusy);
            SetDemoRoleCommand = new RelayCommand(role => SetDemoCredentials(role?.ToString()));
        }

        private void SetDemoCredentials(string? roleKey)
        {
            switch (roleKey)
            {
                case "architect":
                    Username = "admin";
                    Password = "admin";
                    SelectedWarehouse = Warehouses[0];
                    break;
                case "lead":
                    Username = "jenil";
                    Password = "jenil123";
                    SelectedWarehouse = Warehouses[1];
                    break;
                case "module":
                    Username = "hardik";
                    Password = "hardik123";
                    SelectedWarehouse = Warehouses[2];
                    break;
                default:
                    Username = "admin";
                    Password = "admin";
                    break;
            }
            StatusMessage = $"Demo credentials loaded for role: {roleKey}";
            IsError = false;
        }

        private async Task ExecuteLoginAsync()
        {
            IsBusy = true;
            IsError = false;
            IsSuccess = false;
            StatusMessage = "Authenticating operator credentials with STOMS Security Engine...";

            var (success, message, session) = await _authService.AuthenticateAsync(Username, Password, SelectedWarehouse);

            IsBusy = false;
            StatusMessage = message;

            if (success && session != null)
            {
                IsSuccess = true;
                Session = session;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                IsError = true;
            }
        }
    }
}

