using System.Windows;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class SplashViewModel : ViewModelBase
    {
        private readonly IStartupService _startupService;
        private CancellationTokenSource? _cts;

        private double _progressPercentage = 0;
        private string _statusText = "Initializing your workspace...";
        private string _stepTitle = "System Initialization";
        private bool _hasFailed = false;
        private string? _errorMessage;
        private bool _isInitializing = false;

        public event EventHandler? InitializationCompleted;
        public event EventHandler? ExitRequested;

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string StepTitle
        {
            get => _stepTitle;
            set => SetProperty(ref _stepTitle, value);
        }

        public bool HasFailed
        {
            get => _hasFailed;
            set => SetProperty(ref _hasFailed, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsInitializing
        {
            get => _isInitializing;
            set => SetProperty(ref _isInitializing, value);
        }

        public string Tagline => "Smart Inventory. Seamless Management.";
        public string VersionText => "STOMS Business Operating System • v1.0.0-BOS";

        public ICommand RetryCommand { get; }
        public ICommand ExitCommand { get; }

        public SplashViewModel(IStartupService startupService)
        {
            _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));

            RetryCommand = new RelayCommand(async () => await ExecuteRetryAsync(), () => !IsInitializing);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        public async Task StartInitializationAsync()
        {
            if (IsInitializing) return;

            IsInitializing = true;
            HasFailed = false;
            ErrorMessage = null;
            ProgressPercentage = 0;
            StatusText = "Initializing your workspace...";

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var progressReporter = new Progress<StartupMilestone>(milestone =>
            {
                ProgressPercentage = milestone.ProgressPercentage;
                StepTitle = milestone.StepTitle;
                StatusText = milestone.StatusMessage;

                if (milestone.IsFailed)
                {
                    HasFailed = true;
                    ErrorMessage = milestone.ErrorMessage ?? "An unexpected initialization error occurred.";
                }
            });

            bool success = await _startupService.RunStartupInitializationAsync(progressReporter, _cts.Token);

            IsInitializing = false;

            if (success)
            {
                // Brief pause so 100% completion state is clearly seen by the user before transition
                await Task.Delay(250);
                InitializationCompleted?.Invoke(this, EventArgs.Empty);
            }
            else if (!HasFailed)
            {
                HasFailed = true;
                ErrorMessage = _startupService.LastErrorMessage ?? "Application startup could not be completed.";
            }
        }

        private async Task ExecuteRetryAsync()
        {
            _startupService.SetSimulatedFailure(false);
            await StartInitializationAsync();
        }

        private void ExecuteExit()
        {
            _cts?.Cancel();
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }
    }
}
