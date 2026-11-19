using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;

namespace STOMS_SBV1_BOS_IME.Tests
{
    [TestClass]
    public class SmartVisualInventoryTests
    {
        private MockInventoryService _inventory = null!;
        private FefoAllocationEngine _fefo = null!;
        private UnitConversionEngine _unitEngine = null!;
        private SalesService _salesService = null!;

        [TestInitialize]
        public void Setup()
        {
            _inventory = new MockInventoryService();
            _fefo = new FefoAllocationEngine();
            _unitEngine = new UnitConversionEngine();
            _salesService = new SalesService(_inventory, _fefo, _unitEngine);
        }

        [TestMethod]
        public async Task Test01_ProductMaster_CreationWithUnitsBarcodeAndPricing()
        {
            // Scenario:
            // Product = Zozio Chocolate
            // Barcode = 8901234567890
            // Base Unit = PCS, Packaging Unit = BOX (1 BOX = 24 PCS)
            // Selling Price: BOX = ₹480, PCS = ₹20
            // Expiry Alert = 10 Days
            var product = new Product
            {
                Name = "Zozio Chocolate",
                Sku = "SKU-ZOZ-CHOC",
                Barcode = "8901234567890",
                Category = "Confectionery & FMCG",
                Unit = "PCS",
                PackagingUnit = "BOX",
                ConversionFactor = 24m,
                SellingRate = 480m,
                PurchaseRate = 350m,
                ExpiryAlertDays = 10,
                RequiresExpiry = true,
                TrackingType = ProductTrackingType.BatchTracked
            };

            product.Units.Add(new ProductUnit
            {
                UnitName = "PCS",
                ConversionFactor = 1m,
                SellingRate = 20m,
                PurchaseRate = 14.58m,
                IsBaseUnit = true
            });

            product.Units.Add(new ProductUnit
            {
                UnitName = "BOX",
                ConversionFactor = 24m,
                SellingRate = 480m,
                PurchaseRate = 350m,
                IsBaseUnit = false
            });

            var created = await _inventory.CreateProductAsync(product);
            Assert.IsNotNull(created);
            Assert.AreEqual("Zozio Chocolate", created.Name);

            // Verify barcode is searchable
            var fetchedByBarcode = await _inventory.GetProductByBarcodeAsync("8901234567890");
            Assert.IsNotNull(fetchedByBarcode);
            Assert.AreEqual("SKU-ZOZ-CHOC", fetchedByBarcode.Sku);

            // Verify units and rates
            var boxUnit = fetchedByBarcode.FindUnit("BOX");
            Assert.IsNotNull(boxUnit);
            Assert.AreEqual(24m, boxUnit.ConversionFactor);
            Assert.AreEqual(480m, boxUnit.SellingRate);

            var pcsUnit = fetchedByBarcode.FindUnit("PCS");
            Assert.IsNotNull(pcsUnit);
            Assert.AreEqual(1m, pcsUnit.ConversionFactor);
            Assert.AreEqual(20m, pcsUnit.SellingRate);
        }

        [TestMethod]
        public async Task Test02_FirstStockEntry_LiveConversionAndAutoBatch()
        {
            // Scenario:
            // Scan Barcode
            // Quantity = 80 BOX
            // Conversion = 24 PCS -> 1,920 PCS
            // MFG = 01/10/2026, EXP = 30/06/2027
            // Rack = A, Shelf = A1
            var product = await CreateZozioProductAsync();

            var receiveDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 80m,
                Unit = "BOX",
                MfgDate = new DateTime(2026, 10, 1),
                ExpiryDate = new DateTime(2027, 6, 30),
                RackId = "A",
                ShelfCode = "A1",
                Supplier = "Cadbury Mondelez Logistics",
                PurchaseRate = 350m
            };

            var result = await _inventory.ReceiveStockAsync(receiveDto);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1920m, result.ConvertedBaseQuantity, "80 BOX * 24 PCS must equal 1,920 PCS");
            Assert.AreEqual(1920m, result.NewPhysicalStock);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.GeneratedBatchNumber), "Batch ID must be generated automatically");

            // Verify batch created in registry
            var batches = await _inventory.GetBatchesForProductAsync(product.Id);
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(1920m, batches[0].RemainingQuantity);
            Assert.AreEqual("RCK-A", batches[0].RackCode);
            Assert.AreEqual("A1", batches[0].ShelfCode);
        }

        [TestMethod]
        public async Task Test03_SecondStockEntry_SeparateBatchAndAccumulatedStock()
        {
            // Scenario:
            // Scan same barcode
            // Quantity = 50 BOX (50 * 24 = 1,200 PCS)
            // EXP = 30/09/2027
            // Rack = B, Shelf = B1
            // Total Stock = 1,920 + 1,200 = 3,120 PCS
            var product = await CreateZozioProductAsync();

            // First Inward: 80 BOX (1,920 PCS)
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 80m,
                Unit = "BOX",
                MfgDate = new DateTime(2026, 10, 1),
                ExpiryDate = new DateTime(2027, 6, 30),
                RackId = "A",
                ShelfCode = "A1"
            });

            // Second Inward: 50 BOX (1,200 PCS)
            var result2 = await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 50m,
                Unit = "BOX",
                MfgDate = new DateTime(2026, 10, 15),
                ExpiryDate = new DateTime(2027, 9, 30),
                RackId = "B",
                ShelfCode = "B1"
            });

            Assert.IsTrue(result2.Success);
            Assert.AreEqual(1200m, result2.ConvertedBaseQuantity);
            Assert.AreEqual(3120m, result2.NewPhysicalStock, "Total stock must be 3,120 PCS");

            var batches = await _inventory.GetBatchesForProductAsync(product.Id);
            Assert.AreEqual(2, batches.Count, "Two distinct batches must exist");
        }

        [TestMethod]
        public async Task Test04_AutomaticSales_FefoAllocationAndStockDeduction()
        {
            // Scenario:
            // Scan Barcode
            // Select BOX
            // Quantity = 7 BOX (7 * 24 = 168 PCS)
            // Invoice Total = 7 * ₹480 = ₹3,360
            // Deduction = 168 PCS from Batch 1 (earliest expiry 30/06/2027)
            // Batch 1 remaining = 1,752 PCS
            // Batch 2 remaining = 1,200 PCS
            // Total remaining = 2,952 PCS
            var product = await SetupTwoBatchesAsync();

            var invoice = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice, product, 7m, "BOX");

            Assert.AreEqual(3360m, invoice.Subtotal, "7 BOX * ₹480 = ₹3,360");
            Assert.AreEqual(168m, invoice.Items[0].TotalBaseQuantity);

            var saleResult = await _salesService.FinalizeSaleAsync(invoice);
            Assert.IsTrue(saleResult.Success, saleResult.ErrorMessage);

            // Assert Batch Balances
            var batches = await _inventory.GetBatchesForProductAsync(product.Id);
            var batch1 = batches.First(b => b.ExpiryDate == new DateTime(2027, 6, 30));
            var batch2 = batches.First(b => b.ExpiryDate == new DateTime(2027, 9, 30));

            Assert.AreEqual(1752m, batch1.RemainingQuantity, "Batch 1 must have 1,752 PCS remaining (1920 - 168)");
            Assert.AreEqual(1200m, batch2.RemainingQuantity, "Batch 2 must be untouched at 1,200 PCS");

            var refreshedProduct = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(2952m, refreshedProduct!.PhysicalStock, "Total stock must be 2,952 PCS");
        }

        [TestMethod]
        public async Task Test05_MultipleBatchAllocation_SplitsAcrossEarliestBatches()
        {
            // Scenario:
            // Batch 1 remaining = 100 PCS (Expiry: 30/06/2027)
            // Batch 2 remaining = 1200 PCS (Expiry: 30/09/2027)
            // Sale = 168 PCS
            // Expected:
            // Batch 1 deduction = 100 PCS (exhausted to 0)
            // Batch 2 deduction = 68 PCS (remaining 1,132 PCS)
            var product = await SetupTwoBatchesAsync();

            // Set Batch 1 to have only 100 PCS remaining
            var batches = await _inventory.GetBatchesForProductAsync(product.Id);
            var batch1 = batches.First(b => b.ExpiryDate == new DateTime(2027, 6, 30));
            var batch2 = batches.First(b => b.ExpiryDate == new DateTime(2027, 9, 30));
            batch1.RemainingQuantity = 100m;
            product.PhysicalStock = 1300m; // 100 + 1200
            product.AvailableStock = 1300m;

            var invoice = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice, product, 7m, "BOX"); // 168 PCS

            var saleResult = await _salesService.FinalizeSaleAsync(invoice);
            Assert.IsTrue(saleResult.Success, saleResult.ErrorMessage);

            Assert.AreEqual(2, saleResult.TotalAllocations.Count, "Must split across 2 batches");
            Assert.AreEqual(100m, saleResult.TotalAllocations[0].AllocatedBaseQuantity);
            Assert.AreEqual(68m, saleResult.TotalAllocations[1].AllocatedBaseQuantity);

            Assert.AreEqual(0m, batch1.RemainingQuantity);
            Assert.AreEqual(1132m, batch2.RemainingQuantity);
            Assert.AreEqual(1132m, product.PhysicalStock);
        }

        [TestMethod]
        public async Task Test06_PartialUnitSale_NoRoundingErrorAndPackagingBreakdown()
        {
            // Scenario:
            // Available = 80 BOX = 1,920 PCS
            // Customer purchases: 10 PCS
            // Deduction = 10 PCS
            // Remaining = 1,910 PCS
            // Representation: 79 complete BOX + 14 loose PCS
            var product = await CreateZozioProductAsync();
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 80m,
                Unit = "BOX",
                ExpiryDate = new DateTime(2027, 6, 30),
                RackId = "A",
                ShelfCode = "A1"
            });

            var invoice = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice, product, 10m, "PCS");

            var saleResult = await _salesService.FinalizeSaleAsync(invoice);
            Assert.IsTrue(saleResult.Success, saleResult.ErrorMessage);

            var refreshed = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(1910m, refreshed!.PhysicalStock, "Remaining must be exact 1,910 PCS");

            var breakdown = refreshed.GetPackagingBreakdown();
            Assert.AreEqual(79, breakdown.FullPackages);
            Assert.AreEqual(14m, breakdown.LooseQuantity);
            Assert.IsTrue(breakdown.FormattedDescription.Contains("79 BOX + 14 PCS"));
        }

        [TestMethod]
        public async Task Test07_ExpiryValidation_ExpiredBatchesLockedFromSale()
        {
            // Scenario: Expired batches must never be selected for normal sale
            var product = await CreateZozioProductAsync();

            // Receive batch (initially valid, then aged to simulate elapsed time in warehouse)
            var receiveRes = await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 50m,
                Unit = "PCS",
                MfgDate = DateTime.Today.AddYears(-2),
                ExpiryDate = DateTime.Today.AddDays(10),
                RackId = "A",
                ShelfCode = "A1"
            });
            Assert.IsTrue(receiveRes.Success);

            // Simulate that time has elapsed and the batch has expired in warehouse
            var existingBatches = await _inventory.GetBatchesForProductAsync(product.Id);
            var expiredBatch = existingBatches.First();
            expiredBatch.ExpiryDate = DateTime.Today.AddDays(-10);

            // Fresh batch (Expiry next year)
            var freshRes = await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 20m,
                Unit = "PCS",
                MfgDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddYears(1),
                RackId = "B",
                ShelfCode = "B1"
            });
            Assert.IsTrue(freshRes.Success);

            var allBatches = await _inventory.GetAllBatchesAsync();
            var allocResult = _fefo.AllocateStock(product, 25m, allBatches);

            // Should fail because only 20 fresh PCS exist (50 expired are locked!)
            Assert.IsFalse(allocResult.Success);
            Assert.IsTrue(allocResult.ErrorMessage.Contains("Insufficient stock") || allocResult.ErrorMessage.Contains("quarantined"));
            Assert.IsTrue(allocResult.SkippedExpiredBatchNumbers.Count > 0);
        }

        [TestMethod]
        public async Task Test08_InsufficientStock_ClearValidationErrorAndNoDeduction()
        {
            // Scenario: Attempt to sell more than available quantity
            var product = await CreateZozioProductAsync();
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 10m,
                Unit = "PCS",
                ExpiryDate = DateTime.Today.AddYears(1),
                RackId = "A",
                ShelfCode = "A1"
            });

            var invoice = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice, product, 15m, "PCS"); // Needs 15, has 10

            var saleResult = await _salesService.FinalizeSaleAsync(invoice);
            Assert.IsFalse(saleResult.Success);
            Assert.IsTrue(saleResult.ErrorMessage.Contains("Insufficient stock"));

            // Verify stock is untouched at 10 PCS
            var refreshed = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(10m, refreshed!.PhysicalStock);
        }

        [TestMethod]
        public async Task Test09_RackTransfer_PreservesTotalStockAndUpdatesLocations()
        {
            // Scenario: Move 200 PCS from A1 to B1
            var product = await CreateZozioProductAsync();
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 500m,
                Unit = "PCS",
                ExpiryDate = DateTime.Today.AddYears(1),
                RackId = "A",
                ShelfCode = "A1"
            });

            var batch = (await _inventory.GetBatchesForProductAsync(product.Id)).First();

            var transferred = await _inventory.TransferStockAsync(
                product.Id,
                batch.BatchId,
                200m,
                "RCK-A → A1",
                "RCK-B",
                "B1");

            Assert.IsTrue(transferred);

            // Total stock remains invariant at 500 PCS
            var refreshed = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(500m, refreshed!.PhysicalStock, "Total stock must remain strictly unchanged");

            var batches = await _inventory.GetBatchesForProductAsync(product.Id);
            Assert.AreEqual(500m, batches.Sum(b => b.RemainingQuantity));
            Assert.IsTrue(batches.Any(b => b.RackCode == "RCK-B" && b.ShelfCode == "B1" && b.RemainingQuantity == 200m));
        }

        [TestMethod]
        public async Task Test10_SalesReturn_RestoresStockToOriginalBatch()
        {
            // Scenario: Return sold quantity, restoring batch and shelf balance
            var product = await CreateZozioProductAsync();
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 100m,
                Unit = "PCS",
                ExpiryDate = DateTime.Today.AddYears(1),
                RackId = "A",
                ShelfCode = "A1"
            });

            var invoice = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice, product, 25m, "PCS");
            var saleResult = await _salesService.FinalizeSaleAsync(invoice);
            Assert.IsTrue(saleResult.Success);

            // Stock is 75
            Assert.AreEqual(75m, product.PhysicalStock);

            // Cancel Invoice / Return
            var cancelled = await _salesService.CancelInvoiceAsync(invoice.InvoiceId, "Customer Return");
            Assert.IsTrue(cancelled);

            // Stock restored to 100
            var refreshed = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(100m, refreshed!.PhysicalStock);

            var batch = (await _inventory.GetBatchesForProductAsync(product.Id)).First();
            Assert.AreEqual(100m, batch.RemainingQuantity);
        }

        [TestMethod]
        public async Task Test11_BarcodeDuplicate_ValidationEnforced()
        {
            // Scenario: Assigning existing barcode to another product must be rejected
            var product1 = await CreateZozioProductAsync();

            var product2 = new Product
            {
                Name = "Duplicate Barcode Item",
                Sku = "SKU-DUP-01",
                Barcode = product1.Barcode, // Duplicate!
                Category = "General",
                Unit = "PCS"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.CreateProductAsync(product2);
            });
        }

        [TestMethod]
        public async Task Test12_ConcurrentSales_NoOversellingProtection()
        {
            // Scenario: Two sales finalized concurrently when stock is limited
            var product = await CreateZozioProductAsync();
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 100m,
                Unit = "PCS",
                ExpiryDate = DateTime.Today.AddYears(1),
                RackId = "A",
                ShelfCode = "A1"
            });

            var invoice1 = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice1, product, 70m, "PCS");

            var invoice2 = _salesService.CreateDraftInvoice();
            await _salesService.AddProductToInvoiceAsync(invoice2, product, 70m, "PCS");

            // Execute concurrently
            var task1 = _salesService.FinalizeSaleAsync(invoice1);
            var task2 = _salesService.FinalizeSaleAsync(invoice2);

            var results = await Task.WhenAll(task1, task2);

            // One must succeed, and one must fail with insufficient stock (preventing overselling!)
            int successCount = results.Count(r => r.Success);
            int failCount = results.Count(r => !r.Success);

            Assert.AreEqual(1, successCount, "Exactly 1 concurrent sale can succeed");
            Assert.AreEqual(1, failCount, "Second sale must be rejected to prevent negative inventory");

            var refreshed = await _inventory.GetProductByIdAsync(product.Id);
            Assert.AreEqual(30m, refreshed!.PhysicalStock, "Remaining stock must be 30 PCS (100 - 70)");
        }

        // Helper setup methods
        private async Task<Product> CreateZozioProductAsync()
        {
            var product = new Product
            {
                Name = "Zozio Chocolate",
                Sku = $"SKU-ZOZ-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Barcode = "8901234567890",
                Category = "Confectionery & FMCG",
                Unit = "PCS",
                PackagingUnit = "BOX",
                ConversionFactor = 24m,
                SellingRate = 480m,
                PurchaseRate = 350m,
                ExpiryAlertDays = 10,
                RequiresExpiry = true,
                TrackingType = ProductTrackingType.BatchTracked
            };

            product.Units.Add(new ProductUnit
            {
                UnitName = "PCS",
                ConversionFactor = 1m,
                SellingRate = 20m,
                PurchaseRate = 14.58m,
                IsBaseUnit = true
            });

            product.Units.Add(new ProductUnit
            {
                UnitName = "BOX",
                ConversionFactor = 24m,
                SellingRate = 480m,
                PurchaseRate = 350m,
                IsBaseUnit = false
            });

            return await _inventory.CreateProductAsync(product);
        }

        private async Task<Product> SetupTwoBatchesAsync()
        {
            var product = await CreateZozioProductAsync();

            // Batch 1: 80 BOX (1,920 PCS), Exp: 30/06/2027, Rack A, Shelf A1
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 80m,
                Unit = "BOX",
                MfgDate = new DateTime(2026, 10, 1),
                ExpiryDate = new DateTime(2027, 6, 30),
                RackId = "A",
                ShelfCode = "A1"
            });

            // Batch 2: 50 BOX (1,200 PCS), Exp: 30/09/2027, Rack B, Shelf B1
            await _inventory.ReceiveStockAsync(new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 50m,
                Unit = "BOX",
                MfgDate = new DateTime(2026, 10, 15),
                ExpiryDate = new DateTime(2027, 9, 30),
                RackId = "B",
                ShelfCode = "B1"
            });

            return product;
        }
    }
}
