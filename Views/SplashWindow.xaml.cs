using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Views
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// Manages native WPF animations, DPI-aware rendering, real startup status display,
    /// and clean transitions to the IMS Login page.
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly SplashViewModel _viewModel;
        private Storyboard? _entranceStoryboard;
        private Storyboard? _exitStoryboard;
        private Storyboard? _bootStoryboard;
        private bool _isClosingTransition = false;

        public event Action? ReadyToNavigateToLogin;

        public SplashWindow(SplashViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.InitializationCompleted += OnInitializationCompleted;
            _viewModel.ExitRequested += OnExitRequested;

            Loaded += OnSplashLoaded;
            Closing += OnSplashClosing;
        }

        private async void OnSplashLoaded(object sender, RoutedEventArgs e)
        {
            // Start the Phase A -> Phase D Entrance Storyboard
            _entranceStoryboard = TryFindResource("EntranceStoryboard") as Storyboard;
            _exitStoryboard = TryFindResource("ExitStoryboard") as Storyboard;
            _bootStoryboard = TryFindResource("BootProgressBarStoryboard") as Storyboard;

            if (_entranceStoryboard != null)
            {
                _entranceStoryboard.Begin(this);
            }

            // Start the Windows 98 boot-style continuous illuminated segments animation
            if (_bootStoryboard != null)
            {
                _bootStoryboard.Begin(this, isControllable: true);
            }

            // Start asynchronous real application initialization
            await _viewModel.StartInitializationAsync();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SplashViewModel.ProgressPercentage))
            {
                Dispatcher.Invoke(() =>
                {
                    double trackWidth = BootTrackBorder.ActualWidth > 0 ? BootTrackBorder.ActualWidth : 528;
                    double targetWidth = Math.Clamp(trackWidth * (_viewModel.ProgressPercentage / 100.0), 0, trackWidth);

                    var anim = new DoubleAnimation
                    {
                        To = targetWidth,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    RealProgressFill.BeginAnimation(FrameworkElement.WidthProperty, anim);
                });
            }
        }

        private void OnInitializationCompleted(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isClosingTransition) return;
                _isClosingTransition = true;

                bool transitionTriggered = false;
                void TriggerTransition()
                {
                    if (transitionTriggered) return;
                    transitionTriggered = true;
                    ReadyToNavigateToLogin?.Invoke();
                }

                if (_exitStoryboard != null)
                {
                    _exitStoryboard.Completed += (s, args) => TriggerTransition();
                    _exitStoryboard.Begin(this);

                    // Failsafe timer (600ms) in case Storyboard event is delayed
                    Task.Delay(600).ContinueWith(_ => Dispatcher.Invoke(TriggerTransition));
                }
                else
                {
                    TriggerTransition();
                }
            });
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        private void OnSplashClosing(object? sender, CancelEventArgs e)
        {
            _bootStoryboard?.Stop(this);
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.InitializationCompleted -= OnInitializationCompleted;
            _viewModel.ExitRequested -= OnExitRequested;
            _viewModel.Cancel();
        }
    }
}
