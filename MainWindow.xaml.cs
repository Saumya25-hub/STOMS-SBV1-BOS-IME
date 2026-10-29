using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml (IMS Enterprise Login Window)
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public MainWindow() : this(new LoginViewModel(new AuthenticationService()))
        {
        }

        public MainWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Sync initial password to PasswordBox
            txtPassword.Password = _viewModel.Password;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pbox)
            {
                _viewModel.Password = pbox.Password;
            }
        }

        private void OnWindowDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
