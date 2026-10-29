using System.Windows;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;
using STOMS_SBV1_BOS_IME.Views;

namespace STOMS_SBV1_BOS_IME
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Coordinates the application startup lifecycle, splash screen orchestration,
    /// and transition to the IMS Login window.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register global safety exception handlers
            DispatcherUnhandledException += (s, args) =>
            {
                // Prevent crash on minor UI dispatcher exceptions
                args.Handled = true;
                MessageBox.Show($"An unhandled system exception occurred: {args.Exception.Message}",
                                "IMS System Notification",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            };

            // Set explicit shutdown mode while splash screen runs so closing the splash
            // does not prematurely exit the application process.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var startupService = new StartupService();
            var splashVm = new SplashViewModel(startupService);
            var splashWindow = new SplashWindow(splashVm);

            splashWindow.ReadyToNavigateToLogin += () =>
            {
                // Instantiate the IMS login window
                var authService = new AuthenticationService();
                var loginVm = new LoginViewModel(authService);
                var mainWindow = new MainWindow(loginVm);

                // Set application main window and switch shutdown mode
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Display login window smoothly
                mainWindow.Show();
                mainWindow.Activate();

                // Close and clean up the splash window
                splashWindow.Close();
            };

            splashWindow.Show();
        }
    }
}
