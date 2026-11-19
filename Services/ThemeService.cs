using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class StomsAppSettings
    {
        public string ThemeMode { get; set; } = "Dark";
        public string Language { get; set; } = "English";
        public string DefaultWarehouse { get; set; } = "WH-01";
        public bool IsSidebarCollapsed { get; set; } = false;
    }

    public class ThemeService : IThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private bool _isDarkMode = true;
        private ThemeMode _currentMode = ThemeMode.Dark;
        private readonly string _settingsFilePath;

        public bool IsDarkMode => _isDarkMode;
        public ThemeMode CurrentMode => _currentMode;

        public event EventHandler<bool>? ThemeChanged;

        public ThemeService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var stomsDir = Path.Combine(appData, "STOMS-BOS");
            if (!Directory.Exists(stomsDir))
            {
                try { Directory.CreateDirectory(stomsDir); } catch { /* ignore */ }
            }
            _settingsFilePath = Path.Combine(stomsDir, "settings.json");

            LoadSettings();
            ApplyTheme(_isDarkMode);
        }

        public void Initialize()
        {
            LoadSettings();
            ApplyTheme(_isDarkMode);
        }

        public void ToggleTheme()
        {
            SetThemeMode(_isDarkMode ? ThemeMode.Light : ThemeMode.Dark);
        }

        public void SetTheme(bool isDark)
        {
            SetThemeMode(isDark ? ThemeMode.Dark : ThemeMode.Light);
        }

        public void SetThemeMode(ThemeMode mode)
        {
            _currentMode = mode;
            bool targetDark = mode switch
            {
                ThemeMode.Light => false,
                ThemeMode.Dark => true,
                ThemeMode.System => IsWindowsSystemDarkMode(),
                _ => true
            };

            _isDarkMode = targetDark;
            ApplyTheme(_isDarkMode);
            SaveSettings();
            ThemeChanged?.Invoke(this, _isDarkMode);
        }

        private void ApplyTheme(bool isDark)
        {
            if (Application.Current == null) return;

            var themeDictionaryUri = isDark
                ? new Uri("Resources/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Resources/LightTheme.xaml", UriKind.Relative);

            var existingThemeDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

            var newThemeDict = new ResourceDictionary { Source = themeDictionaryUri };

            if (existingThemeDict != null)
            {
                var index = Application.Current.Resources.MergedDictionaries.IndexOf(existingThemeDict);
                Application.Current.Resources.MergedDictionaries[index] = newThemeDict;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
            }
        }

        private bool IsWindowsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int lightVal)
                {
                    return lightVal == 0;
                }
            }
            catch
            {
                // Fallback to dark mode
            }
            return true;
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<StomsAppSettings>(json);
                    if (settings != null)
                    {
                        if (Enum.TryParse<ThemeMode>(settings.ThemeMode, true, out var mode))
                        {
                            _currentMode = mode;
                            _isDarkMode = mode switch
                            {
                                ThemeMode.Light => false,
                                ThemeMode.Dark => true,
                                ThemeMode.System => IsWindowsSystemDarkMode(),
                                _ => true
                            };
                        }
                    }
                }
            }
            catch
            {
                _isDarkMode = true;
                _currentMode = ThemeMode.Dark;
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new StomsAppSettings
                {
                    ThemeMode = _currentMode.ToString(),
                    Language = LocalizationService.Instance.CurrentLanguage.ToString()
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Silent catch for sandboxed environments
            }
        }
    }
}
