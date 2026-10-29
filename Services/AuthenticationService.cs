using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        public async Task<(bool Success, string Message, OperatorSession? Session)> AuthenticateAsync(string username, string password, string warehouseLocation)
        {
            // Simulate network/security hash evaluation asynchronously
            await Task.Delay(400);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Please provide both Operator ID and access password.", null);
            }

            string u = username.Trim().ToLowerInvariant();
            string p = password.Trim();

            if ((u == "admin" && (p == "admin" || p == "admin123")) ||
                (u == "saumya" && p == "stoms2026") ||
                (u == "jenil" && p == "jenil123") ||
                (u == "hardik" && p == "hardik123") ||
                (u == "operator" && p == "operator"))
            {
                string fullName = u switch
                {
                    "admin" => "Saumya Chaudhari (Founder & Architect)",
                    "saumya" => "Saumya Chaudhari (Vision Architect)",
                    "jenil" => "Jenil (Core Systems Engineer)",
                    "hardik" => "Hardik (Module Architect)",
                    _ => "Certified Inventory Operator"
                };

                string role = u switch
                {
                    "admin" or "saumya" => "System Administrator",
                    "jenil" => "Lead Systems Engineer",
                    "hardik" => "Module Lead",
                    _ => "Inventory Operations"
                };

                var session = new OperatorSession
                {
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    WarehouseLocation = warehouseLocation,
                    AuthToken = Guid.NewGuid().ToString("N"),
                    AuthenticatedAt = DateTime.UtcNow
                };

                return (true, $"Authentication successful. Welcome, {fullName}.", session);
            }

            return (false, "Invalid Operator ID or password. Try 'admin' / 'admin'.", null);
        }
    }
}
