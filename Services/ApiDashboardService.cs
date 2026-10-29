using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    /// <summary>
    /// Production client communicating with the central ASP.NET Core Web API over HTTP/REST.
    /// Endpoints expected:
    /// GET /api/v1/dashboard/summary
    /// GET /api/v1/warehouses
    /// GET /api/v1/racks?warehouseId={id}&search={term}&status={status}
    /// GET /api/v1/stock/movements?limit={limit}&dateRange={range}&search={term}
    /// GET /api/v1/alerts/active
    /// </summary>
    public class ApiDashboardService : IDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public bool IsConnectedToRemoteApi { get; private set; } = false;
        public string ApiEndpointUrl => _baseUrl;

        public ApiDashboardService(string baseUrl = "https://api.stoms.internal/v1")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(8)
            };
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<DashboardSummary>("dashboard/summary", cancellationToken);
                IsConnectedToRemoteApi = true;
                return result ?? throw new InvalidOperationException("Empty response from API.");
            }
            catch (Exception ex)
            {
                IsConnectedToRemoteApi = false;
                throw new HttpRequestException($"Failed connecting to central API at {_baseUrl}/dashboard/summary: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetFromJsonAsync<List<Warehouse>>("warehouses", cancellationToken);
            return result ?? new List<Warehouse>();
        }

        public async Task<IReadOnlyList<Rack>> GetRacksAsync(
            string? warehouseId = null,
            string? searchQuery = null,
            StockStatus? statusFilter = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"racks?warehouseId={warehouseId}&search={Uri.EscapeDataString(searchQuery ?? string.Empty)}&status={statusFilter}";
            var result = await _httpClient.GetFromJsonAsync<List<Rack>>(url, cancellationToken);
            return result ?? new List<Rack>();
        }

        public async Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(
            int limit = 10,
            MovementDateRange dateRange = MovementDateRange.AllTime,
            string? searchQuery = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"stock/movements?limit={limit}&dateRange={dateRange}&search={Uri.EscapeDataString(searchQuery ?? string.Empty)}";
            var result = await _httpClient.GetFromJsonAsync<List<StockMovement>>(url, cancellationToken);
            return result ?? new List<StockMovement>();
        }

        public async Task<IReadOnlyList<InventoryAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetFromJsonAsync<List<InventoryAlert>>("alerts/active", cancellationToken);
            return result ?? new List<InventoryAlert>();
        }

        public async Task<int> GetPendingApprovalsCountAsync(CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetFromJsonAsync<int>("approvals/pending/count", cancellationToken);
            return result;
        }
    }
}

