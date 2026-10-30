using System;
using System.Collections.Generic;
using System.Linq;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class FefoAllocationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal RequestedBaseQuantity { get; set; }
        public decimal TotalAllocatedQuantity { get; set; }
        public List<StockAllocation> Allocations { get; set; } = new();
        public List<string> SkippedExpiredBatchNumbers { get; set; } = new();
    }

    public interface IFefoAllocationEngine
    {
        FefoAllocationResult AllocateStock(
            Product product,
            decimal requestedBaseQuantity,
            IEnumerable<StockBatch> availableBatches,
            string? warehouseId = null);
    }

    public class FefoAllocationEngine : IFefoAllocationEngine
    {
        private static FefoAllocationEngine? _instance;
        public static FefoAllocationEngine Instance => _instance ??= new FefoAllocationEngine();

        public FefoAllocationResult AllocateStock(
            Product product,
            decimal requestedBaseQuantity,
            IEnumerable<StockBatch> availableBatches,
            string? warehouseId = null)
        {
            var result = new FefoAllocationResult
            {
                RequestedBaseQuantity = requestedBaseQuantity
            };

            if (requestedBaseQuantity <= 0)
            {
                result.Success = false;
                result.ErrorMessage = "Requested sale quantity must be greater than zero.";
                return result;
            }

            var today = DateTime.Today;

            // 1. Filter batches for the target product and warehouse (if specified)
            var productBatches = availableBatches
                .Where(b => b.ProductId == product.Id)
                .Where(b => string.IsNullOrEmpty(warehouseId) || b.WarehouseId == warehouseId)
                .Where(b => b.Status != StockStatus.Damaged) // Skip damaged/quarantined
                .ToList();

            // 2. Identify and skip expired batches (NEVER allocate expired stock for normal sale!)
            var validBatches = new List<StockBatch>();
            foreach (var batch in productBatches)
            {
                if (batch.ExpiryDate.HasValue && batch.ExpiryDate.Value.Date < today)
                {
                    result.SkippedExpiredBatchNumbers.Add(batch.BatchNumber);
                    continue;
                }

                if (batch.RemainingQuantity > 0)
                {
                    validBatches.Add(batch);
                }
            }

            // 3. Check total available non-expired stock
            var totalAvailableNonExpired = validBatches.Sum(b => b.RemainingQuantity);
            if (totalAvailableNonExpired < requestedBaseQuantity)
            {
                result.Success = false;
                result.ErrorMessage = $"Insufficient stock for {product.Name}. Requested: {requestedBaseQuantity:N0} {product.Unit}, Available non-expired: {totalAvailableNonExpired:N0} {product.Unit}." +
                    (result.SkippedExpiredBatchNumbers.Count > 0 ? $" ({result.SkippedExpiredBatchNumbers.Count} batches quarantined due to expiry)." : "");
                return result;
            }

            // 4. Sort according to FEFO (Earliest Expiry first), fallback to FIFO (Earliest ReceivedDate)
            var sortedBatches = validBatches
                .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1) // Batches with expiration dates come first
                .ThenBy(b => b.ExpiryDate)                   // FEFO: Earliest expiration date first
                .ThenBy(b => b.ReceivedDate)                 // FIFO: Earliest received date next
                .ToList();

            // 5. Allocate requested quantity across batches
            decimal remainingToAllocate = requestedBaseQuantity;

            foreach (var batch in sortedBatches)
            {
                if (remainingToAllocate <= 0) break;

                var allocationQty = Math.Min(batch.RemainingQuantity, remainingToAllocate);

                result.Allocations.Add(new StockAllocation
                {
                    BatchId = batch.BatchId,
                    BatchNumber = batch.BatchNumber,
                    WarehouseId = batch.WarehouseId,
                    RackId = batch.RackId,
                    RackCode = batch.RackCode,
                    ShelfId = batch.ShelfId,
                    ShelfCode = !string.IsNullOrEmpty(batch.ShelfCode) ? batch.ShelfCode : batch.SectionCode,
                    AllocatedBaseQuantity = allocationQty,
                    BaseUnit = product.Unit,
                    ExpiryDate = batch.ExpiryDate
                });

                remainingToAllocate -= allocationQty;
                result.TotalAllocatedQuantity += allocationQty;
            }

            if (remainingToAllocate > 0)
            {
                result.Success = false;
                result.ErrorMessage = "Unable to fulfill total required quantity across available batches.";
                result.Allocations.Clear();
                return result;
            }

            result.Success = true;
            return result;
        }
    }
}
