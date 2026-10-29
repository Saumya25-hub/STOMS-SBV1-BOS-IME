using System;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IAuthService
    {
        UserProfile CurrentUser { get; }
        bool IsAuthenticated { get; }
        event EventHandler<UserProfile?>? AuthStateChanged;
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private UserProfile _currentUser;

        public UserProfile CurrentUser => _currentUser;
        public bool IsAuthenticated => true;

        public event EventHandler<UserProfile?>? AuthStateChanged;

        public AuthService()
        {
            // Initialized with authenticated session for core engineering / operations
            _currentUser = new UserProfile
            {
                Id = "USR-001",
                Username = "saumya.c",
                FullName = "Saumya Chaudhari",
                Role = UserRole.Administrator,
                AssignedWarehouse = "Global Logistics Terminal",
                LastLogin = DateTime.Now.AddHours(-2)
            };
        }

        public void Logout()
        {
            // Clean logout handling for UI
            AuthStateChanged?.Invoke(this, null);
        }
    }
}

