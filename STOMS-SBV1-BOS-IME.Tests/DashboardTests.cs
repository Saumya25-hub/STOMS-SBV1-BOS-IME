using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STOMS_SBV1_BOS_IME.Models;
using STOMS_SBV1_BOS_IME.Services;
using STOMS_SBV1_BOS_IME.ViewModels;

namespace STOMS_SBV1_BOS_IME.Tests
{
    [TestClass]
    public class DashboardTests
    {
        [TestMethod]
        public async Task DashboardSummary_CalculatesAccurateDistinctProductsAndUnitBreakdowns()
        {
            var service = new MockDashboardService();
            var summary = await service.GetDashboardSummaryAsync();

            Assert.IsNotNull(summary);
            // Verify distinct SKU count is greater than 0
            Assert.IsGreaterThan(0, summary.TotalProducts);
            
            // Verify unit breakdown is present and has non-interchangeable units separated
            Assert.IsGreaterThan(1, summary.UnitBreakdowns.Count);
            Assert.IsTrue(summary.UnitBreakdowns.Any(u => u.Unit == "Pcs"));
            Assert.IsTrue(summary.UnitBreakdowns.Any(u => u.Unit == "Drums"));

            // Verify low stock count respects configured thresholds
            Assert.IsGreaterThanOrEqualTo(1, summary.LowStockItemsCount);

            // Verify near-expiry and expired counts are calculated from batch dates
            Assert.IsGreaterThanOrEqualTo(1, summary.NearExpiryBatchesCount);
            Assert.IsGreaterThanOrEqualTo(1, summary.ExpiredStockBatchesCount);

            // Verify demo data flag is true for offline sandbox
            Assert.IsTrue(summary.IsDemoData);
        }

        [TestMethod]
        public async Task RackTopology_HierarchyAndOccupancy_CalculatesCorrectly()
        {
            var service = new MockDashboardService();
            var racks = await service.GetRacksAsync();

            Assert.IsNotNull(racks);
            Assert.IsGreaterThanOrEqualTo(3, racks.Count);

            // Check Rack 1 (RCK-A-01) with configured capacity
            var rack1 = racks.First(r => r.RackCode == "RCK-A-01");
            Assert.IsTrue(rack1.HasCapacityConfigured);
            Assert.AreEqual(1200m, rack1.MaxCapacity);
            Assert.AreEqual(920m, rack1.OccupiedCapacity);
            Assert.IsTrue(rack1.OccupancyPercentage > 70.0 && rack1.OccupancyPercentage <= 100.0);

            // Check sections count
            Assert.HasCount(4, rack1.Sections);

            // Check Empty Section behavior (Section 4 should be empty)
            var emptySection = rack1.Sections.FirstOrDefault(s => s.SectionCode == "A1-T4");
            Assert.IsNotNull(emptySection);
            Assert.IsTrue(emptySection.IsEmpty);
            Assert.IsEmpty(emptySection.Products);
            Assert.AreEqual("Empty Section (Available for allocation)", emptySection.SummaryDescription);

            // Check Rack 3 (RCK-RAW-09) with UNCONFIGURED capacity
            var rack3 = racks.First(r => r.RackCode == "RCK-RAW-09");
            Assert.IsFalse(rack3.HasCapacityConfigured);
            Assert.AreEqual("Not Configured", rack3.OccupancyDisplay);
            Assert.AreEqual("Capacity not configured", rack3.CapacityDetailsDisplay);
        }

        [TestMethod]
        public async Task RackTopology_StatusPropagation_AggregatesWorstStatus()
        {
            var service = new MockDashboardService();
            var racks = await service.GetRacksAsync();

            // Rack 2 (RCK-B-04) has an expired batch in Tier 2
            var rack2 = racks.First(r => r.RackCode == "RCK-B-04");
            Assert.AreEqual(StockStatus.Expired, rack2.OverallStatus);
            Assert.AreEqual("Expired Stock Detected", rack2.StatusBadgeText);
        }

        [TestMethod]
        public async Task RackFilter_FiltersByWarehouseAndSearchTerm()
        {
            var service = new MockDashboardService();

            // Filter by Warehouse 1
            var wh1Racks = await service.GetRacksAsync(warehouseId: "WH-01");
            Assert.IsTrue(wh1Racks.All(r => r.WarehouseId == "WH-01"));

            // Filter by search query "Valves"
            var searchedRacks = await service.GetRacksAsync(searchQuery: "Valves");
            Assert.HasCount(1, searchedRacks);
            Assert.AreEqual("RCK-A-01", searchedRacks[0].RackCode);
        }

        [TestMethod]
        public async Task StockMovements_ReturnsFormattedTransactions()
        {
            var service = new MockDashboardService();
            var movements = await service.GetRecentMovementsAsync(limit: 5);

            Assert.IsNotNull(movements);
            Assert.IsLessThanOrEqualTo(5, movements.Count);

            var first = movements[0];
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.TransactionId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.ProductName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.InitiatedBy));
            Assert.Contains(first.Unit, first.SignedQuantityDisplay);
        }

        [TestMethod]
        public void NavigationService_NavigatesToRequestedModules()
        {
            var nav = new NavigationService();
            string? capturedView = null;
            nav.NavigationChanged += (s, key) => capturedView = key;

            nav.NavigateTo("Products");
            Assert.AreEqual("Products", nav.CurrentViewKey);
            Assert.AreEqual("Products", capturedView);

            nav.NavigateTo("Dashboard");
            Assert.AreEqual("Dashboard", nav.CurrentViewKey);
        }

        [TestMethod]
        public void MainWindowViewModel_InitializesWithDashboardAndAdminUser()
        {
            var vm = new MainWindowViewModel();

            Assert.IsNotNull(vm.CurrentViewModel);
            Assert.IsInstanceOfType(vm.CurrentViewModel, typeof(DashboardViewModel));
            Assert.AreEqual("Home Dashboard", vm.PageTitle);
            Assert.AreEqual("Saumya Chaudhari", vm.CurrentUser.FullName);
            Assert.AreEqual("System Administrator", vm.CurrentUser.DisplayRole);
        }

        [TestMethod]
        public void MainWindowViewModel_PlaceholderModules_DoNotClaimFakeOperationality()
        {
            var vm = new MainWindowViewModel();
            vm.NavigateToModule("Approvals");

            Assert.IsInstanceOfType(vm.CurrentViewModel, typeof(PlaceholderViewModel));
            var placeholder = (PlaceholderViewModel)vm.CurrentViewModel;

            Assert.AreEqual("MOD-APP-06", placeholder.ModuleCode);
            Assert.Contains("Step 4", placeholder.PlannedRelease);
            Assert.IsNotEmpty(placeholder.UpcomingFeatures);
        }
    }
}
