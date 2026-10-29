using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string Message, OperatorSession? Session)> AuthenticateAsync(string username, string password, string warehouseLocation);
    }
}
