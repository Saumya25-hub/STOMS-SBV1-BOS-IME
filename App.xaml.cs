using System;
using System.Windows;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;
using STOMS_SBV1_BOS_IME.Views;

namespace STOMS_SBV1_BOS_IME
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Coordinates the application startup lifecycle, splash screen orchestration,
    /// operator login authentication, and transition to the STOMS BOS-IME Dashboard.
    /// </summary>
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
        private LoginWindow? _loginWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register global safety exception handlers
            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true;
                MessageBox.Show($"An unhandled system exception occurred: {args.Exception.Message}",
                                "STOMS BOS System Notification",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            };

            // Keep the app alive across window transitions
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Step 1: Start Splash Screen
            var startupService = new StartupService();
            var splashVm = new SplashViewModel(startupService);
            var splashWindow = new SplashWindow(splashVm);

            splashWindow.ReadyToNavigateToLogin += () =>
            {
                splashWindow.Close();
                ShowLoginWindow();
            };

            splashWindow.Show();
        }

        private void ShowLoginWindow()
        {
            var authService = new AuthenticationService();
            var loginVm = new LoginViewModel(authService);
            _loginWindow = new LoginWindow(loginVm);

            _loginWindow.LoginSuccessful += (session) =>
            {
                // Map authenticated OperatorSession into STOMS UserProfile
                UserRole role = session.Role switch
                {
                    "System Administrator" or "Architect" => UserRole.Administrator,
                    "Lead" or "Warehouse Operations Manager" => UserRole.WarehouseManager,
                    "Module Engineer" or "Inventory Supervisor" => UserRole.InventorySupervisor,
                    _ => UserRole.Administrator
                };

                var userProfile = new UserProfile
                {
                    Id = string.IsNullOrEmpty(session.Username) ? "USR-001" : session.Username.ToUpper(),
                    Username = session.Username,
                    FullName = string.IsNullOrEmpty(session.FullName) ? "Saumya Chaudhari" : session.FullName,
                    Role = role,
                    AssignedWarehouse = string.IsNullOrEmpty(session.WarehouseLocation) ? "WH-01: Central Regional Hub" : session.WarehouseLocation,
                    LastLogin = session.AuthenticatedAt
                };

                AuthService.Instance.SetCurrentUser(userProfile);

                // Open Main Dashboard Shell
                OpenMainWindow();

                _loginWindow.Close();
                _loginWindow = null;
            };

            _loginWindow.Show();
            _loginWindow.Activate();
        }

        private void OpenMainWindow()
        {
            _mainWindow = new MainWindow();
            MainWindow = _mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Wire logout to return to Login Window
            AuthService.Instance.AuthStateChanged += OnAuthStateChanged;

            _mainWindow.Closed += (s, e) =>
            {
                AuthService.Instance.AuthStateChanged -= OnAuthStateChanged;
            };

            _mainWindow.Show();
            _mainWindow.Activate();
        }

        private void OnAuthStateChanged(object? sender, UserProfile? profile)
        {
            // If user logged out
            if (profile == null && _mainWindow != null)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                _mainWindow.Close();
                _mainWindow = null;
                ShowLoginWindow();
            }
        }
    }
}
