using System;

namespace STOMS_SBV1_BOS_IME.Services
{
    public enum ThemeMode
    {
        Dark,
        Light,
        System
    }

    public interface IThemeService
    {
        bool IsDarkMode { get; }
        ThemeMode CurrentMode { get; }
        event EventHandler<bool>? ThemeChanged;
        void ToggleTheme();
        void SetTheme(bool isDark);
        void SetThemeMode(ThemeMode mode);
        void Initialize();
    }
}
