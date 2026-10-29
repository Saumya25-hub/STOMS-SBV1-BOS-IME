using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    /// <summary>
    /// Service contract for application bootstrap and core engine initialization.
    /// </summary>
    public interface IStartupService
    {
        Task<bool> RunStartupInitializationAsync(IProgress<StartupMilestone> progress, CancellationToken cancellationToken = default);
        void SetSimulatedFailure(bool shouldFail);
        string? LastErrorMessage { get; }
    }
}

