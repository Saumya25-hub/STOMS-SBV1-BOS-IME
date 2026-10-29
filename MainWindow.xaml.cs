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
    /// - Real interactive mouse-following light bloom with smooth interpolation.
    /// - Unified professionally rounded outer window corners on all 4 sides.
    /// - Production-ready operator authentication workflow.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private Storyboard? _ambientStoryboard;
        private readonly CubicEase _motionEasing = new() { EasingMode = EasingMode.EaseOut };
        private readonly TimeSpan _lightMoveDuration = TimeSpan.FromMilliseconds(220);

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

            // Initialize default resting position for mouse light bloom
            double centerX = (LeftGradientHost.ActualWidth > 0 ? LeftGradientHost.ActualWidth : 430) / 2.0 - 170;
            double centerY = (LeftGradientHost.ActualHeight > 0 ? LeftGradientHost.ActualHeight : 650) / 2.0 - 170;
            MouseLightTransform.X = centerX;
            MouseLightTransform.Y = centerY;
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            _ambientStoryboard?.Stop(this);
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

        #region Interactive Mouse-Following Light Engine

        private void OnLeftPanelMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(LeftGradientHost);

            // Clamp coordinates to panel bounds
            double clampedX = Math.Clamp(mousePos.X, 0, LeftGradientHost.ActualWidth);
            double clampedY = Math.Clamp(mousePos.Y, 0, LeftGradientHost.ActualHeight);

            // Center the 340x340 radial light bloom around pointer
            double targetX = clampedX - 170;
            double targetY = clampedY - 170;

            // Smoothly animate the bloom position (fluid damping without jumping)
            var animX = new DoubleAnimation
            {
                To = targetX,
                Duration = _lightMoveDuration,
                EasingFunction = _motionEasing
            };

            var animY = new DoubleAnimation
            {
                To = targetY,
                Duration = _lightMoveDuration,
                EasingFunction = _motionEasing
            };

            MouseLightTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            MouseLightTransform.BeginAnimation(TranslateTransform.YProperty, animY);
        }

        private void OnLeftPanelMouseEnter(object sender, MouseEventArgs e)
        {
            // Fade in interactive light bloom
            var fadeAnim = new DoubleAnimation
            {
                To = 0.72,
                Duration = TimeSpan.FromMilliseconds(240),
                EasingFunction = _motionEasing
            };
            MouseLightBloom.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        private void OnLeftPanelMouseLeave(object sender, MouseEventArgs e)
        {
            // Fade out interactive light bloom gracefully
            var fadeAnim = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(380),
                EasingFunction = _motionEasing
            };
            MouseLightBloom.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            // Gently glide light toward panel center
            double centerX = (LeftGradientHost.ActualWidth > 0 ? LeftGradientHost.ActualWidth : 430) / 2.0 - 170;
            double centerY = (LeftGradientHost.ActualHeight > 0 ? LeftGradientHost.ActualHeight : 650) / 2.0 - 170;

            var returnX = new DoubleAnimation(centerX, TimeSpan.FromMilliseconds(450)) { EasingFunction = _motionEasing };
            var returnY = new DoubleAnimation(centerY, TimeSpan.FromMilliseconds(450)) { EasingFunction = _motionEasing };
            MouseLightTransform.BeginAnimation(TranslateTransform.XProperty, returnX);
            MouseLightTransform.BeginAnimation(TranslateTransform.YProperty, returnY);
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
