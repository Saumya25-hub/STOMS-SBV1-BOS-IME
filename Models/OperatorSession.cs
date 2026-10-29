namespace STOMS_SBV1_BOS_IME.Models
{
    /// <summary>
    /// Represents an authenticated operator session in the IMS ecosystem.
    /// </summary>
    public class OperatorSession
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string WarehouseLocation { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;
        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthToken);
    }
}

