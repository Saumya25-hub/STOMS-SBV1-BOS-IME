using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IDashboardService
    {
        bool IsConnectedToRemoteApi { get; }
        string ApiEndpointUrl { get; }

        Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Rack>> GetRacksAsync(
            string? warehouseId = null,
            string? searchQuery = null,
            StockStatus? statusFilter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(
            int limit = 10,
            MovementDateRange dateRange = MovementDateRange.AllTime,
            string? searchQuery = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<InventoryAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

        Task<int> GetPendingApprovalsCountAsync(CancellationToken cancellationToken = default);
    }
}

