using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    /// <summary>
    /// Production client communicating with the central ASP.NET Core Web API for Product & Stock operations.
    /// Endpoints handled:
    /// GET    /api/v1/products?search={term}&category={cat}&tracking={type}&status={status}&warehouseId={id}
    /// GET    /api/v1/products/{id}
    /// GET    /api/v1/products/sku/{sku}
    /// GET    /api/v1/products/barcode/{barcode}
    /// GET    /api/v1/products/generate-sku?category={cat}&name={name}
    /// POST   /api/v1/products
    /// PUT    /api/v1/products/{id}
    /// POST   /api/v1/stock/receive
    /// POST   /api/v1/stock/issue
    /// GET    /api/v1/stock/batches?productId={id}&warehouseId={whId}
    /// GET    /api/v1/stock/serials?productId={id}&status={status}
    /// GET    /api/v1/stock/approvals/pending
    /// POST   /api/v1/stock/approvals/{id}/approve
    /// POST   /api/v1/stock/approvals/{id}/reject
    /// </summary>
    public class ApiInventoryService : IInventoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public bool IsConnectedToRemoteApi { get; private set; }
        public string ApiEndpointUrl => _baseUrl;

        public event EventHandler? InventoryChanged;

        public ApiInventoryService(string baseUrl = "https://api.stoms.internal/v1")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<List<Product>> GetProductsAsync(ProductFilterCriteria? filter = null, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"pageIndex={pageIndex}",
                    $"pageSize={pageSize}"
                };

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchQuery)) queryParams.Add($"search={Uri.EscapeDataString(filter.SearchQuery)}");
                    if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "All Categories") queryParams.Add($"category={Uri.EscapeDataString(filter.Category)}");
                    if (filter.TrackingType.HasValue) queryParams.Add($"trackingType={(int)filter.TrackingType.Value}");
                    if (filter.Status.HasValue) queryParams.Add($"status={(int)filter.Status.Value}");
                    if (!string.IsNullOrWhiteSpace(filter.WarehouseId) && filter.WarehouseId != "All Warehouses") queryParams.Add($"warehouseId={Uri.EscapeDataString(filter.WarehouseId)}");
                    if (filter.IsActive.HasValue) queryParams.Add($"isActive={filter.IsActive.Value}");
                }

                string url = $"products?{string.Join("&", queryParams)}";
                var result = await _httpClient.GetFromJsonAsync<List<Product>>(url);
                IsConnectedToRemoteApi = true;
                return result ?? new List<Product>();
            }
            catch
            {
                IsConnectedToRemoteApi = false;
                throw;
            }
        }

        public async Task<int> GetProductCountAsync(ProductFilterCriteria? filter = null)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<int>("products/count");
                return result;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            return await _httpClient.GetFromJsonAsync<Product>($"products/{Uri.EscapeDataString(productId)}");
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            return await _httpClient.GetFromJsonAsync<Product>($"products/sku/{Uri.EscapeDataString(sku)}");
        }

        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            return await _httpClient.GetFromJsonAsync<Product>($"products/barcode/{Uri.EscapeDataString(barcode)}");
        }

        public async Task<string> GenerateSkuAsync(string category, string productName)
        {
            var result = await _httpClient.GetStringAsync($"products/generate-sku?category={Uri.EscapeDataString(category)}&name={Uri.EscapeDataString(productName)}");
            return result.Trim('\"');
        }

        public async Task<bool> IsSkuUniqueAsync(string sku, string? excludeProductId = null)
        {
            string url = $"products/validate-sku?sku={Uri.EscapeDataString(sku)}";
            if (!string.IsNullOrEmpty(excludeProductId)) url += $"&excludeId={Uri.EscapeDataString(excludeProductId)}";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, string? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return true;
            string url = $"products/validate-barcode?barcode={Uri.EscapeDataString(barcode)}";
            if (!string.IsNullOrEmpty(excludeProductId)) url += $"&excludeId={Uri.EscapeDataString(excludeProductId)}";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var response = await _httpClient.PostAsJsonAsync("products", product);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<Product>() ?? product;
            InventoryChanged?.Invoke(this, EventArgs.Empty);
            return created;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var response = await _httpClient.PutAsJsonAsync($"products/{product.Id}", product);
            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<Product>() ?? product;
            InventoryChanged?.Invoke(this, EventArgs.Empty);
            return updated;
        }

        public async Task<bool> ToggleProductActiveAsync(string productId)
        {
            var response = await _httpClient.PutAsync($"products/{productId}/toggle-active", null);
            InventoryChanged?.Invoke(this, EventArgs.Empty);
            return response.IsSuccessStatusCode;
        }

        public async Task<StockReceiveResult> ReceiveStockAsync(StockReceiveDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("stock/receive", dto);
            var result = await response.Content.ReadFromJsonAsync<StockReceiveResult>();
            if (result?.Success == true)
            {
                InventoryChanged?.Invoke(this, EventArgs.Empty);
            }
            return result ?? new StockReceiveResult { Success = false, Message = "Failed parsing API response" };
        }

        public async Task<StockIssueResult> RequestOrIssueStockAsync(StockIssueDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("stock/issue", dto);
            var result = await response.Content.ReadFromJsonAsync<StockIssueResult>();
            if (result?.Success == true)
            {
                InventoryChanged?.Invoke(this, EventArgs.Empty);
            }
            return result ?? new StockIssueResult { Success = false, Message = "Failed parsing API response" };
        }

        public async Task<List<StockBatch>> GetBatchesForProductAsync(string productId, string? warehouseId = null)
        {
            string url = $"stock/batches?productId={Uri.EscapeDataString(productId)}";
            if (!string.IsNullOrEmpty(warehouseId) && warehouseId != "All Warehouses") url += $"&warehouseId={Uri.EscapeDataString(warehouseId)}";
            var result = await _httpClient.GetFromJsonAsync<List<StockBatch>>(url);
            return result ?? new List<StockBatch>();
        }

        public async Task<List<StockBatch>> GetAllBatchesAsync(string? warehouseId = null)
        {
            string url = "stock/batches";
            if (!string.IsNullOrEmpty(warehouseId) && warehouseId != "All Warehouses") url += $"?warehouseId={Uri.EscapeDataString(warehouseId)}";
            var result = await _httpClient.GetFromJsonAsync<List<StockBatch>>(url);
            return result ?? new List<StockBatch>();
        }

        public async Task<List<StockSerialNumber>> GetSerialNumbersForProductAsync(string productId, string? warehouseId = null, StockSerialStatus? status = null)
        {
            string url = $"stock/serials?productId={Uri.EscapeDataString(productId)}";
            if (!string.IsNullOrEmpty(warehouseId) && warehouseId != "All Warehouses") url += $"&warehouseId={Uri.EscapeDataString(warehouseId)}";
            if (status.HasValue) url += $"&status={(int)status.Value}";
            var result = await _httpClient.GetFromJsonAsync<List<StockSerialNumber>>(url);
            return result ?? new List<StockSerialNumber>();
        }

        public async Task<List<StockSerialNumber>> GetAllSerialNumbersAsync(string? warehouseId = null)
        {
            string url = "stock/serials";
            if (!string.IsNullOrEmpty(warehouseId) && warehouseId != "All Warehouses") url += $"?warehouseId={Uri.EscapeDataString(warehouseId)}";
            var result = await _httpClient.GetFromJsonAsync<List<StockSerialNumber>>(url);
            return result ?? new List<StockSerialNumber>();
        }

        public async Task<bool> IsSerialNumberUniqueAsync(string productId, string serialNumber)
        {
            return await _httpClient.GetFromJsonAsync<bool>($"stock/serials/validate?productId={productId}&serialNumber={Uri.EscapeDataString(serialNumber)}");
        }

        public async Task<List<StockLocationBalance>> GetLocationBalancesForProductAsync(string productId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<StockLocationBalance>>($"stock/balances/{productId}");
            return result ?? new List<StockLocationBalance>();
        }

        public async Task<List<StockMovement>> GetMovementsForProductAsync(string productId, int limit = 50)
        {
            var result = await _httpClient.GetFromJsonAsync<List<StockMovement>>($"stock/movements?productId={productId}&limit={limit}");
            return result ?? new List<StockMovement>();
        }

        public async Task<List<StockApprovalRequest>> GetPendingApprovalsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<StockApprovalRequest>>("stock/approvals/pending");
            return result ?? new List<StockApprovalRequest>();
        }

        public async Task<bool> ApproveIssueRequestAsync(string requestId, string approvedBy)
        {
            var response = await _httpClient.PostAsJsonAsync($"stock/approvals/{requestId}/approve", new { approvedBy });
            if (response.IsSuccessStatusCode)
            {
                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public async Task<bool> RejectIssueRequestAsync(string requestId, string rejectedBy, string reason)
        {
            var response = await _httpClient.PostAsJsonAsync($"stock/approvals/{requestId}/reject", new { rejectedBy, reason });
            if (response.IsSuccessStatusCode)
            {
                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<string>>("products/categories");
            return result ?? new List<string>();
        }

        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<Warehouse>>("warehouses");
            return result ?? new List<Warehouse>();
        }

        public async Task<List<Rack>> GetRacksByWarehouseAsync(string warehouseId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<Rack>>($"warehouses/{warehouseId}/racks");
            return result ?? new List<Rack>();
        }

        public async Task<List<RackSection>> GetSectionsByRackAsync(string rackId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<RackSection>>($"racks/{rackId}/sections");
            return result ?? new List<RackSection>();
        }

        public async Task<bool> TransferStockAsync(string productId, string batchId, decimal quantity, string sourceLocation, string destinationRackCode, string destinationShelfCode, string destinationWarehouseId = "WH-01")
        {
            var response = await _httpClient.PostAsJsonAsync("stock/transfer", new { productId, batchId, quantity, sourceLocation, destinationRackCode, destinationShelfCode, destinationWarehouseId });
            return response.IsSuccessStatusCode;
        }

        public async Task<List<RackShelf>> GetShelvesByRackAsync(string rackIdOrCode)
        {
            var result = await _httpClient.GetFromJsonAsync<List<RackShelf>>($"racks/{rackIdOrCode}/shelves");
            return result ?? new List<RackShelf>();
        }
    }
}

