using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class MockDashboardService : IDashboardService
    {
        private readonly List<Warehouse> _warehouses;
        private readonly List<Rack> _racks;
        private readonly List<StockMovement> _movements;
        private readonly List<InventoryAlert> _alerts;

        public bool IsConnectedToRemoteApi => false;
        public string ApiEndpointUrl => "https://api.stoms.internal/v1 (Local Mock)";

        public MockDashboardService()
        {
            var now = DateTime.Now;

            // 1. Warehouses
            _warehouses = new List<Warehouse>
            {
                new Warehouse
                {
                    Id = "WH-01",
                    Code = "WH-NORTH",
                    Name = "Central Logistics Hub - Sector 4",
                    CityLocation = "Vadodara Industrial Zone",
                    TotalRacksCount = 8,
                    TotalCapacity = 500,
                    OccupiedCapacity = 360,
                    Unit = "Tons"
                },
                new Warehouse
                {
                    Id = "WH-02",
                    Code = "WH-SOUTH",
                    Name = "Raw Materials & Heavy Storage Depot",
                    CityLocation = "Surat Export Park",
                    TotalRacksCount = 6,
                    TotalCapacity = 300,
                    OccupiedCapacity = 190,
                    Unit = "Tons"
                },
                new Warehouse
                {
                    Id = "WH-03",
                    Code = "WH-EAST",
                    Name = "Spares & Precision Assembly Annex",
                    CityLocation = "Ahmedabad Tech Corridor",
                    TotalRacksCount = 4,
                    TotalCapacity = null, // Demonstrates "Capacity not configured"
                    OccupiedCapacity = 0,
                    Unit = "Units"
                }
            };

            // 2. Racks & Sections with Industrial Products
            _racks = new List<Rack>
            {
                new Rack
                {
                    Id = "RCK-01",
                    WarehouseId = "WH-01",
                    WarehouseName = "Central Logistics Hub - Sector 4",
                    RackCode = "RCK-A-01",
                    Name = "High-Density Heavy Pallet Rack A1",
                    Zone = "Aisle 1 - Bay North",
                    CanvasX = 30,
                    CanvasY = 30,
                    MaxCapacity = 1200,
                    OccupiedCapacity = 920,
                    CapacityUnit = "kg",
                    IsExpanded = true,
                    Sections = new List<RackSection>
                    {
                        new RackSection
                        {
                            Id = "SEC-01",
                            SectionCode = "A1-T1",
                            Name = "Tier 1: Ground Pallet Load",
                            TierLevel = 1,
                            MaxCapacity = 500,
                            OccupiedCapacity = 420,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-101",
                                    Sku = "SKU-BRG-6205",
                                    Name = "Heavy Industrial Ball Bearings (Deep Groove 6205)",
                                    Category = "Mechanical Power Transmission",
                                    BatchNumber = "BAT-2026-088",
                                    Quantity = 450,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 100,
                                    Status = StockStatus.Normal,
                                    CompartmentCode = "BIN-01"
                                },
                                new StoredProductItem
                                {
                                    ProductId = "PRD-102",
                                    Sku = "SKU-VAL-DN50",
                                    Name = "Stainless Steel Butterfly Valves DN50 PN16",
                                    Category = "Fluid & Flow Control",
                                    BatchNumber = "BAT-2026-092",
                                    Quantity = 42,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 50, // Trigger Low Stock! (42 <= 50)
                                    Status = StockStatus.LowStock,
                                    CompartmentCode = "BIN-02"
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-02",
                            SectionCode = "A1-T2",
                            Name = "Tier 2: Hydraulic Lubricants & Solvents",
                            TierLevel = 2,
                            MaxCapacity = 400,
                            OccupiedCapacity = 380,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-103",
                                    Sku = "SKU-LUB-ISO68",
                                    Name = "Industrial Hydraulic ISO VG 68 Lubricant Drums",
                                    Category = "Chemicals & Lubricants",
                                    BatchNumber = "BAT-2026-CHEM-12",
                                    Quantity = 8,
                                    Unit = "Drums",
                                    ExpiryDate = now.AddDays(18), // Near Expiry (< 30 days)
                                    ReorderThreshold = 4,
                                    Status = StockStatus.NearExpiry,
                                    CompartmentCode = "PAL-02"
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-03",
                            SectionCode = "A1-T3",
                            Name = "Tier 3: Structural High-Tensile Fasteners",
                            TierLevel = 3,
                            MaxCapacity = 300,
                            OccupiedCapacity = 120,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-104",
                                    Sku = "SKU-BOLT-M16",
                                    Name = "M16 Grade 8.8 Hex Bolts & Washer Kits",
                                    Category = "Fasteners & Hardware",
                                    BatchNumber = "BAT-2025-FST-99",
                                    Quantity = 1200,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 300,
                                    Status = StockStatus.Normal,
                                    CompartmentCode = "BIN-03"
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-04",
                            SectionCode = "A1-T4",
                            Name = "Tier 4: Top Level Reserve Buffer",
                            TierLevel = 4,
                            MaxCapacity = 200,
                            OccupiedCapacity = 0,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>() // Empty section demonstration
                        }
                    }
                },
                new Rack
                {
                    Id = "RCK-02",
                    WarehouseId = "WH-01",
                    WarehouseName = "Central Logistics Hub - Sector 4",
                    RackCode = "RCK-B-04",
                    Name = "Pneumatics & Sensor Instrumentation Rack B4",
                    Zone = "Aisle 2 - Bay Central",
                    CanvasX = 320,
                    CanvasY = 30,
                    MaxCapacity = 800,
                    OccupiedCapacity = 640,
                    CapacityUnit = "kg",
                    IsExpanded = false,
                    Sections = new List<RackSection>
                    {
                        new RackSection
                        {
                            Id = "SEC-05",
                            SectionCode = "B4-T1",
                            Name = "Tier 1: High-Precision Transducers",
                            TierLevel = 1,
                            MaxCapacity = 250,
                            OccupiedCapacity = 180,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-105",
                                    Sku = "SKU-SNS-420MA",
                                    Name = "Pressure Transmitters (4-20mA Industrial 0-10 Bar)",
                                    Category = "Sensors & Automation",
                                    BatchNumber = "BAT-2026-SNS-01",
                                    Quantity = 85,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 20,
                                    Status = StockStatus.Normal,
                                    CompartmentCode = "CAB-A1"
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-06",
                            SectionCode = "B4-T2",
                            Name = "Tier 2: Expired Adhesives & Sealants",
                            TierLevel = 2,
                            MaxCapacity = 250,
                            OccupiedCapacity = 210,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-106",
                                    Sku = "SKU-ADH-EPOXY",
                                    Name = "Industrial Structural Epoxy Resin Adhesive Cartridges",
                                    Category = "Chemical Adhesives",
                                    BatchNumber = "BAT-2025-ADH-04",
                                    Quantity = 35,
                                    Unit = "Pcs",
                                    ExpiryDate = now.AddDays(-14), // Already Expired!
                                    ReorderThreshold = 15,
                                    Status = StockStatus.Expired,
                                    CompartmentCode = "CAB-A2"
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-07",
                            SectionCode = "B4-T3",
                            Name = "Tier 3: Damaged Stock Quarantine",
                            TierLevel = 3,
                            MaxCapacity = 300,
                            OccupiedCapacity = 250,
                            CapacityUnit = "kg",
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-107",
                                    Sku = "SKU-CYL-PNEU",
                                    Name = "Double-Acting ISO Pneumatic Cylinders (Damaged Seals)",
                                    Category = "Pneumatics",
                                    BatchNumber = "BAT-2026-CYL-19",
                                    Quantity = 14,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 10,
                                    Status = StockStatus.Damaged,
                                    CompartmentCode = "QR-01"
                                }
                            }
                        }
                    }
                },
                new Rack
                {
                    Id = "RCK-03",
                    WarehouseId = "WH-02",
                    WarehouseName = "Raw Materials & Heavy Storage Depot",
                    RackCode = "RCK-RAW-09",
                    Name = "Cast Iron Castings & Raw Bar Stock C9",
                    Zone = "Heavy Yard Yard-3",
                    CanvasX = 30,
                    CanvasY = 220,
                    MaxCapacity = null, // Capacity not configured demonstration!
                    OccupiedCapacity = 0,
                    CapacityUnit = "kg",
                    IsExpanded = false,
                    Sections = new List<RackSection>
                    {
                        new RackSection
                        {
                            Id = "SEC-08",
                            SectionCode = "RAW-L1",
                            Name = "Level 1: Cast Iron Flanges (Rough Machined)",
                            TierLevel = 1,
                            MaxCapacity = null,
                            OccupiedCapacity = 0,
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-108",
                                    Sku = "SKU-RAW-CIF50",
                                    Name = "Class 150 Cast Iron Flange Blanks DN50",
                                    Category = "Raw Materials",
                                    BatchNumber = "BAT-2026-RAW-81",
                                    Quantity = 220,
                                    Unit = "Pcs",
                                    ExpiryDate = null,
                                    ReorderThreshold = 50,
                                    Status = StockStatus.Normal
                                }
                            }
                        },
                        new RackSection
                        {
                            Id = "SEC-09",
                            SectionCode = "RAW-L2",
                            Name = "Level 2: Stainless Steel Round Rods 316L",
                            TierLevel = 2,
                            MaxCapacity = null,
                            OccupiedCapacity = 0,
                            Products = new List<StoredProductItem>
                            {
                                new StoredProductItem
                                {
                                    ProductId = "PRD-109",
                                    Sku = "SKU-ROD-SS316",
                                    Name = "Cold Drawn SS316L Round Bar 25mm x 3m",
                                    Category = "Raw Materials",
                                    BatchNumber = "BAT-2026-MET-54",
                                    Quantity = 75,
                                    Unit = "Bars",
                                    ExpiryDate = null,
                                    ReorderThreshold = 20,
                                    Status = StockStatus.Normal
                                }
                            }
                        }
                    }
                }
            };

            // 3. Stock Movements (Real transactions)
            _movements = new List<StockMovement>
            {
                new StockMovement
                {
                    TransactionId = "TRX-2026-1049",
                    Timestamp = now.AddMinutes(-32),
                    ProductId = "PRD-101",
                    ProductName = "Heavy Industrial Ball Bearings (6205)",
                    Sku = "SKU-BRG-6205",
                    Type = MovementType.StockIn,
                    Quantity = 150,
                    Unit = "Pcs",
                    SourceLocation = "Dock Bay #2 (Supplier PO-441)",
                    DestinationLocation = "WH-01 / RCK-A-01 / SEC-01",
                    InitiatedBy = "Saumya Chaudhari",
                    Status = MovementStatus.Completed,
                    Remarks = "Quality acceptance passed. GRN #8820 recorded."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1048",
                    Timestamp = now.AddHours(-1.8),
                    ProductId = "PRD-102",
                    ProductName = "Stainless Steel Butterfly Valves DN50",
                    Sku = "SKU-VAL-DN50",
                    Type = MovementType.StockOut,
                    Quantity = 20,
                    Unit = "Pcs",
                    SourceLocation = "WH-01 / RCK-A-01 / SEC-01",
                    DestinationLocation = "Shopfloor Assembly Line #3",
                    InitiatedBy = "Jenil Patel",
                    Status = MovementStatus.Completed,
                    Remarks = "Production dispatch order #WO-904."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1047",
                    Timestamp = now.AddHours(-4.5),
                    ProductId = "PRD-103",
                    ProductName = "Industrial Hydraulic ISO VG 68 Lubricant",
                    Sku = "SKU-LUB-ISO68",
                    Type = MovementType.Transfer,
                    Quantity = 2,
                    Unit = "Drums",
                    SourceLocation = "WH-02 / Raw Materials Depot",
                    DestinationLocation = "WH-01 / RCK-A-01 / SEC-02",
                    InitiatedBy = "Hardik Jariwala",
                    Status = MovementStatus.PendingApproval,
                    Remarks = "Inter-warehouse transfer awaiting supervisor sign-off."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1046",
                    Timestamp = now.AddHours(-9.2),
                    ProductId = "PRD-107",
                    ProductName = "Double-Acting ISO Pneumatic Cylinders",
                    Sku = "SKU-CYL-PNEU",
                    Type = MovementType.Damage,
                    Quantity = 4,
                    Unit = "Pcs",
                    SourceLocation = "WH-01 / Main Receiving",
                    DestinationLocation = "Quarantine Bay QR-01",
                    InitiatedBy = "QA Inspector Verma",
                    Status = MovementStatus.Completed,
                    Remarks = "Transit impact damage detected on piston rod threads."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1045",
                    Timestamp = now.AddDays(-1).AddHours(2),
                    ProductId = "PRD-104",
                    ProductName = "M16 Grade 8.8 Hex Bolts & Washer Kits",
                    Sku = "SKU-BOLT-M16",
                    Type = MovementType.StockIn,
                    Quantity = 500,
                    Unit = "Pcs",
                    SourceLocation = "Supplier Inward Gate #1",
                    DestinationLocation = "WH-01 / RCK-A-01 / SEC-03",
                    InitiatedBy = "Saumya Chaudhari",
                    Status = MovementStatus.Completed,
                    Remarks = "Batch certification verified (Mill Cert EN 10204 3.1)."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1044",
                    Timestamp = now.AddDays(-1).AddHours(-3),
                    ProductId = "PRD-105",
                    ProductName = "Pressure Transmitters (4-20mA)",
                    Sku = "SKU-SNS-420MA",
                    Type = MovementType.Adjustment,
                    Quantity = 5,
                    Unit = "Pcs",
                    SourceLocation = "WH-01 / RCK-B-04 / SEC-05",
                    DestinationLocation = "WH-01 / RCK-B-04 / SEC-05",
                    InitiatedBy = "Hardik Jariwala",
                    Status = MovementStatus.PendingApproval,
                    Remarks = "Cycle count variance reconciliation (+5 units)."
                },
                new StockMovement
                {
                    TransactionId = "TRX-2026-1043",
                    Timestamp = now.AddDays(-2),
                    ProductId = "PRD-108",
                    ProductName = "Cast Iron Flange Blanks DN50",
                    Sku = "SKU-RAW-CIF50",
                    Type = MovementType.StockIn,
                    Quantity = 100,
                    Unit = "Pcs",
                    SourceLocation = "Foundry Supply Carrier",
                    DestinationLocation = "WH-02 / RCK-RAW-09 / SEC-08",
                    InitiatedBy = "Logistics Team",
                    Status = MovementStatus.Completed,
                    Remarks = "Direct unloading to outdoor heavy yard."
                }
            };

            // 4. Alerts
            _alerts = new List<InventoryAlert>
            {
                new InventoryAlert
                {
                    Id = "ALT-001",
                    Category = AlertCategory.LowStock,
                    Severity = AlertSeverity.Warning,
                    Title = "Low Stock Alert: Butterfly Valves DN50",
                    Description = "Available stock (42 Pcs) is at or below the reorder threshold (50 Pcs).",
                    TargetSku = "SKU-VAL-DN50",
                    Location = "WH-01 / RCK-A-01 / SEC-01",
                    Timestamp = now.AddHours(-1.5),
                    TargetModule = "Product Management"
                },
                new InventoryAlert
                {
                    Id = "ALT-002",
                    Category = AlertCategory.ExpiredStock,
                    Severity = AlertSeverity.Critical,
                    Title = "Expired Batch: Epoxy Resin Adhesives",
                    Description = "Batch BAT-2025-ADH-04 expired 14 days ago (35 Pcs). Immediate disposal or recertification required.",
                    TargetSku = "SKU-ADH-EPOXY",
                    Location = "WH-01 / RCK-B-04 / SEC-06",
                    Timestamp = now.AddDays(-1),
                    TargetModule = "Stock Management"
                },
                new InventoryAlert
                {
                    Id = "ALT-003",
                    Category = AlertCategory.NearExpiry,
                    Severity = AlertSeverity.Warning,
                    Title = "Near Expiry Alert: Hydraulic Fluid Drums",
                    Description = "Batch BAT-2026-CHEM-12 expires in 18 days. Please prioritize issue to manufacturing line.",
                    TargetSku = "SKU-LUB-ISO68",
                    Location = "WH-01 / RCK-A-01 / SEC-02",
                    Timestamp = now.AddHours(-5),
                    TargetModule = "Stock Movements"
                },
                new InventoryAlert
                {
                    Id = "ALT-004",
                    Category = AlertCategory.PendingApproval,
                    Severity = AlertSeverity.Info,
                    Title = "Pending Approval: Inter-Warehouse Transfer",
                    Description = "Transfer TRX-2026-1047 (2 Drums Lubricant) initiated by Hardik J. requires supervisor authorization.",
                    TargetSku = "SKU-LUB-ISO68",
                    Location = "WH-02 -> WH-01",
                    Timestamp = now.AddHours(-4.5),
                    TargetModule = "Approvals"
                }
            };
        }

        public Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        {
            var allProducts = _racks.SelectMany(r => r.Sections).SelectMany(s => s.Products).ToList();
            var distinctProductSkus = allProducts.Select(p => p.Sku).Distinct().Count();

            // Total stock broken down by unit
            var unitGroups = allProducts
                .GroupBy(p => p.Unit)
                .Select(g => new UnitStockBreakdown
                {
                    Unit = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(u => u.Quantity)
                .ToList();

            var lowStockCount = allProducts.Count(p => p.Status == StockStatus.LowStock);
            var nearExpiryCount = allProducts.Count(p => p.Status == StockStatus.NearExpiry);
            var expiredCount = allProducts.Count(p => p.Status == StockStatus.Expired);
            var pendingApprovalsCount = _movements.Count(m => m.Status == MovementStatus.PendingApproval);

            var summary = new DashboardSummary
            {
                TotalProducts = distinctProductSkus,
                TotalStockQuantity = allProducts.Where(p => p.Unit == "Pcs").Sum(p => p.Quantity),
                PrimaryUnit = "Pcs (Core Units)",
                TotalValuation = 4820500m,
                TodayInwardQuantity = 150m,
                TodayInwardCount = 1,
                TodayOutwardQuantity = 20m,
                TodayOutwardCount = 1,
                UnitBreakdowns = unitGroups,
                LowStockItemsCount = lowStockCount,
                OutOfStockItemsCount = allProducts.Count(p => p.Quantity <= 0),
                NearExpiryBatchesCount = nearExpiryCount,
                ExpiredStockBatchesCount = expiredCount,
                PendingApprovalsCount = pendingApprovalsCount,
                LastRefreshed = DateTime.Now,
                IsDemoData = true
            };

            return Task.FromResult(summary);
        }

        public Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Warehouse>>(_warehouses.AsReadOnly());
        }

        public Task<IReadOnlyList<Rack>> GetRacksAsync(
            string? warehouseId = null,
            string? searchQuery = null,
            StockStatus? statusFilter = null,
            CancellationToken cancellationToken = default)
        {
            var query = _racks.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(warehouseId))
            {
                query = query.Where(r => r.WarehouseId == warehouseId);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    r.RackCode.ToLowerInvariant().Contains(term) ||
                    r.Name.ToLowerInvariant().Contains(term) ||
                    r.WarehouseName.ToLowerInvariant().Contains(term) ||
                    r.Sections.Any(s =>
                        s.SectionCode.ToLowerInvariant().Contains(term) ||
                        s.Name.ToLowerInvariant().Contains(term) ||
                        s.Products.Any(p => p.Name.ToLowerInvariant().Contains(term) || p.Sku.ToLowerInvariant().Contains(term))));
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(r => r.OverallStatus == statusFilter.Value ||
                                         r.Sections.Any(s => s.WorstStatus == statusFilter.Value || s.Products.Any(p => p.Status == statusFilter.Value)));
            }

            return Task.FromResult<IReadOnlyList<Rack>>(query.ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(
            int limit = 10,
            MovementDateRange dateRange = MovementDateRange.AllTime,
            string? searchQuery = null,
            CancellationToken cancellationToken = default)
        {
            var query = _movements.AsEnumerable();
            var now = DateTime.Now;

            switch (dateRange)
            {
                case MovementDateRange.Today:
                    query = query.Where(m => m.Timestamp.Date == now.Date);
                    break;
                case MovementDateRange.ThisWeek:
                    query = query.Where(m => m.Timestamp >= now.AddDays(-7));
                    break;
                case MovementDateRange.ThisMonth:
                    query = query.Where(m => m.Timestamp >= now.AddDays(-30));
                    break;
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(m =>
                    m.TransactionId.ToLowerInvariant().Contains(term) ||
                    m.ProductName.ToLowerInvariant().Contains(term) ||
                    m.Sku.ToLowerInvariant().Contains(term) ||
                    m.SourceLocation.ToLowerInvariant().Contains(term) ||
                    m.DestinationLocation.ToLowerInvariant().Contains(term) ||
                    m.InitiatedBy.ToLowerInvariant().Contains(term));
            }

            var results = query.OrderByDescending(m => m.Timestamp).Take(limit).ToList();
            return Task.FromResult<IReadOnlyList<StockMovement>>(results.AsReadOnly());
        }

        public Task<IReadOnlyList<InventoryAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<InventoryAlert>>(_alerts.AsReadOnly());
        }

        public Task<int> GetPendingApprovalsCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_movements.Count(m => m.Status == MovementStatus.PendingApproval));
        }
    }
}

