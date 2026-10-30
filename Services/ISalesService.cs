using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class FinalizeSaleResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public SaleInvoice? Invoice { get; set; }
        public List<StockAllocation> TotalAllocations { get; set; } = new();
    }

    public interface ISalesService
    {
        event EventHandler<SaleInvoice>? SaleFinalized;
        event EventHandler<SaleInvoice>? SaleCancelled;

        SaleInvoice CreateDraftInvoice();
        Task<SaleInvoiceItem> AddProductToInvoiceAsync(SaleInvoice invoice, Product product, decimal quantity, string unitName);
        Task<SaleInvoiceItem?> ScanBarcodeToInvoiceAsync(SaleInvoice invoice, string barcode, decimal quantity = 1m);
        void RemoveItem(SaleInvoice invoice, SaleInvoiceItem item);
        Task<FinalizeSaleResult> FinalizeSaleAsync(SaleInvoice invoice);
        Task<bool> CancelInvoiceAsync(string invoiceId, string reason = "Customer Cancellation");
        Task<List<SaleInvoice>> GetInvoicesAsync();
        Task<SaleInvoice?> GetInvoiceByIdAsync(string invoiceId);
    }

    public class SalesService : ISalesService
    {
        private static SalesService? _instance;
        public static SalesService Instance => _instance ??= new SalesService(
            MockInventoryService.Instance,
            FefoAllocationEngine.Instance,
            UnitConversionEngine.Instance);

        private readonly IInventoryService _inventoryService;
        private readonly IFefoAllocationEngine _fefoEngine;
        private readonly IUnitConversionEngine _conversionEngine;
        private readonly List<SaleInvoice> _invoices = new();
        private readonly SemaphoreSlim _saleLock = new(1, 1);

        public event EventHandler<SaleInvoice>? SaleFinalized;
        public event EventHandler<SaleInvoice>? SaleCancelled;

        public SalesService(
            IInventoryService inventoryService,
            IFefoAllocationEngine fefoEngine,
            IUnitConversionEngine conversionEngine)
        {
            _inventoryService = inventoryService;
            _fefoEngine = fefoEngine;
            _conversionEngine = conversionEngine;
        }

        public SaleInvoice CreateDraftInvoice()
        {
            return new SaleInvoice();
        }

        public Task<SaleInvoiceItem> AddProductToInvoiceAsync(SaleInvoice invoice, Product product, decimal quantity, string unitName)
        {
            var configuredUnit = product.FindUnit(unitName);
            var conversionFactor = configuredUnit?.ConversionFactor ?? _conversionEngine.GetDefaultConversionFactor(product.Unit, unitName);
            var unitPrice = configuredUnit?.SellingRate > 0 ? configuredUnit.SellingRate : product.SellingRate * conversionFactor;

            var existing = invoice.Items.FirstOrDefault(i => i.ProductId == product.Id && string.Equals(i.SelectedUnit, unitName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Quantity += quantity;
                invoice.NotifyTotalsChanged();
                return Task.FromResult(existing);
            }

            var item = new SaleInvoiceItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                Barcode = product.Barcode,
                Quantity = quantity,
                SelectedUnit = unitName,
                ConversionFactor = conversionFactor,
                UnitPrice = unitPrice,
                GstRate = product.GstRate
            };

            invoice.Items.Add(item);
            invoice.NotifyTotalsChanged();
            return Task.FromResult(item);
        }

        public async Task<SaleInvoiceItem?> ScanBarcodeToInvoiceAsync(SaleInvoice invoice, string barcode, decimal quantity = 1m)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            var product = await _inventoryService.GetProductByBarcodeAsync(barcode.Trim());
            if (product == null)
            {
                // Try searching by SKU
                product = await _inventoryService.GetProductBySkuAsync(barcode.Trim());
            }

            if (product == null) return null;

            // Check if scanned barcode matches a specific packaging unit
            var matchedBarcode = product.Barcodes.FirstOrDefault(b => string.Equals(b.Code, barcode.Trim(), StringComparison.OrdinalIgnoreCase));
            var unit = matchedBarcode != null ? matchedBarcode.Unit : (!string.IsNullOrWhiteSpace(product.PackagingUnit) ? product.PackagingUnit : product.Unit);

            return await AddProductToInvoiceAsync(invoice, product, quantity, unit);
        }

        public void RemoveItem(SaleInvoice invoice, SaleInvoiceItem item)
        {
            invoice.Items.Remove(item);
            invoice.NotifyTotalsChanged();
        }

        public async Task<FinalizeSaleResult> FinalizeSaleAsync(SaleInvoice invoice)
        {
            var result = new FinalizeSaleResult { Invoice = invoice };

            if (invoice.Items.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "Cannot finalize an empty invoice. Add at least one item.";
                return result;
            }

            if (invoice.Status == InvoiceStatus.Confirmed)
            {
                result.Success = false;
                result.ErrorMessage = "This invoice is already confirmed and stock has been deducted.";
                return result;
            }

            // CRUCIAL: Protect against race conditions and concurrent sales with atomic semaphore lock (Part 18, Test 12)
            await _saleLock.WaitAsync();
            try
            {
                var allBatches = await _inventoryService.GetAllBatchesAsync();
                var itemAllocations = new Dictionary<SaleInvoiceItem, List<StockAllocation>>();

                // Step 1: Validate and allocate FEFO batches for all line items BEFORE making any changes
                foreach (var item in invoice.Items)
                {
                    var product = await _inventoryService.GetProductByIdAsync(item.ProductId);
                    if (product == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Product '{item.ProductName}' not found in catalog.";
                        return result;
                    }

                    var requiredBaseQty = item.TotalBaseQuantity;
                    var allocationResult = _fefoEngine.AllocateStock(product, requiredBaseQty, allBatches);

                    if (!allocationResult.Success)
                    {
                        result.Success = false;
                        result.ErrorMessage = allocationResult.ErrorMessage;
                        return result;
                    }

                    itemAllocations[item] = allocationResult.Allocations;
                }

                // Step 2: Atomic Execution: Deduct stock from batches, shelves, and products
                foreach (var (item, allocations) in itemAllocations)
                {
                    item.Allocations = allocations;
                    result.TotalAllocations.AddRange(allocations);

                    var product = await _inventoryService.GetProductByIdAsync(item.ProductId);
                    if (product == null) continue;

                    foreach (var alloc in allocations)
                    {
                        var targetBatch = allBatches.FirstOrDefault(b => b.BatchId == alloc.BatchId);
                        if (targetBatch != null)
                        {
                            targetBatch.RemainingQuantity = Math.Max(0m, targetBatch.RemainingQuantity - alloc.AllocatedBaseQuantity);
                        }

                        // Deduct from product physical & available stock
                        product.PhysicalStock = Math.Max(0m, product.PhysicalStock - alloc.AllocatedBaseQuantity);
                        product.AvailableStock = Math.Max(0m, product.AvailableStock - alloc.AllocatedBaseQuantity);

                        // Record permanent movement ledger entry (Part 10)
                        if (_inventoryService is MockInventoryService mockInventory)
                        {
                            mockInventory.RecordDirectMovement(new StockMovement
                            {
                                TransactionId = $"MOV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
                                Timestamp = DateTime.Now,
                                ProductId = product.Id,
                                ProductName = product.Name,
                                Sku = product.Sku,
                                BatchId = alloc.BatchId,
                                BatchNumber = alloc.BatchNumber,
                                Type = MovementType.Sale,
                                Quantity = alloc.AllocatedBaseQuantity,
                                BaseQuantity = alloc.AllocatedBaseQuantity,
                                Unit = product.Unit,
                                PreviousBalance = product.PhysicalStock + alloc.AllocatedBaseQuantity,
                                NewBalance = product.PhysicalStock,
                                SourceLocation = $"{alloc.RackCode} → {alloc.ShelfCode}",
                                DestinationLocation = $"Customer: {invoice.CustomerName}",
                                ReferenceDocument = invoice.InvoiceNumber,
                                InitiatedBy = invoice.CashierName,
                                Status = MovementStatus.Completed,
                                Remarks = $"POS Sale: {item.Quantity} {item.SelectedUnit} (Batch: {alloc.BatchNumber})"
                            });

                            // Update rack shelf capacity / occupancy
                            mockInventory.DeductShelfStock(alloc.RackCode, alloc.ShelfCode, alloc.AllocatedBaseQuantity);
                        }
                    }

                    await _inventoryService.UpdateProductAsync(product);
                }

                invoice.Status = InvoiceStatus.Confirmed;
                if (!_invoices.Contains(invoice))
                {
                    _invoices.Add(invoice);
                }

                result.Success = true;
                SaleFinalized?.Invoke(this, invoice);
                return result;
            }
            finally
            {
                _saleLock.Release();
            }
        }

        public async Task<bool> CancelInvoiceAsync(string invoiceId, string reason = "Customer Cancellation")
        {
            await _saleLock.WaitAsync();
            try
            {
                var invoice = _invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice == null || invoice.Status != InvoiceStatus.Confirmed) return false;

                var allBatches = await _inventoryService.GetAllBatchesAsync();

                // Reverse all allocations back to their original batches and shelves (Part 18, Test 10)
                foreach (var item in invoice.Items)
                {
                    var product = await _inventoryService.GetProductByIdAsync(item.ProductId);
                    if (product == null) continue;

                    foreach (var alloc in item.Allocations)
                    {
                        var targetBatch = allBatches.FirstOrDefault(b => b.BatchId == alloc.BatchId);
                        if (targetBatch != null)
                        {
                            targetBatch.RemainingQuantity += alloc.AllocatedBaseQuantity;
                        }

                        product.PhysicalStock += alloc.AllocatedBaseQuantity;
                        product.AvailableStock += alloc.AllocatedBaseQuantity;

                        if (_inventoryService is MockInventoryService mockInventory)
                        {
                            mockInventory.RecordDirectMovement(new StockMovement
                            {
                                TransactionId = $"MOV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
                                Timestamp = DateTime.Now,
                                ProductId = product.Id,
                                ProductName = product.Name,
                                Sku = product.Sku,
                                BatchId = alloc.BatchId,
                                BatchNumber = alloc.BatchNumber,
                                Type = MovementType.SalesReturn,
                                Quantity = alloc.AllocatedBaseQuantity,
                                BaseQuantity = alloc.AllocatedBaseQuantity,
                                Unit = product.Unit,
                                PreviousBalance = product.PhysicalStock - alloc.AllocatedBaseQuantity,
                                NewBalance = product.PhysicalStock,
                                SourceLocation = $"Customer Return: {invoice.CustomerName}",
                                DestinationLocation = $"{alloc.RackCode} → {alloc.ShelfCode}",
                                ReferenceDocument = invoice.InvoiceNumber,
                                InitiatedBy = invoice.CashierName,
                                Status = MovementStatus.Completed,
                                Remarks = $"Sale Reversal / Return: {reason}"
                            });

                            mockInventory.RestoreShelfStock(alloc.RackCode, alloc.ShelfCode, alloc.AllocatedBaseQuantity);
                        }
                    }

                    await _inventoryService.UpdateProductAsync(product);
                }

                invoice.Status = InvoiceStatus.Cancelled;
                SaleCancelled?.Invoke(this, invoice);
                return true;
            }
            finally
            {
                _saleLock.Release();
            }
        }

        public Task<List<SaleInvoice>> GetInvoicesAsync()
        {
            return Task.FromResult(_invoices.OrderByDescending(i => i.InvoiceDate).ToList());
        }

        public Task<SaleInvoice?> GetInvoiceByIdAsync(string invoiceId)
        {
            return Task.FromResult(_invoices.FirstOrDefault(i => i.InvoiceId == invoiceId));
        }
    }
}
