using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using STOMS_SBV1_BOS_IME.Common;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localizationService;

        private ThemeMode _selectedThemeMode;
        private AppLanguage _selectedLanguage;
        private string _apiEndpoint = "https://api.stoms.internal/v1";
        private string _feedbackMessage = string.Empty;
        private bool _isSavedNotificationVisible;

        public ThemeMode SelectedThemeMode
        {
            get => _selectedThemeMode;
            set
            {
                if (SetProperty(ref _selectedThemeMode, value))
                {
                    _themeService.SetThemeMode(value);
                    ShowFeedback($"Theme set to {value}.");
                }
            }
        }

        public AppLanguage SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    _localizationService.SetLanguage(value);
                    ShowFeedback($"Application language updated to {value}.");
                }
            }
        }

        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set => SetProperty(ref _apiEndpoint, value);
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set => SetProperty(ref _feedbackMessage, value);
        }

        public bool IsSavedNotificationVisible
        {
            get => _isSavedNotificationVisible;
            set => SetProperty(ref _isSavedNotificationVisible, value);
        }

        public string AppVersion => "STOMS Business Operating System — BOS-IME v2.0";
        public string RuntimeFramework => ".NET 8.0 (Windows Desktop WPF)";
        public string StorageArchitecture => "Central ASP.NET Core API + Real-Time SignalR Ledger";

        public ICommand SetThemeDarkCommand { get; }
        public ICommand SetThemeLightCommand { get; }
        public ICommand SetThemeSystemCommand { get; }
        public ICommand SetLanguageEnCommand { get; }
        public ICommand SetLanguageHiCommand { get; }
        public ICommand SetLanguageHinglishCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand DismissFeedbackCommand { get; }

        public SettingsViewModel(
            IThemeService? themeService = null,
            ILocalizationService? localizationService = null)
        {
            _themeService = themeService ?? ThemeService.Instance;
            _localizationService = localizationService ?? LocalizationService.Instance;

            _selectedThemeMode = _themeService.CurrentMode;
            _selectedLanguage = _localizationService.CurrentLanguage;

            SetThemeDarkCommand = new RelayCommand(() => SelectedThemeMode = ThemeMode.Dark);
            SetThemeLightCommand = new RelayCommand(() => SelectedThemeMode = ThemeMode.Light);
            SetThemeSystemCommand = new RelayCommand(() => SelectedThemeMode = ThemeMode.System);

            SetLanguageEnCommand = new RelayCommand(() => SelectedLanguage = AppLanguage.English);
            SetLanguageHiCommand = new RelayCommand(() => SelectedLanguage = AppLanguage.Hindi);
            SetLanguageHinglishCommand = new RelayCommand(() => SelectedLanguage = AppLanguage.Hinglish);

            SaveSettingsCommand = new RelayCommand(() =>
            {
                ShowFeedback("Settings preferences successfully saved.");
            });

            DismissFeedbackCommand = new RelayCommand(() => IsSavedNotificationVisible = false);
        }

        private void ShowFeedback(string message)
        {
            FeedbackMessage = message;
            IsSavedNotificationVisible = true;
        }
    }
}
