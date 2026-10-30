using System.Windows;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Apply saved user theme before MainWindow initializes to prevent theme flashing
            ThemeService.Instance.Initialize();
            base.OnStartup(e);
        }
    }
}
