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
    public class WarehouseAndRackTests
    {
        private MockInventoryService _inventory = null!;
        private LocationSuggestionEngine _suggestionEngine = null!;

        [TestInitialize]
        public void Setup()
        {
            _inventory = new MockInventoryService();
            _suggestionEngine = LocationSuggestionEngine.Instance;
        }

        [TestMethod]
        public async Task Test01_WarehouseMaster_CreateMultipleWarehouses_ValidatesUniqueCodes()
        {
            // 1. Create Facility 1
            var wh1 = new Warehouse
            {
                Code = "WH-NORTH-01",
                Name = "Northern Assembly & Spares Depot",
                CityLocation = "Noida Sector 62",
                WarehouseType = "Finished Goods Depot",
                TotalFloorArea = 8500,
                TotalCapacity = 1500,
                Unit = "Tons",
                IsActive = true
            };
            var created1 = await _inventory.CreateWarehouseAsync(wh1);
            Assert.IsNotNull(created1);
            Assert.AreEqual("WH-NORTH-01", created1.Code);

            // 2. Create Facility 2
            var wh2 = new Warehouse
            {
                Code = "WH-WEST-02",
                Name = "Western Cold Chain Logistics Hub",
                CityLocation = "Kandla Port SEZ",
                WarehouseType = "Cold Storage",
                TotalFloorArea = 6200,
                TotalCapacity = 800,
                Unit = "Tons",
                IsActive = true
            };
            var created2 = await _inventory.CreateWarehouseAsync(wh2);
            Assert.IsNotNull(created2);
            Assert.AreEqual("WH-WEST-02", created2.Code);

            // 3. Duplicate code rejection
            var duplicateWh = new Warehouse
            {
                Code = "WH-NORTH-01",
                Name = "Duplicate Facility Attempt"
            };
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.CreateWarehouseAsync(duplicateWh);
            });
        }

        [TestMethod]
        public async Task Test02_WarehouseMaster_DeletionProtection_BlocksDeleteWhenWarehouseContainsStock()
        {
            // WH-01 has active physical stock (pre-seeded ball bearings, valves, etc.)
            var wh01 = await _inventory.GetWarehouseByIdAsync("WH-01");
            Assert.IsNotNull(wh01);

            // Attempting to delete WH-01 must throw InvalidOperationException
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.DeleteWarehouseAsync(wh01.Id);
            });

            Assert.IsTrue(ex.Message.Contains("physical stock") || ex.Message.Contains("active"));
        }

        [TestMethod]
        public async Task Test03_RackMaster_AddMultipleRacksToWarehouse_ValidatesUniqueCodes()
        {
            // Add RCK-X1 to WH-02
            var rack1 = new Rack
            {
                RackCode = "RCK-X1",
                Name = "Heavy Duty Pallet Bay X1",
                WarehouseId = "WH-02",
                Zone = "Zone X",
                RackType = "Selective Pallet",
                MaxCapacity = 1500,
                NumberOfLevels = 4,
                IsActive = true
            };
            var createdRack1 = await _inventory.CreateRackAsync(rack1);
            Assert.IsNotNull(createdRack1);

            // Duplicate rack code in same warehouse must be rejected
            var duplicateRack = new Rack
            {
                RackCode = "RCK-X1",
                Name = "Duplicate Rack Code",
                WarehouseId = "WH-02"
            };
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.CreateRackAsync(duplicateRack);
            });
        }

        [TestMethod]
        public async Task Test04_RackMaster_DeletionProtection_BlocksDeleteWhenRackContainsStock()
        {
            // RCK-01 contains ball bearings & butterfly valves in seed data
            var rack01 = await _inventory.GetRackByIdAsync("RCK-01");
            Assert.IsNotNull(rack01);
            Assert.IsTrue(rack01.TotalStockQuantity > 0, "RCK-01 must contain stock for test");

            // Attempting to delete RCK-01 must be blocked
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.DeleteRackAsync(rack01.Id);
            });

            Assert.IsTrue(ex.Message.Contains("contains active stock"));
        }

        [TestMethod]
        public async Task Test05_RackLayout_FreePositioning_MoveRacksSideBySideAndSeparate()
        {
            var racks = await _inventory.GetRacksByWarehouseAsync("WH-01");
            Assert.IsTrue(racks.Count >= 2);

            var rackA = racks[0];
            var rackB = racks[1];

            // 1. Arrange Side-by-Side: Rack A at (40, 100), Rack B right next to it at (300, 100)
            rackA.CanvasX = 40;
            rackA.CanvasY = 100;
            rackA.Width = 250;

            rackB.CanvasX = 300; // side-by-side with 10px spacing
            rackB.CanvasY = 100;
            rackB.Width = 250;

            await _inventory.SaveRackLayoutCoordinatesAsync("WH-01", new List<Rack> { rackA, rackB });

            var reloaded = await _inventory.GetRacksByWarehouseAsync("WH-01");
            Assert.AreEqual(40, reloaded.First(r => r.Id == rackA.Id).CanvasX);
            Assert.AreEqual(300, reloaded.First(r => r.Id == rackB.Id).CanvasX);

            // 2. Separate into different warehouse zones
            rackB.CanvasX = 900;
            rackB.CanvasY = 600;
            await _inventory.SaveRackLayoutCoordinatesAsync("WH-01", new List<Rack> { rackB });

            var separated = await _inventory.GetRacksByWarehouseAsync("WH-01");
            Assert.AreEqual(900, separated.First(r => r.Id == rackB.Id).CanvasX);
            Assert.AreEqual(600, separated.First(r => r.Id == rackB.Id).CanvasY);
        }

        [TestMethod]
        public async Task Test06_RackLayout_ResizeAndRotate_UpdatesDimensionsWithoutChangingStock()
        {
            var rack = await _inventory.GetRackByIdAsync("RCK-01");
            Assert.IsNotNull(rack);
            decimal originalStock = rack.TotalStockQuantity;

            // Rotate 90 degrees and resize
            rack.Rotation = 90;
            rack.Width = 320;
            rack.Height = 180;

            await _inventory.UpdateRackAsync(rack);

            var updated = await _inventory.GetRackByIdAsync("RCK-01");
            Assert.IsNotNull(updated);
            Assert.AreEqual(90, updated.Rotation);
            Assert.AreEqual(320, updated.Width);
            Assert.AreEqual(180, updated.Height);

            // Critical rule: visual movement/rotation/resizing must not change stock quantities
            Assert.AreEqual(originalStock, updated.TotalStockQuantity, "Visual modifications must never change stock!");
        }

        [TestMethod]
        public async Task Test07_RackLayout_SaveAndRestore_PersistsCoordinatesAndRevision()
        {
            var wh = await _inventory.GetWarehouseByIdAsync("WH-01");
            Assert.IsNotNull(wh);
            int initialRev = wh.LayoutRevision;

            var racks = await _inventory.GetRacksByWarehouseAsync("WH-01");
            racks[0].CanvasX = 540;
            racks[0].CanvasY = 320;

            await _inventory.SaveRackLayoutCoordinatesAsync("WH-01", racks);

            var whAfter = await _inventory.GetWarehouseByIdAsync("WH-01");
            Assert.IsNotNull(whAfter);
            Assert.IsTrue(whAfter.LayoutRevision > initialRev, "Layout revision counter must increment on save");

            var reloadedRacks = await _inventory.GetRacksByWarehouseAsync("WH-01");
            Assert.AreEqual(540, reloadedRacks[0].CanvasX);
            Assert.AreEqual(320, reloadedRacks[0].CanvasY);
        }

        [TestMethod]
        public async Task Test08_Hierarchy_NonUniformSectionCountsAcrossRacks()
        {
            // Rack 1: 4 sections
            var rack1 = new Rack
            {
                RackCode = "RCK-VAR-1",
                Name = "Variable Rack 1",
                WarehouseId = "WH-03",
                NumberOfLevels = 4
            };
            rack1.Sections.Add(new RackSection { SectionCode = "VAR1-S1", Name = "S1", TierLevel = 1 });
            rack1.Sections.Add(new RackSection { SectionCode = "VAR1-S2", Name = "S2", TierLevel = 2 });
            rack1.Sections.Add(new RackSection { SectionCode = "VAR1-S3", Name = "S3", TierLevel = 3 });
            rack1.Sections.Add(new RackSection { SectionCode = "VAR1-S4", Name = "S4", TierLevel = 4 });
            await _inventory.CreateRackAsync(rack1);

            // Rack 2: 2 sections only
            var rack2 = new Rack
            {
                RackCode = "RCK-VAR-2",
                Name = "Variable Rack 2",
                WarehouseId = "WH-03",
                NumberOfLevels = 2
            };
            rack2.Sections.Add(new RackSection { SectionCode = "VAR2-S1", Name = "Ground Ingot Bay", TierLevel = 1 });
            rack2.Sections.Add(new RackSection { SectionCode = "VAR2-S2", Name = "Upper Heavy Deck", TierLevel = 2 });
            await _inventory.CreateRackAsync(rack2);

            var fetched1 = await _inventory.GetRackByIdAsync("RCK-VAR-1");
            var fetched2 = await _inventory.GetRackByIdAsync("RCK-VAR-2");

            Assert.AreEqual(4, fetched1!.Sections.Count);
            Assert.AreEqual(2, fetched2!.Sections.Count);
        }

        [TestMethod]
        public async Task Test09_Hierarchy_NestedCompartmentsAndBins_MaintainsParentChildRelationship()
        {
            var section = new RackSection
            {
                Id = "SEC-TEST-NESTED",
                RackId = "RCK-01",
                SectionCode = "A1-T1-NEST",
                Name = "Tier with nested bins"
            };

            var compA = new CompartmentBin
            {
                Id = "CMP-01",
                BinCode = "COMP-A",
                Name = "Compartment Alpha",
                Capacity = 100,
                Occupied = 40
            };
            compA.NestedBins.Add(new CompartmentBin
            {
                Id = "BIN-01",
                BinCode = "BIN-A1",
                Name = "Micro Bin A1",
                Capacity = 20,
                Occupied = 15
            });

            section.Compartments.Add(compA);

            Assert.AreEqual(1, section.Compartments.Count);
            Assert.AreEqual(1, section.Compartments[0].NestedBins.Count);
            Assert.AreEqual("BIN-A1", section.Compartments[0].NestedBins[0].BinCode);
        }

        [TestMethod]
        public async Task Test10_Hierarchy_StoreMultipleProductsInSection_CalculatesLocationBalanceAccurately()
        {
            var items = await _inventory.GetInventoryByLocationAsync("WH-01", "RCK-A-01", "A1-T1");
            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count >= 2, "Section A1-T1 must contain multiple products");

            // Verify both Bearings and Butterfly Valves are stored in A1-T1
            Assert.IsTrue(items.Any(p => p.Sku == "SKU-BRG-6205"));
            Assert.IsTrue(items.Any(p => p.Sku == "SKU-VAL-DN50"));

            decimal totalQty = items.Sum(i => i.Quantity);
            Assert.IsTrue(totalQty > 0);
        }

        [TestMethod]
        public async Task Test11_CapacityAndOccupancy_CalculatesUsedRemainingAndStatusIndicators()
        {
            var rack = await _inventory.GetRackByIdAsync("RCK-01");
            Assert.IsNotNull(rack);
            Assert.IsTrue(rack.HasCapacityConfigured);
            Assert.IsTrue(rack.OccupancyPercentage > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(rack.CapacityDetailsDisplay));

            // Test section occupancy
            var sec = rack.Sections.First();
            Assert.IsTrue(sec.HasCapacityConfigured);
            Assert.AreEqual("kg", sec.CapacityUnit);
            Assert.IsTrue(sec.OccupiedCapacity <= sec.MaxCapacity);
        }

        [TestMethod]
        public async Task Test12_Capacity_RejectsOverCapacityPlacement_WhenHardLimitExceeded()
        {
            // Attempt to relocate an excessive quantity that exceeds hard safety capacity (1.5x)
            var relocationDto = new StockRelocationDto
            {
                ProductId = "PRD-101",
                Quantity = 50000m, // massive overload
                Unit = "Pcs",
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-01",
                DestinationRackCode = "RCK-A-01",
                DestinationLocationCode = "A1-T2"
            };

            var res = await _inventory.RelocateStockAsync(relocationDto);
            Assert.IsFalse(res.Success);
            Assert.IsTrue(res.Message.Contains("hard safety capacity limit") || res.Message.Contains("exceed"));
        }

        [TestMethod]
        public async Task Test13_LocationSuggestions_GeneratesRankedSuggestionsWithReasoning()
        {
            var req = new LocationSuggestionRequest
            {
                ProductId = "PRD-101", // Bearings (default location WH-01, RCK-A-01, A1-T1)
                Quantity = 20m,
                Unit = "Pcs",
                TargetWarehouseId = "WH-01",
                IsPerishable = false
            };

            var suggestions = await _inventory.SuggestStorageLocationsAsync(req);
            Assert.IsNotNull(suggestions);
            Assert.IsTrue(suggestions.Count > 0, "Must return ranked candidate locations");

            // The product's configured default location should rank top
            var top = suggestions[0];
            Assert.AreEqual("WH-01", top.WarehouseId);
            Assert.IsTrue(top.MatchScore >= 80, "Top suggestion must have high match score");
            Assert.IsTrue(top.SuggestionReason.Contains("Default") || top.SuggestionReason.Contains("Consolidation"));
        }

        [TestMethod]
        public async Task Test14_LocationSuggestions_ManualOverride_PermitsCustomValidDestination()
        {
            // Even if A1-T1 is suggested, user can manually override to B4-T1
            var relocationDto = new StockRelocationDto
            {
                ProductId = "PRD-101",
                Quantity = 10m,
                Unit = "Pcs",
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-01",
                DestinationRackCode = "RCK-B-04", // Manual override destination
                DestinationLocationCode = "B4-T1",
                Reason = "Supervisor Manual Override Slotting"
            };

            var result = await _inventory.RelocateStockAsync(relocationDto);
            Assert.IsTrue(result.Success, result.Message);
            Assert.IsTrue(result.Message.Contains("B4-T1"));
        }

        [TestMethod]
        public async Task Test15_StockRelocation_AtomicTransferBetweenLocations_LogsLedgerTransaction()
        {
            var prod = await _inventory.GetProductBySkuAsync("SKU-VAL-DN50");
            Assert.IsNotNull(prod);

            var relocation = new StockRelocationDto
            {
                ProductId = prod.Id,
                Quantity = 5m,
                Unit = prod.Unit,
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-01",
                DestinationRackCode = "RCK-A-01",
                DestinationLocationCode = "A1-T3",
                Reason = "Pallet Tier Restructuring"
            };

            var res = await _inventory.RelocateStockAsync(relocation);
            Assert.IsTrue(res.Success);

            // Verify stock movement ledger has logged this atomic transaction
            var movements = await _inventory.GetMovementsForProductAsync(prod.Id, 5);
            Assert.IsTrue(movements.Any(m => m.Type == MovementType.LocationRelocation && m.Quantity == 5m));
        }

        [TestMethod]
        public async Task Test16_StockRelocation_InterWarehouseTransfer_UpdatesBothWarehousesAndMovementLedger()
        {
            var prod = await _inventory.GetProductBySkuAsync("SKU-BRG-6205");
            Assert.IsNotNull(prod);

            var transfer = new StockRelocationDto
            {
                ProductId = prod.Id,
                Quantity = 15m,
                Unit = "Pcs",
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-02", // Transfer to South Depot
                DestinationRackCode = "RCK-RAW-09",
                DestinationLocationCode = "R9-T1",
                Reason = "Inter-facility transfer request"
            };

            var res = await _inventory.RelocateStockAsync(transfer);
            Assert.IsTrue(res.Success);

            // Must be logged as Transfer
            var movements = await _inventory.GetMovementsForProductAsync(prod.Id, 5);
            Assert.IsTrue(movements.Any(m => m.Type == MovementType.Transfer && m.DestinationLocation.Contains("WH-SOUTH") || m.DestinationLocation.Contains("WH-02")));
        }

        [TestMethod]
        public async Task Test17_StockRelocation_BatchTrackingValidation_DeductsFromSpecificBatch()
        {
            var batches = await _inventory.GetBatchesForProductAsync("PRD-101");
            Assert.IsTrue(batches.Count > 0);
            var targetBatch = batches.First();
            decimal origRemaining = targetBatch.RemainingQuantity;

            var relocation = new StockRelocationDto
            {
                ProductId = "PRD-101",
                BatchId = targetBatch.BatchId,
                BatchNumber = targetBatch.BatchNumber,
                Quantity = 10m,
                Unit = targetBatch.Unit,
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-01",
                DestinationRackCode = "RCK-B-04",
                DestinationLocationCode = "B4-T1"
            };

            var res = await _inventory.RelocateStockAsync(relocation);
            Assert.IsTrue(res.Success);
            Assert.AreEqual("RCK-B-04", targetBatch.RackCode);
            Assert.AreEqual("B4-T1", targetBatch.ShelfCode);
        }

        [TestMethod]
        public async Task Test18_DuplicateLocationCode_PreventionEnforced()
        {
            var sec1 = new RackSection
            {
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionCode = "DUP-S1",
                Name = "Duplicate Section Test"
            };
            await _inventory.CreateSectionAsync(sec1);

            var secDuplicate = new RackSection
            {
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionCode = "DUP-S1",
                Name = "Duplicate Section 2"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _inventory.CreateSectionAsync(secDuplicate);
            });
        }

        [TestMethod]
        public async Task Test19_VisualRackMovement_DoesNotChangeInventoryQuantities()
        {
            var prod = await _inventory.GetProductBySkuAsync("SKU-BRG-6205");
            Assert.IsNotNull(prod);
            decimal initialStock = prod.PhysicalStock;

            // Perform visual canvas rack moves
            var racks = await _inventory.GetRacksByWarehouseAsync("WH-01");
            foreach (var r in racks)
            {
                r.CanvasX += 100;
                r.CanvasY += 50;
            }
            await _inventory.SaveRackLayoutCoordinatesAsync("WH-01", racks);

            // Re-fetch product
            var prodAfter = await _inventory.GetProductBySkuAsync("SKU-BRG-6205");
            Assert.IsNotNull(prodAfter);
            Assert.AreEqual(initialStock, prodAfter.PhysicalStock, "Visual movement on canvas MUST NOT alter inventory balances!");
        }

        [TestMethod]
        public async Task Test20_WarehouseStatus_DeactivationBlocksInwardAndRelocation()
        {
            var wh = await _inventory.GetWarehouseByIdAsync("WH-03");
            Assert.IsNotNull(wh);

            // Deactivate WH-03
            await _inventory.ToggleWarehouseStatusAsync(wh.Id, false);
            Assert.IsFalse(wh.IsActive);

            // Attempting to relocate to a deactivated warehouse must fail
            var relocation = new StockRelocationDto
            {
                ProductId = "PRD-101",
                Quantity = 5m,
                Unit = "Pcs",
                SourceWarehouseId = "WH-01",
                SourceRackCode = "RCK-A-01",
                SourceLocationCode = "A1-T1",
                DestinationWarehouseId = "WH-03", // Deactivated!
                DestinationRackCode = "RCK-TEMP",
                DestinationLocationCode = "T1"
            };

            var res = await _inventory.RelocateStockAsync(relocation);
            Assert.IsFalse(res.Success);
            Assert.IsTrue(res.Message.Contains("deactivated"));
        }
    }
}
