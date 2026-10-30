using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public interface IInventoryService
    {
        bool IsConnectedToRemoteApi { get; }
        string ApiEndpointUrl { get; }

        event EventHandler? InventoryChanged;

        // Product Catalog
        Task<List<Product>> GetProductsAsync(ProductFilterCriteria? filter = null, int pageIndex = 1, int pageSize = 100);
        Task<int> GetProductCountAsync(ProductFilterCriteria? filter = null);
        Task<Product?> GetProductByIdAsync(string productId);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task<Product?> GetProductByBarcodeAsync(string barcode);
        Task<string> GenerateSkuAsync(string category, string productName);
        Task<bool> IsSkuUniqueAsync(string sku, string? excludeProductId = null);
        Task<bool> IsBarcodeUniqueAsync(string barcode, string? excludeProductId = null);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> ToggleProductActiveAsync(string productId);

        // Stock Inward
        Task<StockReceiveResult> ReceiveStockAsync(StockReceiveDto dto);

        // Stock Outward
        Task<StockIssueResult> RequestOrIssueStockAsync(StockIssueDto dto);

        // Batches & FEFO
        Task<List<StockBatch>> GetBatchesForProductAsync(string productId, string? warehouseId = null);
        Task<List<StockBatch>> GetAllBatchesAsync(string? warehouseId = null);

        // Serial Numbers
        Task<List<StockSerialNumber>> GetSerialNumbersForProductAsync(string productId, string? warehouseId = null, StockSerialStatus? status = null);
        Task<List<StockSerialNumber>> GetAllSerialNumbersAsync(string? warehouseId = null);
        Task<bool> IsSerialNumberUniqueAsync(string productId, string serialNumber);

        // Balances & Movements
        Task<List<StockLocationBalance>> GetLocationBalancesForProductAsync(string productId);
        Task<List<StockMovement>> GetMovementsForProductAsync(string productId, int limit = 50);

        // Approvals
        Task<List<StockApprovalRequest>> GetPendingApprovalsAsync();
        Task<bool> ApproveIssueRequestAsync(string requestId, string approvedBy);
        Task<bool> RejectIssueRequestAsync(string requestId, string rejectedBy, string reason);

        // Topology Lookups
        Task<List<string>> GetCategoriesAsync();
        Task<List<Warehouse>> GetWarehousesAsync();
        Task<List<Rack>> GetRacksByWarehouseAsync(string warehouseId);
        Task<List<RackSection>> GetSectionsByRackAsync(string rackId);
    }
}
