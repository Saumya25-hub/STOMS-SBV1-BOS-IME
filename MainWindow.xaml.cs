using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Features:
    /// - Continuously animated ambient blue gradient environment.
    /// - Zero-lag real-time hardware-synchronized mouse-following light sheen.
    /// - Seamless diffuse specular lighting with zero dark circular rings.
    /// - Modern electric blue custom ComboBox & ListBox controls.
    /// - Unified professionally rounded outer window corners on all 4 sides.
    /// - Production-ready operator authentication workflow.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private Storyboard? _ambientStoryboard;

        // High-performance pointer tracking variables (Zero allocations on render loop)
        private double _targetX;
        private double _targetY;
        private double _currentX;
        private double _currentY;
        private bool _isRenderingSubscribed = false;

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

            Loaded += OnWindowLoaded;
            Unloaded += OnWindowUnloaded;
            StateChanged += OnWindowStateChanged;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Start continuous ambient gradient animation
            _ambientStoryboard = TryFindResource("AmbientGradientStoryboard") as Storyboard;
            _ambientStoryboard?.Begin(this, isControllable: true);

            // Initialize default resting position for light sheen (centered)
            double panelW = LeftGradientHost.ActualWidth > 0 ? LeftGradientHost.ActualWidth : 430;
            double panelH = LeftGradientHost.ActualHeight > 0 ? LeftGradientHost.ActualHeight : 650;
            _currentX = (panelW / 2.0) - 280;
            _currentY = (panelH / 2.0) - 280;
            _targetX = _currentX;
            _targetY = _currentY;
            MouseLightTransform.X = _currentX;
            MouseLightTransform.Y = _currentY;
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            _ambientStoryboard?.Stop(this);

            if (_isRenderingSubscribed)
            {
                CompositionTarget.Rendering -= OnRenderFrame;
                _isRenderingSubscribed = false;
            }

            Loaded -= OnWindowLoaded;
            Unloaded -= OnWindowUnloaded;
            StateChanged -= OnWindowStateChanged;
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            // Pause GPU animations when minimized to preserve computer resources
            if (WindowState == WindowState.Minimized)
            {
                _ambientStoryboard?.Pause(this);
            }
            else
            {
                _ambientStoryboard?.Resume(this);
            }
        }

        #region Zero-Lag Hardware-Synchronized Mouse-Following Light Sheen

        private void OnLeftPanelMouseEnter(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(LeftGradientHost);
            double clampedX = Math.Clamp(pos.X, 0, LeftGradientHost.ActualWidth);
            double clampedY = Math.Clamp(pos.Y, 0, LeftGradientHost.ActualHeight);

            _targetX = clampedX - 280;
            _targetY = clampedY - 280;
            _currentX = _targetX;
            _currentY = _targetY;
            MouseLightTransform.X = _currentX;
            MouseLightTransform.Y = _currentY;

            if (!_isRenderingSubscribed)
            {
                CompositionTarget.Rendering += OnRenderFrame;
                _isRenderingSubscribed = true;
            }

            // Smoothly fade in specular light sheen
            var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            MouseLightBloom.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void OnLeftPanelMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(LeftGradientHost);

            // Directly update target position — ZERO allocations and ZERO storyboard clocks started!
            double clampedX = Math.Clamp(mousePos.X, 0, LeftGradientHost.ActualWidth);
            double clampedY = Math.Clamp(mousePos.Y, 0, LeftGradientHost.ActualHeight);

            _targetX = clampedX - 280;
            _targetY = clampedY - 280;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            // Monitor-synchronized 60/120fps smooth exponential lerp
            double deltaX = _targetX - _currentX;
            double deltaY = _targetY - _currentY;

            _currentX += deltaX * 0.18;
            _currentY += deltaY * 0.18;

            MouseLightTransform.X = _currentX;
            MouseLightTransform.Y = _currentY;
        }

        private void OnLeftPanelMouseLeave(object sender, MouseEventArgs e)
        {
            if (_isRenderingSubscribed)
            {
                CompositionTarget.Rendering -= OnRenderFrame;
                _isRenderingSubscribed = false;
            }

            // Smoothly fade out specular sheen
            var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            MouseLightBloom.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void OnLeftPanelMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #endregion

        #region Window Controls & Input Handlers

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

        #endregion
    }
}
