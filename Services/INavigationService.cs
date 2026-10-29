using System;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface INavigationService
    {
        string CurrentViewKey { get; }
        event EventHandler<string>? NavigationChanged;
        void NavigateTo(string viewKey, object? parameter = null);
    }

    public class NavigationService : INavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private string _currentViewKey = "Dashboard";

        public string CurrentViewKey => _currentViewKey;

        public event EventHandler<string>? NavigationChanged;

        public void NavigateTo(string viewKey, object? parameter = null)
        {
            if (_currentViewKey == viewKey && parameter == null) return;
            _currentViewKey = viewKey;
            NavigationChanged?.Invoke(this, viewKey);
        }
    }
}

