using System;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        event EventHandler<bool>? ThemeChanged;
        void ToggleTheme();
        void SetTheme(bool isDark);
    }
}

