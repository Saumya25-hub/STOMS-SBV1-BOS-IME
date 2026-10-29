using System;
using System.Linq;
using System.Windows;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class ThemeService : IThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private bool _isDarkMode = true; // Industrial ERP looks exceptionally sharp in dark mode by default

        public bool IsDarkMode => _isDarkMode;

        public event EventHandler<bool>? ThemeChanged;

        public ThemeService()
        {
            ApplyTheme(_isDarkMode);
        }

        public void ToggleTheme()
        {
            SetTheme(!_isDarkMode);
        }

        public void SetTheme(bool isDark)
        {
            if (_isDarkMode == isDark) return;
            _isDarkMode = isDark;
            ApplyTheme(_isDarkMode);
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
    }
}

