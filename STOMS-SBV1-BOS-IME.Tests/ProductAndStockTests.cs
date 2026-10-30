using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Tests
{
    [TestClass]
    public class ProductAndStockTests
    {
        [TestMethod]
        public async Task Product_AutoGeneratesUniqueSku_AndRejectsDuplicateSku()
        {
            var service = new MockInventoryService();

            // 1. Generate SKU
            string sku1 = await service.GenerateSkuAsync("Mechanical Power Transmission", "Cylindrical Roller Bearings");
            Assert.IsFalse(string.IsNullOrWhiteSpace(sku1));
            Assert.StartsWith("SKU-BRG-", sku1);

            // 2. Test uniqueness validator
            bool isUnique = await service.IsSkuUniqueAsync(sku1);
            Assert.IsTrue(isUnique);

            // 3. Existing SKU check
            bool existingUnique = await service.IsSkuUniqueAsync("SKU-BRG-6205");
            Assert.IsFalse(existingUnique);

            // 4. Create Product with unique SKU
            var newProd = new Product
            {
                Name = "Cylindrical Roller Bearings NU205",
                Sku = sku1,
                Category = "Mechanical Power Transmission",
                Unit = "Pcs",
                MinStockLevel = 10,
                ReorderLevel = 25
            };
            var created = await service.CreateProductAsync(newProd);
            Assert.AreEqual(sku1, created.Sku);

            // 5. Attempting to create duplicate SKU must throw
            var dupProd = new Product
            {
                Name = "Duplicate SKU item",
                Sku = sku1,
                Category = "Mechanical Power Transmission",
                Unit = "Pcs"
            };
            bool threw = false;
            try
            {
                await service.CreateProductAsync(dupProd);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }

        [TestMethod]
        public async Task Product_CreatesProductWithoutBarcode_AndEnforcesUniquenessWhenProvided()
        {
            var service = new MockInventoryService();

            // Product without barcode (barcode is optional)
            var noBarcodeProd = new Product
            {
                Name = "Standard Hex Nut Kit",
                Sku = "SKU-FST-NUT-M10",
                Barcode = null,
                Category = "Fasteners & Hardware",
                Unit = "Pcs"
            };
            var created = await service.CreateProductAsync(noBarcodeProd);
            Assert.IsNotNull(created);
            Assert.IsFalse(created.HasBarcode);
            Assert.AreEqual("No Barcode", created.BarcodeDisplay);

            // Barcode uniqueness
            bool barcodeUnique = await service.IsBarcodeUniqueAsync("8901234567890"); // Belongs to PRD-101
            Assert.IsFalse(barcodeUnique);

            bool newBarcodeUnique = await service.IsBarcodeUniqueAsync("999988887777");
            Assert.IsTrue(newBarcodeUnique);
        }

        [TestMethod]
        public async Task Product_SearchAndMultiFilter_FiltersAccurately()
        {
            var service = new MockInventoryService();

            // 1. Search by SKU
            var skuResults = await service.GetProductsAsync(new ProductFilterCriteria { SearchQuery = "SKU-VAL-DN50" });
            Assert.HasCount(1, skuResults);
            Assert.AreEqual("Stainless Steel Butterfly Valves DN50 PN16", skuResults[0].Name);

            // 2. Search by Barcode
            var barcodeResults = await service.GetProductsAsync(new ProductFilterCriteria { SearchQuery = "8901234567892" });
            Assert.HasCount(1, barcodeResults);
            Assert.AreEqual("SKU-LUB-ISO68", barcodeResults[0].Sku);

            // 3. Filter by Category
            var catResults = await service.GetProductsAsync(new ProductFilterCriteria { Category = "Sensors & Automation" });
            Assert.IsTrue(catResults.All(p => p.Category == "Sensors & Automation"));

            // 4. Filter by Tracking Type
            var serialResults = await service.GetProductsAsync(new ProductFilterCriteria { TrackingType = ProductTrackingType.SerialTracked });
            Assert.IsTrue(serialResults.All(p => p.TrackingType == ProductTrackingType.SerialTracked));
        }

        [TestMethod]
        public async Task StockReceive_QuantityBasedProduct_IncreasesPhysicalAndAvailableStock()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-BOLT-M16");
            Assert.IsNotNull(product);

            decimal initialPhysical = product.PhysicalStock;
            decimal initialAvailable = product.AvailableStock;

            var dto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 300,
                Unit = product.Unit,
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-03",
                Supplier = "Industrial Fasteners India Ltd",
                ReferenceNumber = "PO-2026-991",
                Notes = "Batch delivery acceptance passed"
            };

            var result = await service.ReceiveStockAsync(dto);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(initialPhysical + 300, product.PhysicalStock);
            Assert.AreEqual(initialAvailable + 300, product.AvailableStock);

            // Verify transaction was logged
            var movements = await service.GetMovementsForProductAsync(product.Id);
            Assert.IsTrue(movements.Any(m => m.TransactionId == result.TransactionId && m.Type == MovementType.StockIn));
        }

        [TestMethod]
        public async Task StockReceive_BatchTrackedProduct_CreatesBatchAndEnforcesFutureExpiryWhenRequired()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-LUB-ISO68"); // RequiresExpiry = true
            Assert.IsNotNull(product);

            // 1. Missing batch number must be rejected
            var invalidBatchDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 5,
                Unit = product.Unit,
                BatchNumber = null,
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-02"
            };
            var res1 = await service.ReceiveStockAsync(invalidBatchDto);
            Assert.IsFalse(res1.Success);
            Assert.Contains("Batch/Lot number is required", res1.Message);

            // 2. Past expiry date must be rejected for perishable item
            var pastExpiryDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 5,
                Unit = product.Unit,
                BatchNumber = "BAT-LUB-PAST",
                ExpiryDate = DateTime.Today.AddDays(-5),
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-02"
            };
            var res2 = await service.ReceiveStockAsync(pastExpiryDto);
            Assert.IsFalse(res2.Success);
            Assert.Contains("Expiry date must be in the future", res2.Message);

            // 3. Valid batch with future expiry
            var validDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 10,
                Unit = product.Unit,
                BatchNumber = "BAT-LUB-2027-NEW",
                ExpiryDate = DateTime.Today.AddMonths(18),
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-02",
                Supplier = "Castrol India"
            };
            var res3 = await service.ReceiveStockAsync(validDto);
            Assert.IsTrue(res3.Success);

            // Verify batch was created
            var batches = await service.GetBatchesForProductAsync(product.Id);
            var found = batches.FirstOrDefault(b => b.BatchNumber == "BAT-LUB-2027-NEW");
            Assert.IsNotNull(found);
            Assert.AreEqual(10, found.RemainingQuantity);
            Assert.IsFalse(found.IsExpired);
        }

        [TestMethod]
        public async Task StockReceive_SerialTrackedProduct_EnforcesExactCountAndRejectsDuplicates()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-SNS-420MA"); // SerialTracked
            Assert.IsNotNull(product);

            // 1. Mismatch between quantity and serial count
            var mismatchDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 3,
                Unit = product.Unit,
                SerialNumbers = new List<string> { "SN-NEW-01", "SN-NEW-02" }, // Only 2 instead of 3
                WarehouseId = "WH-01",
                RackId = "RCK-02",
                SectionId = "SEC-05"
            };
            var res1 = await service.ReceiveStockAsync(mismatchDto);
            Assert.IsFalse(res1.Success);
            Assert.Contains("unique serial numbers are required", res1.Message);

            // 2. Duplicate serial numbers in request
            var dupInRequestDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 2,
                Unit = product.Unit,
                SerialNumbers = new List<string> { "SN-TEST-SAME", "SN-TEST-SAME" },
                WarehouseId = "WH-01",
                RackId = "RCK-02",
                SectionId = "SEC-05"
            };
            var res2 = await service.ReceiveStockAsync(dupInRequestDto);
            Assert.IsFalse(res2.Success);
            Assert.Contains("Duplicate serial numbers submitted", res2.Message);

            // 3. Serial that already exists in registry
            var alreadyExistsDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 1,
                Unit = product.Unit,
                SerialNumbers = new List<string> { "SN-SNS-420-0001" }, // Already exists
                WarehouseId = "WH-01",
                RackId = "RCK-02",
                SectionId = "SEC-05"
            };
            var res3 = await service.ReceiveStockAsync(alreadyExistsDto);
            Assert.IsFalse(res3.Success);
            Assert.Contains("already exists in registry", res3.Message);

            // 4. Valid serial receipt
            var validDto = new StockReceiveDto
            {
                ProductId = product.Id,
                Quantity = 2,
                Unit = product.Unit,
                SerialNumbers = new List<string> { "SN-SNS-9999-01", "SN-SNS-9999-02" },
                WarehouseId = "WH-01",
                RackId = "RCK-02",
                SectionId = "SEC-05"
            };
            var res4 = await service.ReceiveStockAsync(validDto);
            Assert.IsTrue(res4.Success);

            // Verify serials are marked Available in registry
            var serials = await service.GetSerialNumbersForProductAsync(product.Id);
            var sn1 = serials.FirstOrDefault(s => s.SerialNumber == "SN-SNS-9999-01");
            Assert.IsNotNull(sn1);
            Assert.AreEqual(StockSerialStatus.Available, sn1.Status);
        }

        [TestMethod]
        public async Task StockIssue_RejectsExcessiveQuantity_AndPreventsNegativeInventory()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-VAL-DN50");
            Assert.IsNotNull(product);

            decimal available = product.AvailableStock; // 42

            // Attempt to issue 100 when only 42 available
            var issueDto = new StockIssueDto
            {
                ProductId = product.Id,
                Quantity = available + 50,
                Unit = product.Unit,
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-01",
                BatchNumber = "BAT-2026-092",
                Reason = "Production Overdraw"
            };

            var result = await service.RequestOrIssueStockAsync(issueDto);
            Assert.IsFalse(result.Success);
            Assert.Contains("exceeds available stock", result.Message);
            Assert.Contains("Negative inventory is strictly prohibited", result.Message);

            // Verify available stock was NOT modified
            Assert.AreEqual(available, product.AvailableStock);
        }

        [TestMethod]
        public async Task StockIssue_BatchSelection_FollowsFEFOOrdering_AndRejectsExpiredBatches()
        {
            var service = new MockInventoryService();

            // Batches for PRD-106 (Adhesives) has an expired batch BAT-2025-ADH-04
            var product = await service.GetProductBySkuAsync("SKU-ADH-EPOXY");
            Assert.IsNotNull(product);

            var batches = await service.GetBatchesForProductAsync(product.Id);
            var expiredBatch = batches.FirstOrDefault(b => b.BatchNumber == "BAT-2025-ADH-04");
            Assert.IsNotNull(expiredBatch);
            Assert.IsTrue(expiredBatch.IsExpired);

            // Attempting to issue the expired batch must fail
            var issueDto = new StockIssueDto
            {
                ProductId = product.Id,
                Quantity = 5,
                Unit = product.Unit,
                WarehouseId = "WH-01",
                RackId = "RCK-02",
                SectionId = "SEC-06",
                BatchNumber = expiredBatch.BatchNumber,
                Reason = "Bonding Assembly"
            };

            var result = await service.RequestOrIssueStockAsync(issueDto);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task StockIssue_ApprovalWorkflow_ReservesStockAndFinalizesOnApproval()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-BRG-6205");
            Assert.IsNotNull(product);

            decimal initialPhysical = product.PhysicalStock; // 450
            decimal initialAvailable = product.AvailableStock; // 450
            decimal initialReserved = product.ReservedStock; // 0

            // Request issue with approval required
            var issueDto = new StockIssueDto
            {
                ProductId = product.Id,
                Quantity = 25,
                Unit = product.Unit,
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-01",
                BatchNumber = "BAT-2026-088",
                Reason = "Special Project Requisition",
                ReferenceNumber = "REQ-SP-001",
                RequiresApproval = true
            };

            var result = await service.RequestOrIssueStockAsync(issueDto);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsPendingApproval);
            Assert.IsNotNull(result.ApprovalRequestId);

            // Check reservation: Physical remains 450, Available drops by 25 (425), Reserved increases by 25
            Assert.AreEqual(initialPhysical, product.PhysicalStock);
            Assert.AreEqual(initialAvailable - 25, product.AvailableStock);
            Assert.AreEqual(initialReserved + 25, product.ReservedStock);

            // Approve the request
            bool approved = await service.ApproveIssueRequestAsync(result.ApprovalRequestId, "Supervisor Lead");
            Assert.IsTrue(approved);

            // Post-approval: Physical stock deducted by 25, Reserved stock released back to 0
            Assert.AreEqual(initialPhysical - 25, product.PhysicalStock);
            Assert.AreEqual(initialReserved, product.ReservedStock);
        }

        [TestMethod]
        public async Task StockIssue_RejectionWorkflow_RestoresAvailableStockAndReleasesReservation()
        {
            var service = new MockInventoryService();
            var product = await service.GetProductBySkuAsync("SKU-BRG-6205");
            Assert.IsNotNull(product);

            decimal initialPhysical = product.PhysicalStock;
            decimal initialAvailable = product.AvailableStock;

            // Request issue with approval required
            var issueDto = new StockIssueDto
            {
                ProductId = product.Id,
                Quantity = 15,
                Unit = product.Unit,
                WarehouseId = "WH-01",
                RackId = "RCK-01",
                SectionId = "SEC-01",
                BatchNumber = "BAT-2026-088",
                Reason = "Maintenance Spare Requisition",
                RequiresApproval = true
            };

            var result = await service.RequestOrIssueStockAsync(issueDto);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.IsPendingApproval);

            // Reject the request
            bool rejected = await service.RejectIssueRequestAsync(result.ApprovalRequestId!, "Supervisor Lead", "Quota exceeded");
            Assert.IsTrue(rejected);

            // Post-rejection: Available stock fully restored, physical stock untouched, reserved stock 0
            Assert.AreEqual(initialPhysical, product.PhysicalStock);
            Assert.AreEqual(initialAvailable, product.AvailableStock);
            Assert.AreEqual(0, product.ReservedStock);
        }

        [TestMethod]
        public async Task ProductManagementViewModel_InitializationAndModalWorkflows()
        {
            var service = new MockInventoryService();
            var vm = new ProductManagementViewModel(service);

            // Ensure catalog is populated
            Assert.IsNotEmpty(vm.Products);
            Assert.IsGreaterThan(0, vm.TotalProductCount);

            // Filter testing
            vm.SearchQuery = "Butterfly";
            await Task.Delay(50); // allow async refresh
            Assert.IsTrue(vm.Products.All(p => p.Name.Contains("Butterfly")));

            // Open Add Product modal
            vm.OpenAddProduct();
            Assert.IsTrue(vm.IsProductFormOpen);
            Assert.IsNotNull(vm.CurrentProductForm);
            Assert.IsFalse(vm.CurrentProductForm.IsEditMode);

            // Close modals
            vm.CloseAllModals();
            Assert.IsFalse(vm.IsProductFormOpen);
            Assert.IsNull(vm.CurrentProductForm);

            // Open Stock Receive modal
            var firstProd = vm.Products.First();
            vm.OpenReceiveStock(firstProd);
            Assert.IsTrue(vm.IsReceiveFormOpen);
            Assert.IsNotNull(vm.CurrentReceiveForm);
            Assert.AreEqual(firstProd.Id, vm.CurrentReceiveForm.SelectedProduct?.Id);

            // Close modals
            vm.CloseAllModals();
            Assert.IsFalse(vm.IsReceiveFormOpen);
        }

        [TestMethod]
        public void ProductManagementView_InitializesWithoutMissingResources()
        {
            Exception? caughtException = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null)
                    {
                        var app = new System.Windows.Application();
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/STOMS-SBV1-BOS-IME;component/Resources/Colors.xaml") });
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/STOMS-SBV1-BOS-IME;component/Resources/DarkTheme.xaml") });
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/STOMS-SBV1-BOS-IME;component/Resources/Icons.xaml") });
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/STOMS-SBV1-BOS-IME;component/Resources/Styles.xaml") });

                        // Add converters
                        app.Resources.Add("BoolToVisibilityConverter", new Converters.BoolToVisibilityConverter());
                        app.Resources.Add("StockStatusToColorConverter", new Converters.StockStatusToColorConverter());
                        app.Resources.Add("StockStatusToBgColorConverter", new Converters.StockStatusToBgColorConverter());
                    }

                    var view = new Views.ProductManagementView
                    {
                        DataContext = new ProductManagementViewModel()
                    };
                    Assert.IsNotNull(view);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caughtException != null)
            {
                Assert.Fail($"ProductManagementView failed to initialize: {caughtException}");
            }
        }
    }
}
