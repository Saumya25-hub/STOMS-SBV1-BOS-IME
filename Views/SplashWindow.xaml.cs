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

            if (_entranceStoryboard != null)
            {
                _entranceStoryboard.Begin(this);
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
                    var anim = new DoubleAnimation
                    {
                        To = _viewModel.ProgressPercentage,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    IndicatorProgressBar.BeginAnimation(ProgressBar.ValueProperty, anim);
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
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.InitializationCompleted -= OnInitializationCompleted;
            _viewModel.ExitRequested -= OnExitRequested;
            _viewModel.Cancel();
        }
    }
}
