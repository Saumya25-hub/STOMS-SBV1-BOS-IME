using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STOMS_SBV1_BOS_IME.Models;

namespace STOMS_SBV1_BOS_IME.Services
{
    public class MockInventoryService : IInventoryService
    {
        private static MockInventoryService? _instance;
        private static readonly object _lock = new();

        public static MockInventoryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new MockInventoryService();
                    }
                }
                return _instance;
            }
        }

        private readonly List<Product> _products = new();
        private readonly List<StockBatch> _batches = new();
        private readonly List<StockSerialNumber> _serialNumbers = new();
        private readonly List<StockLocationBalance> _locationBalances = new();
        private readonly List<StockMovement> _movements = new();
        private readonly List<StockApprovalRequest> _approvalRequests = new();
        private readonly List<Warehouse> _warehouses = new();
        private readonly List<Rack> _racks = new();

        public bool IsConnectedToRemoteApi => false;
        public string ApiEndpointUrl => "https://api.stoms.internal/v1 (In-Memory Master Ledger)";

        public event EventHandler? InventoryChanged;

        public MockInventoryService()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            var now = DateTime.Now;

            // Warehouses
            _warehouses.Add(new Warehouse
            {
                Id = "WH-01",
                Code = "WH-NORTH",
                Name = "Central Logistics Hub - Sector 4",
                CityLocation = "Vadodara Industrial Zone",
                TotalRacksCount = 8,
                TotalCapacity = 500,
                OccupiedCapacity = 360,
                Unit = "Tons"
            });
            _warehouses.Add(new Warehouse
            {
                Id = "WH-02",
                Code = "WH-SOUTH",
                Name = "Raw Materials & Heavy Storage Depot",
                CityLocation = "Surat Export Park",
                TotalRacksCount = 6,
                TotalCapacity = 300,
                OccupiedCapacity = 190,
                Unit = "Tons"
            });
            _warehouses.Add(new Warehouse
            {
                Id = "WH-03",
                Code = "WH-EAST",
                Name = "Spares & Precision Assembly Annex",
                CityLocation = "Ahmedabad Tech Corridor",
                TotalRacksCount = 4,
                TotalCapacity = null,
                OccupiedCapacity = 0,
                Unit = "Units"
            });

            // Racks
            _racks.Add(new Rack
            {
                Id = "RCK-01",
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackCode = "RCK-A-01",
                Name = "High-Density Heavy Pallet Rack A1",
                Zone = "Aisle 1 - Bay North",
                MaxCapacity = 1200,
                OccupiedCapacity = 920,
                CapacityUnit = "kg",
                Sections = new List<RackSection>
                {
                    new RackSection { Id = "SEC-01", SectionCode = "A1-T1", Name = "Tier 1: Ground Pallet Load", TierLevel = 1, MaxCapacity = 500, OccupiedCapacity = 420 },
                    new RackSection { Id = "SEC-02", SectionCode = "A1-T2", Name = "Tier 2: Hydraulic Lubricants & Solvents", TierLevel = 2, MaxCapacity = 400, OccupiedCapacity = 380 },
                    new RackSection { Id = "SEC-03", SectionCode = "A1-T3", Name = "Tier 3: Structural High-Tensile Fasteners", TierLevel = 3, MaxCapacity = 300, OccupiedCapacity = 120 },
                    new RackSection { Id = "SEC-04", SectionCode = "A1-T4", Name = "Tier 4: Top Level Reserve Buffer", TierLevel = 4, MaxCapacity = 200, OccupiedCapacity = 0 }
                }
            });

            _racks.Add(new Rack
            {
                Id = "RCK-02",
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackCode = "RCK-B-04",
                Name = "Pneumatics & Sensor Instrumentation Rack B4",
                Zone = "Aisle 2 - Bay Central",
                MaxCapacity = 800,
                OccupiedCapacity = 640,
                CapacityUnit = "kg",
                Sections = new List<RackSection>
                {
                    new RackSection { Id = "SEC-05", SectionCode = "B4-T1", Name = "Tier 1: High-Precision Transducers", TierLevel = 1, MaxCapacity = 250, OccupiedCapacity = 180 },
                    new RackSection { Id = "SEC-06", SectionCode = "B4-T2", Name = "Tier 2: Expired Adhesives & Sealants", TierLevel = 2, MaxCapacity = 250, OccupiedCapacity = 210 },
                    new RackSection { Id = "SEC-07", SectionCode = "B4-T3", Name = "Tier 3: Damaged Stock Quarantine", TierLevel = 3, MaxCapacity = 300, OccupiedCapacity = 250 }
                }
            });

            _racks.Add(new Rack
            {
                Id = "RCK-03",
                WarehouseId = "WH-02",
                WarehouseName = "Raw Materials & Heavy Storage Depot",
                RackCode = "RCK-RAW-09",
                Name = "Cast Iron Castings & Raw Bar Stock C9",
                Zone = "Heavy Yard Yard-3",
                Sections = new List<RackSection>
                {
                    new RackSection { Id = "SEC-08", SectionCode = "R9-T1", Name = "Yard Tier 1: Heavy Ingot Slabs", TierLevel = 1, MaxCapacity = 5000, OccupiedCapacity = 3500 }
                }
            });

            // Products
            var p1 = new Product
            {
                Id = "PRD-101",
                Name = "Heavy Industrial Ball Bearings (Deep Groove 6205)",
                Sku = "SKU-BRG-6205",
                Barcode = "8901234567890",
                Category = "Mechanical Power Transmission",
                Unit = "Pcs",
                Description = "High precision deep groove ball bearing 6205-2RS with rubber contact seals for rotary motors and pumps.",
                MinStockLevel = 50,
                ReorderLevel = 100,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-01",
                DefaultRackCode = "RCK-A-01",
                DefaultSectionId = "SEC-01",
                DefaultSectionCode = "A1-T1",
                TrackingType = ProductTrackingType.BatchTracked,
                RequiresExpiry = false,
                PhysicalStock = 450,
                AvailableStock = 450,
                IsActive = true
            };
            _products.Add(p1);

            var p2 = new Product
            {
                Id = "PRD-102",
                Name = "Stainless Steel Butterfly Valves DN50 PN16",
                Sku = "SKU-VAL-DN50",
                Barcode = "8901234567891",
                Category = "Fluid & Flow Control",
                Unit = "Pcs",
                Description = "Wafer-type stainless steel butterfly valve DN50 with EPDM liner and lever handle operator.",
                MinStockLevel = 20,
                ReorderLevel = 50,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-01",
                DefaultRackCode = "RCK-A-01",
                DefaultSectionId = "SEC-01",
                DefaultSectionCode = "A1-T1",
                TrackingType = ProductTrackingType.BatchTracked,
                RequiresExpiry = false,
                PhysicalStock = 42,
                AvailableStock = 42, // Trigger low stock (42 <= 50)
                IsActive = true
            };
            _products.Add(p2);

            var p3 = new Product
            {
                Id = "PRD-103",
                Name = "Industrial Hydraulic ISO VG 68 Lubricant Drums",
                Sku = "SKU-LUB-ISO68",
                Barcode = "8901234567892",
                Category = "Chemicals & Lubricants",
                Unit = "Drums",
                Description = "Premium anti-wear hydraulic oil 205-liter drum for severe heavy industrial hydraulic presses.",
                MinStockLevel = 2,
                ReorderLevel = 4,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-01",
                DefaultRackCode = "RCK-A-01",
                DefaultSectionId = "SEC-02",
                DefaultSectionCode = "A1-T2",
                TrackingType = ProductTrackingType.BatchTracked,
                RequiresExpiry = true,
                PhysicalStock = 8,
                AvailableStock = 8,
                IsActive = true
            };
            _products.Add(p3);

            var p4 = new Product
            {
                Id = "PRD-104",
                Name = "M16 Grade 8.8 Hex Bolts & Washer Kits",
                Sku = "SKU-BOLT-M16",
                Barcode = null, // Demonstrates optional barcode
                Category = "Fasteners & Hardware",
                Unit = "Pcs",
                Description = "High-tensile zinc plated M16x65 hex bolts with matching spring washers and grade 8 nuts.",
                MinStockLevel = 100,
                ReorderLevel = 300,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-01",
                DefaultRackCode = "RCK-A-01",
                DefaultSectionId = "SEC-03",
                DefaultSectionCode = "A1-T3",
                TrackingType = ProductTrackingType.QuantityOnly,
                RequiresExpiry = false,
                PhysicalStock = 1200,
                AvailableStock = 1200,
                IsActive = true
            };
            _products.Add(p4);

            var p5 = new Product
            {
                Id = "PRD-105",
                Name = "Pressure Transmitters (4-20mA Industrial 0-10 Bar)",
                Sku = "SKU-SNS-420MA",
                Barcode = "8901234567893",
                Category = "Sensors & Automation",
                Unit = "Pcs",
                Description = "Piezoresistive pressure sensor with 4-20mA output, stainless steel diaphragm, DIN connector.",
                MinStockLevel = 10,
                ReorderLevel = 20,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-02",
                DefaultRackCode = "RCK-B-04",
                DefaultSectionId = "SEC-05",
                DefaultSectionCode = "B4-T1",
                TrackingType = ProductTrackingType.SerialTracked,
                RequiresExpiry = false,
                PhysicalStock = 85,
                AvailableStock = 85,
                IsActive = true
            };
            _products.Add(p5);

            var p6 = new Product
            {
                Id = "PRD-106",
                Name = "Industrial Structural Epoxy Resin Adhesive Cartridges",
                Sku = "SKU-ADH-EPOXY",
                Barcode = "8901234567894",
                Category = "Chemical Adhesives",
                Unit = "Pcs",
                Description = "Two-component high-strength structural bonding epoxy cartridge 400ml. Strictly perishable.",
                MinStockLevel = 5,
                ReorderLevel = 15,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-02",
                DefaultRackCode = "RCK-B-04",
                DefaultSectionId = "SEC-06",
                DefaultSectionCode = "B4-T2",
                TrackingType = ProductTrackingType.BatchTracked,
                RequiresExpiry = true,
                PhysicalStock = 35,
                AvailableStock = 0, // Expired cannot be issued normally
                ExpiredStock = 35,
                IsActive = true
            };
            _products.Add(p6);

            var p7 = new Product
            {
                Id = "PRD-107",
                Name = "Double-Acting ISO Pneumatic Cylinders (Damaged Seals)",
                Sku = "SKU-CYL-PNEU",
                Barcode = null,
                Category = "Pneumatics",
                Unit = "Pcs",
                Description = "Standard ISO 15552 pneumatic cylinder bore 63mm stroke 200mm awaiting overhaul.",
                MinStockLevel = 5,
                ReorderLevel = 10,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-02",
                DefaultRackCode = "RCK-B-04",
                DefaultSectionId = "SEC-07",
                DefaultSectionCode = "B4-T3",
                TrackingType = ProductTrackingType.SerialTracked,
                RequiresExpiry = false,
                PhysicalStock = 14,
                AvailableStock = 0,
                DamagedStock = 14,
                IsActive = true
            };
            _products.Add(p7);

            var p8 = new Product
            {
                Id = "PRD-108",
                Name = "Cast Iron Continuous Cast Machine Bar Stock",
                Sku = "SKU-RAW-CI-BED",
                Barcode = null,
                Category = "Raw Materials",
                Unit = "Bars",
                Description = "Grade EN-GJL-250 continuous cast rectangular grey cast iron bar 120mm x 60mm x 3000mm.",
                MinStockLevel = 5,
                ReorderLevel = 10,
                DefaultWarehouseId = "WH-02",
                DefaultWarehouseName = "Raw Materials & Heavy Storage Depot",
                DefaultRackId = "RCK-03",
                DefaultRackCode = "RCK-RAW-09",
                DefaultSectionId = "SEC-08",
                DefaultSectionCode = "R9-T1",
                TrackingType = ProductTrackingType.BatchTracked,
                RequiresExpiry = false,
                PhysicalStock = 210,
                AvailableStock = 210,
                IsActive = true
            };
            _products.Add(p8);

            var p9 = new Product
            {
                Id = "PRD-109",
                Name = "High-Torque Synchronous Neodymium Servo Motor",
                Sku = "SKU-SRV-3KW",
                Barcode = "8901234567895",
                Category = "Motors & Drives",
                Unit = "Kits",
                Description = "3.0 kW 3000 RPM high inertia AC servo motor with absolute multi-turn optical encoder.",
                MinStockLevel = 1,
                ReorderLevel = 3,
                DefaultWarehouseId = "WH-01",
                DefaultWarehouseName = "Central Logistics Hub - Sector 4",
                DefaultRackId = "RCK-01",
                DefaultRackCode = "RCK-A-01",
                DefaultSectionId = "SEC-01",
                DefaultSectionCode = "A1-T1",
                TrackingType = ProductTrackingType.BatchAndSerial,
                RequiresExpiry = false,
                PhysicalStock = 2,
                AvailableStock = 2,
                IsActive = true
            };
            _products.Add(p9);

            // Batches
            _batches.Add(new StockBatch
            {
                BatchId = "BAT-01",
                BatchNumber = "BAT-2026-088",
                ProductId = p1.Id,
                ProductName = p1.Name,
                Sku = p1.Sku,
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-01",
                SectionCode = "A1-T1",
                ReceivedQuantity = 500,
                RemainingQuantity = 450,
                Unit = p1.Unit,
                MfgDate = now.AddDays(-60),
                ExpiryDate = null,
                ReceivedDate = now.AddDays(-55),
                Supplier = "SKF Industrial Bearings Ltd.",
                Status = StockStatus.Normal
            });

            _batches.Add(new StockBatch
            {
                BatchId = "BAT-02",
                BatchNumber = "BAT-2026-092",
                ProductId = p2.Id,
                ProductName = p2.Name,
                Sku = p2.Sku,
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-01",
                SectionCode = "A1-T1",
                ReceivedQuantity = 50,
                RemainingQuantity = 42,
                Unit = p2.Unit,
                MfgDate = now.AddDays(-40),
                ExpiryDate = null,
                ReceivedDate = now.AddDays(-38),
                Supplier = "Kirloskar Valves Corp.",
                Status = StockStatus.LowStock
            });

            _batches.Add(new StockBatch
            {
                BatchId = "BAT-03",
                BatchNumber = "BAT-2026-CHEM-12",
                ProductId = p3.Id,
                ProductName = p3.Name,
                Sku = p3.Sku,
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-02",
                SectionCode = "A1-T2",
                ReceivedQuantity = 10,
                RemainingQuantity = 8,
                Unit = p3.Unit,
                MfgDate = now.AddMonths(-11),
                ExpiryDate = now.AddDays(18), // Near Expiry!
                ReceivedDate = now.AddMonths(-10),
                Supplier = "Castrol Industrial Lubricants",
                Status = StockStatus.NearExpiry
            });

            _batches.Add(new StockBatch
            {
                BatchId = "BAT-04",
                BatchNumber = "BAT-2025-ADH-04",
                ProductId = p6.Id,
                ProductName = p6.Name,
                Sku = p6.Sku,
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-02",
                RackCode = "RCK-B-04",
                SectionId = "SEC-06",
                SectionCode = "B4-T2",
                ReceivedQuantity = 50,
                RemainingQuantity = 35,
                Unit = p6.Unit,
                MfgDate = now.AddMonths(-18),
                ExpiryDate = now.AddDays(-14), // Expired!
                ReceivedDate = now.AddMonths(-17),
                Supplier = "Henkel Loctite Adhesives",
                Status = StockStatus.Expired
            });

            _batches.Add(new StockBatch
            {
                BatchId = "BAT-05",
                BatchNumber = "BAT-2026-RAW-88",
                ProductId = p8.Id,
                ProductName = p8.Name,
                Sku = p8.Sku,
                WarehouseId = "WH-02",
                WarehouseName = "Raw Materials & Heavy Storage Depot",
                RackId = "RCK-03",
                RackCode = "RCK-RAW-09",
                SectionId = "SEC-08",
                SectionCode = "R9-T1",
                ReceivedQuantity = 250,
                RemainingQuantity = 210,
                Unit = p8.Unit,
                MfgDate = now.AddDays(-90),
                ExpiryDate = null,
                ReceivedDate = now.AddDays(-80),
                Supplier = "Gujarat Metallurgical Castings",
                Status = StockStatus.Normal
            });

            // Serial Numbers for PRD-105 (Pressure Transmitters)
            for (int i = 1; i <= 85; i++)
            {
                _serialNumbers.Add(new StockSerialNumber
                {
                    Id = $"SN-105-{i:D3}",
                    SerialNumber = $"SN-SNS-420-{i:D4}",
                    ProductId = p5.Id,
                    ProductName = p5.Name,
                    Sku = p5.Sku,
                    WarehouseId = "WH-01",
                    WarehouseName = "Central Logistics Hub - Sector 4",
                    RackId = "RCK-02",
                    RackCode = "RCK-B-04",
                    SectionId = "SEC-05",
                    SectionCode = "B4-T1",
                    Status = StockSerialStatus.Available,
                    ReceivedDate = now.AddDays(-30)
                });
            }

            // Serial Numbers for PRD-107 (Damaged Cylinders)
            for (int i = 1; i <= 14; i++)
            {
                _serialNumbers.Add(new StockSerialNumber
                {
                    Id = $"SN-107-{i:D3}",
                    SerialNumber = $"SN-CYL-ISO-{i:D4}",
                    ProductId = p7.Id,
                    ProductName = p7.Name,
                    Sku = p7.Sku,
                    WarehouseId = "WH-01",
                    WarehouseName = "Central Logistics Hub - Sector 4",
                    RackId = "RCK-02",
                    RackCode = "RCK-B-04",
                    SectionId = "SEC-07",
                    SectionCode = "B4-T3",
                    Status = StockSerialStatus.Damaged,
                    ReceivedDate = now.AddDays(-60),
                    Notes = "Damaged rod seal detected during visual inspection. Awaiting overhaul kit."
                });
            }

            // Serial Numbers for PRD-109 (Servo Motors)
            _serialNumbers.Add(new StockSerialNumber
            {
                Id = "SN-109-001",
                SerialNumber = "SN-SRV-2026-0001",
                ProductId = p9.Id,
                ProductName = p9.Name,
                Sku = p9.Sku,
                BatchNumber = "BAT-SRV-01",
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-01",
                SectionCode = "A1-T1",
                Status = StockSerialStatus.Available,
                ReceivedDate = now.AddDays(-15)
            });
            _serialNumbers.Add(new StockSerialNumber
            {
                Id = "SN-109-002",
                SerialNumber = "SN-SRV-2026-0002",
                ProductId = p9.Id,
                ProductName = p9.Name,
                Sku = p9.Sku,
                BatchNumber = "BAT-SRV-01",
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-01",
                SectionCode = "A1-T1",
                Status = StockSerialStatus.Available,
                ReceivedDate = now.AddDays(-15)
            });

            // Location balances
            foreach (var prod in _products)
            {
                _locationBalances.Add(new StockLocationBalance
                {
                    ProductId = prod.Id,
                    ProductName = prod.Name,
                    Sku = prod.Sku,
                    WarehouseId = prod.DefaultWarehouseId,
                    WarehouseName = prod.DefaultWarehouseName,
                    RackId = prod.DefaultRackId,
                    RackCode = prod.DefaultRackCode,
                    SectionId = prod.DefaultSectionId,
                    SectionCode = prod.DefaultSectionCode,
                    PhysicalQuantity = prod.PhysicalStock,
                    AvailableQuantity = prod.AvailableStock,
                    ReservedQuantity = prod.ReservedStock,
                    DamagedQuantity = prod.DamagedStock,
                    ExpiredQuantity = prod.ExpiredStock,
                    Unit = prod.Unit
                });
            }

            // Stock Movements
            _movements.Add(new StockMovement
            {
                TransactionId = "TXN-2026-0091",
                Timestamp = now.AddMinutes(-25),
                ProductId = p1.Id,
                ProductName = p1.Name,
                Sku = p1.Sku,
                Type = MovementType.StockIn,
                Quantity = 150,
                Unit = p1.Unit,
                SourceLocation = "Dock Bay #2 (Supplier PO #4092)",
                DestinationLocation = "WH-01 / RCK-A-01 / SEC-01",
                InitiatedBy = "Saumya Chaudhari",
                Status = MovementStatus.Completed,
                Remarks = "Quality acceptance passed. GRN #8820 recorded."
            });

            _movements.Add(new StockMovement
            {
                TransactionId = "TXN-2026-0090",
                Timestamp = now.AddHours(-2),
                ProductId = p2.Id,
                ProductName = p2.Name,
                Sku = p2.Sku,
                Type = MovementType.StockOut,
                Quantity = 20,
                Unit = p2.Unit,
                SourceLocation = "WH-01 / RCK-A-01 / SEC-01",
                DestinationLocation = "Shop Floor Assembly Line 4",
                InitiatedBy = "Kishan Patel",
                Status = MovementStatus.Completed,
                Remarks = "Issue voucher #REQ-9912 authorized."
            });

            // Pending Approvals
            _approvalRequests.Add(new StockApprovalRequest
            {
                RequestId = "APR-2026-001",
                TransactionType = MovementType.StockOut,
                ProductId = p1.Id,
                ProductName = p1.Name,
                Sku = p1.Sku,
                Quantity = 30,
                Unit = p1.Unit,
                WarehouseId = "WH-01",
                WarehouseName = "Central Logistics Hub - Sector 4",
                RackId = "RCK-01",
                RackCode = "RCK-A-01",
                SectionId = "SEC-01",
                SectionCode = "A1-T1",
                BatchNumber = "BAT-2026-088",
                RequestedBy = "Rohit Verma (Assembly Lead)",
                RequestedAt = now.AddHours(-1),
                Reason = "Production Requisition for Line 3 expansion",
                ReferenceNumber = "REQ-PRD-4012",
                Status = MovementStatus.PendingApproval
            });
        }

        public Task<List<Product>> GetProductsAsync(ProductFilterCriteria? filter = null, int pageIndex = 1, int pageSize = 100)
        {
            lock (_lock)
            {
                var query = _products.AsQueryable();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                    {
                        var q = filter.SearchQuery.Trim().ToLowerInvariant();
                        query = query.Where(p =>
                            p.Name.ToLowerInvariant().Contains(q) ||
                            p.Sku.ToLowerInvariant().Contains(q) ||
                            (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(q)) ||
                            p.Category.ToLowerInvariant().Contains(q));
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "All Categories")
                    {
                        query = query.Where(p => p.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));
                    }

                    if (filter.TrackingType.HasValue)
                    {
                        query = query.Where(p => p.TrackingType == filter.TrackingType.Value);
                    }

                    if (filter.Status.HasValue)
                    {
                        query = query.Where(p => p.Status == filter.Status.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.WarehouseId) && filter.WarehouseId != "All Warehouses")
                    {
                        query = query.Where(p => p.DefaultWarehouseId == filter.WarehouseId);
                    }

                    if (filter.IsActive.HasValue)
                    {
                        query = query.Where(p => p.IsActive == filter.IsActive.Value);
                    }
                }

                var list = query
                    .OrderBy(p => p.Name)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(list);
            }
        }

        public Task<int> GetProductCountAsync(ProductFilterCriteria? filter = null)
        {
            lock (_lock)
            {
                var query = _products.AsQueryable();

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                    {
                        var q = filter.SearchQuery.Trim().ToLowerInvariant();
                        query = query.Where(p =>
                            p.Name.ToLowerInvariant().Contains(q) ||
                            p.Sku.ToLowerInvariant().Contains(q) ||
                            (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(q)));
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "All Categories")
                    {
                        query = query.Where(p => p.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));
                    }

                    if (filter.TrackingType.HasValue)
                    {
                        query = query.Where(p => p.TrackingType == filter.TrackingType.Value);
                    }

                    if (filter.Status.HasValue)
                    {
                        query = query.Where(p => p.Status == filter.Status.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.WarehouseId) && filter.WarehouseId != "All Warehouses")
                    {
                        query = query.Where(p => p.DefaultWarehouseId == filter.WarehouseId);
                    }

                    if (filter.IsActive.HasValue)
                    {
                        query = query.Where(p => p.IsActive == filter.IsActive.Value);
                    }
                }

                return Task.FromResult(query.Count());
            }
        }

        public Task<Product?> GetProductByIdAsync(string productId)
        {
            lock (_lock)
            {
                var prod = _products.FirstOrDefault(p => p.Id.Equals(productId, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(prod);
            }
        }

        public Task<Product?> GetProductBySkuAsync(string sku)
        {
            lock (_lock)
            {
                var prod = _products.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(prod);
            }
        }

        public Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            lock (_lock)
            {
                var prod = _products.FirstOrDefault(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(prod);
            }
        }

        public Task<string> GenerateSkuAsync(string category, string productName)
        {
            lock (_lock)
            {
                string catCode = category switch
                {
                    "Mechanical Power Transmission" => "BRG",
                    "Fluid & Flow Control" => "VAL",
                    "Chemicals & Lubricants" => "LUB",
                    "Fasteners & Hardware" => "FST",
                    "Sensors & Automation" => "SNS",
                    "Chemical Adhesives" => "ADH",
                    "Pneumatics" => "PNU",
                    "Raw Materials" => "RAW",
                    "Motors & Drives" => "MOT",
                    _ => "GEN"
                };

                // Generate clean suffix from words in product name
                string nameCode = "ITEM";
                if (!string.IsNullOrWhiteSpace(productName))
                {
                    var words = productName.Split(new[] { ' ', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        nameCode = words[0].Length >= 4 ? words[0].Substring(0, 4).ToUpperInvariant() : words[0].ToUpperInvariant();
                    }
                }

                string candidate = $"SKU-{catCode}-{nameCode}";
                int counter = 1;

                while (_products.Any(p => p.Sku.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                {
                    candidate = $"SKU-{catCode}-{nameCode}-{counter:D2}";
                    counter++;
                }

                return Task.FromResult(candidate);
            }
        }

        public Task<bool> IsSkuUniqueAsync(string sku, string? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return Task.FromResult(false);

            lock (_lock)
            {
                bool exists = _products.Any(p =>
                    p.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    (excludeProductId == null || !p.Id.Equals(excludeProductId, StringComparison.OrdinalIgnoreCase)));
                return Task.FromResult(!exists);
            }
        }

        public Task<bool> IsBarcodeUniqueAsync(string barcode, string? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return Task.FromResult(true); // Optional barcodes are valid

            lock (_lock)
            {
                bool exists = _products.Any(p =>
                    !string.IsNullOrWhiteSpace(p.Barcode) &&
                    p.Barcode.Trim().Equals(barcode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    (excludeProductId == null || !p.Id.Equals(excludeProductId, StringComparison.OrdinalIgnoreCase)));
                return Task.FromResult(!exists);
            }
        }

        public Task<Product> CreateProductAsync(Product product)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(product.Sku))
                {
                    throw new InvalidOperationException("Product SKU is required.");
                }

                if (_products.Any(p => p.Sku.Equals(product.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Product with SKU '{product.Sku}' already exists.");
                }

                if (!string.IsNullOrWhiteSpace(product.Barcode) &&
                    _products.Any(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(product.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Product with Barcode '{product.Barcode}' already exists.");
                }

                if (string.IsNullOrWhiteSpace(product.Id))
                {
                    product.Id = $"PRD-{100 + _products.Count + 1}";
                }

                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;

                _products.Add(product);

                // Add initial location balance if default location is set
                if (!string.IsNullOrEmpty(product.DefaultWarehouseId))
                {
                    _locationBalances.Add(new StockLocationBalance
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        WarehouseId = product.DefaultWarehouseId,
                        WarehouseName = product.DefaultWarehouseName,
                        RackId = product.DefaultRackId,
                        RackCode = product.DefaultRackCode,
                        SectionId = product.DefaultSectionId,
                        SectionCode = product.DefaultSectionCode,
                        PhysicalQuantity = 0,
                        AvailableQuantity = 0,
                        Unit = product.Unit
                    });
                }

                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(product);
            }
        }

        public Task<Product> UpdateProductAsync(Product product)
        {
            lock (_lock)
            {
                var existing = _products.FirstOrDefault(p => p.Id.Equals(product.Id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    throw new InvalidOperationException($"Product with ID '{product.Id}' not found.");
                }

                // Verify SKU uniqueness
                if (!existing.Sku.Equals(product.Sku.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    _products.Any(p => p.Id != product.Id && p.Sku.Equals(product.Sku.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Product with SKU '{product.Sku}' already exists.");
                }

                // Verify barcode uniqueness if provided
                if (!string.IsNullOrWhiteSpace(product.Barcode) &&
                    _products.Any(p => p.Id != product.Id && !string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(product.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Product with Barcode '{product.Barcode}' already exists.");
                }

                existing.Name = product.Name;
                existing.Sku = product.Sku;
                existing.Barcode = product.Barcode;
                existing.Category = product.Category;
                existing.Unit = product.Unit;
                existing.Description = product.Description;
                existing.MinStockLevel = product.MinStockLevel;
                existing.ReorderLevel = product.ReorderLevel;
                existing.DefaultWarehouseId = product.DefaultWarehouseId;
                existing.DefaultWarehouseName = product.DefaultWarehouseName;
                existing.DefaultRackId = product.DefaultRackId;
                existing.DefaultRackCode = product.DefaultRackCode;
                existing.DefaultSectionId = product.DefaultSectionId;
                existing.DefaultSectionCode = product.DefaultSectionCode;
                existing.TrackingType = product.TrackingType;
                existing.RequiresExpiry = product.RequiresExpiry;
                existing.IsActive = product.IsActive;
                existing.UpdatedAt = DateTime.Now;

                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(existing);
            }
        }

        public Task<bool> ToggleProductActiveAsync(string productId)
        {
            lock (_lock)
            {
                var existing = _products.FirstOrDefault(p => p.Id.Equals(productId, StringComparison.OrdinalIgnoreCase));
                if (existing == null) return Task.FromResult(false);

                existing.IsActive = !existing.IsActive;
                existing.UpdatedAt = DateTime.Now;
                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(true);
            }
        }

        public Task<StockReceiveResult> ReceiveStockAsync(StockReceiveDto dto)
        {
            lock (_lock)
            {
                if (dto.Quantity <= 0)
                {
                    return Task.FromResult(new StockReceiveResult
                    {
                        Success = false,
                        Message = "Stock receive quantity must be greater than zero."
                    });
                }

                var product = _products.FirstOrDefault(p => p.Id.Equals(dto.ProductId, StringComparison.OrdinalIgnoreCase));
                if (product == null)
                {
                    return Task.FromResult(new StockReceiveResult
                    {
                        Success = false,
                        Message = $"Product '{dto.ProductId}' not found."
                    });
                }

                var warehouse = _warehouses.FirstOrDefault(w => w.Id == dto.WarehouseId);
                var rack = _racks.FirstOrDefault(r => r.Id == dto.RackId);
                var section = rack?.Sections.FirstOrDefault(s => s.Id == dto.SectionId);

                if (warehouse == null || rack == null || section == null)
                {
                    return Task.FromResult(new StockReceiveResult
                    {
                        Success = false,
                        Message = "Destination storage location (Warehouse, Rack, Tier Section) is invalid or inactive."
                    });
                }

                // Batch Validation
                if (product.IsBatchTracked)
                {
                    if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                    {
                        return Task.FromResult(new StockReceiveResult
                        {
                            Success = false,
                            Message = "Batch/Lot number is required for batch-tracked products."
                        });
                    }

                    if (product.RequiresExpiry)
                    {
                        if (!dto.ExpiryDate.HasValue)
                        {
                            return Task.FromResult(new StockReceiveResult
                            {
                                Success = false,
                                Message = "Expiry date is mandatory for perishable products."
                            });
                        }

                        if (dto.ExpiryDate.Value.Date <= DateTime.Today)
                        {
                            return Task.FromResult(new StockReceiveResult
                            {
                                Success = false,
                                Message = "Expiry date must be in the future. Cannot receive already expired stock."
                            });
                        }
                    }
                }

                // Serial Number Validation
                if (product.IsSerialTracked)
                {
                    int expectedCount = (int)dto.Quantity;
                    if (dto.SerialNumbers == null || dto.SerialNumbers.Count != expectedCount)
                    {
                        return Task.FromResult(new StockReceiveResult
                        {
                            Success = false,
                            Message = $"Exactly {expectedCount} unique serial numbers are required for this receipt (received: {dto.SerialNumbers?.Count ?? 0})."
                        });
                    }

                    // Check for duplicate serials in request
                    var duplicatesInRequest = dto.SerialNumbers.GroupBy(s => s.Trim(), StringComparer.OrdinalIgnoreCase)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicatesInRequest.Any())
                    {
                        return Task.FromResult(new StockReceiveResult
                        {
                            Success = false,
                            Message = $"Duplicate serial numbers submitted: {string.Join(", ", duplicatesInRequest)}."
                        });
                    }

                    // Check if any serial already exists in the registry for this product
                    foreach (var sn in dto.SerialNumbers)
                    {
                        var trimmedSn = sn.Trim();
                        if (_serialNumbers.Any(s => s.ProductId == product.Id && s.SerialNumber.Equals(trimmedSn, StringComparison.OrdinalIgnoreCase)))
                        {
                            return Task.FromResult(new StockReceiveResult
                            {
                                Success = false,
                                Message = $"Serial number '{trimmedSn}' already exists in registry for product {product.Sku}."
                            });
                        }
                    }
                }

                // 1. Process Batch Record
                if (product.IsBatchTracked && !string.IsNullOrWhiteSpace(dto.BatchNumber))
                {
                    var cleanBatch = dto.BatchNumber.Trim();
                    var existingBatch = _batches.FirstOrDefault(b =>
                        b.ProductId == product.Id &&
                        b.BatchNumber.Equals(cleanBatch, StringComparison.OrdinalIgnoreCase) &&
                        b.SectionId == section.Id);

                    if (existingBatch != null)
                    {
                        existingBatch.ReceivedQuantity += dto.Quantity;
                        existingBatch.RemainingQuantity += dto.Quantity;
                    }
                    else
                    {
                        _batches.Add(new StockBatch
                        {
                            BatchId = $"BAT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            BatchNumber = cleanBatch,
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Sku = product.Sku,
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            RackId = rack.Id,
                            RackCode = rack.RackCode,
                            SectionId = section.Id,
                            SectionCode = section.SectionCode,
                            ReceivedQuantity = dto.Quantity,
                            RemainingQuantity = dto.Quantity,
                            Unit = product.Unit,
                            MfgDate = dto.MfgDate,
                            ExpiryDate = dto.ExpiryDate,
                            ReceivedDate = DateTime.Now,
                            Supplier = dto.Supplier,
                            Status = StockStatus.Normal
                        });
                    }
                }

                // 2. Process Serial Records
                if (product.IsSerialTracked && dto.SerialNumbers != null)
                {
                    foreach (var sn in dto.SerialNumbers)
                    {
                        _serialNumbers.Add(new StockSerialNumber
                        {
                            Id = $"SN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            SerialNumber = sn.Trim(),
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Sku = product.Sku,
                            BatchNumber = dto.BatchNumber?.Trim(),
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            RackId = rack.Id,
                            RackCode = rack.RackCode,
                            SectionId = section.Id,
                            SectionCode = section.SectionCode,
                            Status = StockSerialStatus.Available,
                            ReceivedDate = DateTime.Now,
                            Notes = $"Received via {dto.ReferenceNumber}"
                        });
                    }
                }

                // 3. Update Location Balance
                var locBal = _locationBalances.FirstOrDefault(b =>
                    b.ProductId == product.Id &&
                    b.WarehouseId == warehouse.Id &&
                    b.SectionId == section.Id);

                if (locBal != null)
                {
                    locBal.PhysicalQuantity += dto.Quantity;
                    locBal.AvailableQuantity += dto.Quantity;
                }
                else
                {
                    _locationBalances.Add(new StockLocationBalance
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        RackId = rack.Id,
                        RackCode = rack.RackCode,
                        SectionId = section.Id,
                        SectionCode = section.SectionCode,
                        PhysicalQuantity = dto.Quantity,
                        AvailableQuantity = dto.Quantity,
                        Unit = product.Unit
                    });
                }

                // 4. Update Product Master Totals
                product.PhysicalStock += dto.Quantity;
                product.AvailableStock += dto.Quantity;
                product.UpdatedAt = DateTime.Now;

                // 5. Create Movement Transaction
                string txnId = $"TXN-{DateTime.Now:yyyyMMdd}-{_movements.Count + 1:D4}";
                _movements.Insert(0, new StockMovement
                {
                    TransactionId = txnId,
                    Timestamp = DateTime.Now,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Type = MovementType.StockIn,
                    Quantity = dto.Quantity,
                    Unit = product.Unit,
                    SourceLocation = string.IsNullOrWhiteSpace(dto.Supplier) ? "Supplier Goods Inward" : $"Supplier: {dto.Supplier}",
                    DestinationLocation = $"{warehouse.Code} / {rack.RackCode} / {section.SectionCode}",
                    InitiatedBy = dto.ReceivedBy,
                    Status = MovementStatus.Completed,
                    Remarks = string.IsNullOrWhiteSpace(dto.ReferenceNumber) ? "GRN Stock Inward" : $"Ref: {dto.ReferenceNumber} — {dto.Notes}"
                });

                InventoryChanged?.Invoke(this, EventArgs.Empty);

                return Task.FromResult(new StockReceiveResult
                {
                    Success = true,
                    Message = $"Successfully received {dto.Quantity:N0} {product.Unit} into {rack.RackCode} / {section.SectionCode}.",
                    TransactionId = txnId,
                    NewPhysicalStock = product.PhysicalStock,
                    NewAvailableStock = product.AvailableStock
                });
            }
        }

        public Task<StockIssueResult> RequestOrIssueStockAsync(StockIssueDto dto)
        {
            lock (_lock)
            {
                if (dto.Quantity <= 0)
                {
                    return Task.FromResult(new StockIssueResult
                    {
                        Success = false,
                        Message = "Stock issue quantity must be greater than zero."
                    });
                }

                var product = _products.FirstOrDefault(p => p.Id.Equals(dto.ProductId, StringComparison.OrdinalIgnoreCase));
                if (product == null)
                {
                    return Task.FromResult(new StockIssueResult
                    {
                        Success = false,
                        Message = $"Product '{dto.ProductId}' not found."
                    });
                }

                // Stock Availability Validation
                if (dto.Quantity > product.AvailableStock)
                {
                    return Task.FromResult(new StockIssueResult
                    {
                        Success = false,
                        Message = $"Requested quantity ({dto.Quantity:N0} {product.Unit}) exceeds available stock ({product.AvailableStock:N0} {product.Unit}). Negative inventory is strictly prohibited."
                    });
                }

                var warehouse = _warehouses.FirstOrDefault(w => w.Id == dto.WarehouseId);
                var rack = _racks.FirstOrDefault(r => r.Id == dto.RackId);
                var section = rack?.Sections.FirstOrDefault(s => s.Id == dto.SectionId);

                // Batch Validation
                StockBatch? batchToIssue = null;
                if (product.IsBatchTracked)
                {
                    if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                    {
                        // Auto FEFO selection if not explicitly provided
                        batchToIssue = _batches
                            .Where(b => b.ProductId == product.Id && b.RemainingQuantity >= dto.Quantity && !b.IsExpired)
                            .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                            .FirstOrDefault();

                        if (batchToIssue == null)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = "No single valid unexpired batch has sufficient remaining quantity to fulfill this issue."
                            });
                        }
                    }
                    else
                    {
                        batchToIssue = _batches.FirstOrDefault(b =>
                            b.ProductId == product.Id &&
                            b.BatchNumber.Equals(dto.BatchNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (batchToIssue == null)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = $"Batch '{dto.BatchNumber}' not found for product {product.Sku}."
                            });
                        }

                        if (batchToIssue.IsExpired)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = $"Batch '{dto.BatchNumber}' is expired. Expired stock cannot be issued for production or dispatch."
                            });
                        }

                        if (batchToIssue.RemainingQuantity < dto.Quantity)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = $"Batch '{dto.BatchNumber}' only has {batchToIssue.RemainingQuantity:N0} remaining, which is less than requested {dto.Quantity:N0}."
                            });
                        }
                    }
                }

                // Serial Numbers Validation
                List<StockSerialNumber> serialsToIssue = new();
                if (product.IsSerialTracked)
                {
                    int expectedCount = (int)dto.Quantity;
                    if (dto.SelectedSerialNumbers == null || dto.SelectedSerialNumbers.Count != expectedCount)
                    {
                        return Task.FromResult(new StockIssueResult
                        {
                            Success = false,
                            Message = $"Exactly {expectedCount} serial numbers must be selected (selected: {dto.SelectedSerialNumbers?.Count ?? 0})."
                        });
                    }

                    foreach (var sn in dto.SelectedSerialNumbers)
                    {
                        var match = _serialNumbers.FirstOrDefault(s =>
                            s.ProductId == product.Id &&
                            s.SerialNumber.Equals(sn.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (match == null)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = $"Serial number '{sn}' does not exist for product {product.Sku}."
                            });
                        }

                        if (match.Status != StockSerialStatus.Available)
                        {
                            return Task.FromResult(new StockIssueResult
                            {
                                Success = false,
                                Message = $"Serial number '{sn}' is currently '{match.StatusDisplay}' and cannot be issued."
                            });
                        }

                        serialsToIssue.Add(match);
                    }
                }

                // APPROVAL WORKFLOW ROUTING
                if (dto.RequiresApproval)
                {
                    string reqId = $"APR-{DateTime.Now:yyyyMMdd}-{_approvalRequests.Count + 1:D3}";
                    var approval = new StockApprovalRequest
                    {
                        RequestId = reqId,
                        TransactionType = MovementType.StockOut,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        Quantity = dto.Quantity,
                        Unit = product.Unit,
                        WarehouseId = dto.WarehouseId,
                        WarehouseName = warehouse?.Name ?? "Selected Warehouse",
                        RackId = dto.RackId,
                        RackCode = rack?.RackCode ?? "Rack",
                        SectionId = dto.SectionId,
                        SectionCode = section?.SectionCode ?? "Section",
                        BatchNumber = batchToIssue?.BatchNumber ?? dto.BatchNumber,
                        SerialNumbers = dto.SelectedSerialNumbers ?? new(),
                        RequestedBy = dto.IssuedBy,
                        RequestedAt = DateTime.Now,
                        Reason = dto.Reason,
                        ReferenceNumber = dto.ReferenceNumber,
                        Status = MovementStatus.PendingApproval
                    };

                    _approvalRequests.Add(approval);

                    // Place in Reserved state
                    product.AvailableStock -= dto.Quantity;
                    product.ReservedStock += dto.Quantity;

                    foreach (var sn in serialsToIssue)
                    {
                        sn.Status = StockSerialStatus.Reserved;
                    }

                    // Record Pending Movement Ledger Entry
                    _movements.Insert(0, new StockMovement
                    {
                        TransactionId = reqId,
                        Timestamp = DateTime.Now,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        Type = MovementType.StockOut,
                        Quantity = dto.Quantity,
                        Unit = product.Unit,
                        SourceLocation = $"{warehouse?.Code ?? "WH"} / {rack?.RackCode ?? "RCK"} / {section?.SectionCode ?? "SEC"}",
                        DestinationLocation = dto.Reason,
                        InitiatedBy = dto.IssuedBy,
                        Status = MovementStatus.PendingApproval,
                        Remarks = $"Sign-off required. Ref: {dto.ReferenceNumber}"
                    });

                    InventoryChanged?.Invoke(this, EventArgs.Empty);

                    return Task.FromResult(new StockIssueResult
                    {
                        Success = true,
                        IsPendingApproval = true,
                        ApprovalRequestId = reqId,
                        Message = $"Issue request {reqId} submitted for Manager Sign-Off. Stock reserved.",
                        NewPhysicalStock = product.PhysicalStock,
                        NewAvailableStock = product.AvailableStock
                    });
                }

                // IMMEDIATE ATOMIC ISSUE (When Approval Not Required)
                string txnId = $"TXN-{DateTime.Now:yyyyMMdd}-{_movements.Count + 1:D4}";

                if (batchToIssue != null)
                {
                    batchToIssue.RemainingQuantity -= dto.Quantity;
                }

                foreach (var sn in serialsToIssue)
                {
                    sn.Status = StockSerialStatus.Issued;
                    sn.IssuedDate = DateTime.Now;
                    sn.IssueTransactionId = txnId;
                }

                // Deduct location balance
                var locBal = _locationBalances.FirstOrDefault(b =>
                    b.ProductId == product.Id &&
                    b.WarehouseId == dto.WarehouseId &&
                    b.SectionId == dto.SectionId);

                if (locBal != null)
                {
                    locBal.PhysicalQuantity = Math.Max(0, locBal.PhysicalQuantity - dto.Quantity);
                    locBal.AvailableQuantity = Math.Max(0, locBal.AvailableQuantity - dto.Quantity);
                }

                // Deduct product master
                product.PhysicalStock = Math.Max(0, product.PhysicalStock - dto.Quantity);
                product.AvailableStock = Math.Max(0, product.AvailableStock - dto.Quantity);
                product.UpdatedAt = DateTime.Now;

                // Record Completed Movement Ledger Entry
                _movements.Insert(0, new StockMovement
                {
                    TransactionId = txnId,
                    Timestamp = DateTime.Now,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Type = MovementType.StockOut,
                    Quantity = dto.Quantity,
                    Unit = product.Unit,
                    SourceLocation = $"{warehouse?.Code ?? "WH"} / {rack?.RackCode ?? "RCK"} / {section?.SectionCode ?? "SEC"}",
                    DestinationLocation = dto.Reason,
                    InitiatedBy = dto.IssuedBy,
                    Status = MovementStatus.Completed,
                    Remarks = string.IsNullOrWhiteSpace(dto.ReferenceNumber) ? dto.Reason : $"Ref: {dto.ReferenceNumber} — {dto.Notes}"
                });

                InventoryChanged?.Invoke(this, EventArgs.Empty);

                return Task.FromResult(new StockIssueResult
                {
                    Success = true,
                    IsPendingApproval = false,
                    TransactionId = txnId,
                    Message = $"Successfully issued {dto.Quantity:N0} {product.Unit} ({dto.Reason}).",
                    NewPhysicalStock = product.PhysicalStock,
                    NewAvailableStock = product.AvailableStock
                });
            }
        }

        public Task<List<StockBatch>> GetBatchesForProductAsync(string productId, string? warehouseId = null)
        {
            lock (_lock)
            {
                var query = _batches.Where(b => b.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseId != "All Warehouses")
                {
                    query = query.Where(b => b.WarehouseId == warehouseId);
                }

                // FEFO Order: non-expired with nearest expiry first, followed by non-expiring, expired last
                var list = query
                    .OrderBy(b => b.IsExpired)
                    .ThenBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                    .ThenBy(b => b.ExpiryDate)
                    .ToList();

                return Task.FromResult(list);
            }
        }

        public Task<List<StockBatch>> GetAllBatchesAsync(string? warehouseId = null)
        {
            lock (_lock)
            {
                var query = _batches.AsQueryable();
                if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseId != "All Warehouses")
                {
                    query = query.Where(b => b.WarehouseId == warehouseId);
                }

                var list = query
                    .OrderBy(b => b.IsExpired)
                    .ThenBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                    .ThenBy(b => b.ExpiryDate)
                    .ToList();

                return Task.FromResult(list);
            }
        }

        public Task<List<StockSerialNumber>> GetSerialNumbersForProductAsync(string productId, string? warehouseId = null, StockSerialStatus? status = null)
        {
            lock (_lock)
            {
                var query = _serialNumbers.Where(s => s.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseId != "All Warehouses")
                {
                    query = query.Where(s => s.WarehouseId == warehouseId);
                }
                if (status.HasValue)
                {
                    query = query.Where(s => s.Status == status.Value);
                }

                return Task.FromResult(query.OrderBy(s => s.SerialNumber).ToList());
            }
        }

        public Task<List<StockSerialNumber>> GetAllSerialNumbersAsync(string? warehouseId = null)
        {
            lock (_lock)
            {
                var query = _serialNumbers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseId != "All Warehouses")
                {
                    query = query.Where(s => s.WarehouseId == warehouseId);
                }
                return Task.FromResult(query.OrderBy(s => s.SerialNumber).ToList());
            }
        }

        public Task<bool> IsSerialNumberUniqueAsync(string productId, string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber)) return Task.FromResult(false);
            lock (_lock)
            {
                bool exists = _serialNumbers.Any(s =>
                    s.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase) &&
                    s.SerialNumber.Equals(serialNumber.Trim(), StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(!exists);
            }
        }

        public Task<List<StockLocationBalance>> GetLocationBalancesForProductAsync(string productId)
        {
            lock (_lock)
            {
                var list = _locationBalances
                    .Where(b => b.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(b => b.WarehouseName)
                    .ThenBy(b => b.RackCode)
                    .ThenBy(b => b.SectionCode)
                    .ToList();
                return Task.FromResult(list);
            }
        }

        public Task<List<StockMovement>> GetMovementsForProductAsync(string productId, int limit = 50)
        {
            lock (_lock)
            {
                var list = _movements
                    .Where(m => m.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToList();
                return Task.FromResult(list);
            }
        }

        public Task<List<StockApprovalRequest>> GetPendingApprovalsAsync()
        {
            lock (_lock)
            {
                var list = _approvalRequests
                    .Where(a => a.Status == MovementStatus.PendingApproval)
                    .OrderByDescending(a => a.RequestedAt)
                    .ToList();
                return Task.FromResult(list);
            }
        }

        public Task<bool> ApproveIssueRequestAsync(string requestId, string approvedBy)
        {
            lock (_lock)
            {
                var request = _approvalRequests.FirstOrDefault(a => a.RequestId.Equals(requestId, StringComparison.OrdinalIgnoreCase));
                if (request == null || request.Status != MovementStatus.PendingApproval) return Task.FromResult(false);

                var product = _products.FirstOrDefault(p => p.Id == request.ProductId);
                if (product == null) return Task.FromResult(false);

                // Finalize deduction: PhysicalStock -= quantity, ReservedStock -= quantity
                product.PhysicalStock = Math.Max(0, product.PhysicalStock - request.Quantity);
                product.ReservedStock = Math.Max(0, product.ReservedStock - request.Quantity);
                product.UpdatedAt = DateTime.Now;

                // Update serials to Issued
                foreach (var snStr in request.SerialNumbers)
                {
                    var sn = _serialNumbers.FirstOrDefault(s => s.ProductId == product.Id && s.SerialNumber.Equals(snStr, StringComparison.OrdinalIgnoreCase));
                    if (sn != null)
                    {
                        sn.Status = StockSerialStatus.Issued;
                        sn.IssuedDate = DateTime.Now;
                        sn.IssueTransactionId = request.RequestId;
                    }
                }

                // Update batch remaining
                if (!string.IsNullOrWhiteSpace(request.BatchNumber))
                {
                    var batch = _batches.FirstOrDefault(b => b.ProductId == product.Id && b.BatchNumber.Equals(request.BatchNumber, StringComparison.OrdinalIgnoreCase));
                    if (batch != null)
                    {
                        batch.RemainingQuantity = Math.Max(0, batch.RemainingQuantity - request.Quantity);
                    }
                }

                request.Status = MovementStatus.Completed;
                request.ReviewedBy = approvedBy;
                request.ReviewedAt = DateTime.Now;
                request.ReviewRemarks = "Approved by Operations Lead";

                // Update corresponding movement ledger
                var movement = _movements.FirstOrDefault(m => m.TransactionId == request.RequestId);
                if (movement != null)
                {
                    movement.Status = MovementStatus.Completed;
                    movement.Remarks += $" [Approved by {approvedBy}]";
                }

                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(true);
            }
        }

        public Task<bool> RejectIssueRequestAsync(string requestId, string rejectedBy, string reason)
        {
            lock (_lock)
            {
                var request = _approvalRequests.FirstOrDefault(a => a.RequestId.Equals(requestId, StringComparison.OrdinalIgnoreCase));
                if (request == null || request.Status != MovementStatus.PendingApproval) return Task.FromResult(false);

                var product = _products.FirstOrDefault(p => p.Id == request.ProductId);
                if (product != null)
                {
                    // Restore available stock and release reserved stock
                    product.ReservedStock = Math.Max(0, product.ReservedStock - request.Quantity);
                    product.AvailableStock += request.Quantity;
                    product.UpdatedAt = DateTime.Now;
                }

                // Restore serials to Available
                foreach (var snStr in request.SerialNumbers)
                {
                    var sn = _serialNumbers.FirstOrDefault(s => s.ProductId == request.ProductId && s.SerialNumber.Equals(snStr, StringComparison.OrdinalIgnoreCase));
                    if (sn != null)
                    {
                        sn.Status = StockSerialStatus.Available;
                    }
                }

                request.Status = MovementStatus.Rejected;
                request.ReviewedBy = rejectedBy;
                request.ReviewedAt = DateTime.Now;
                request.ReviewRemarks = reason;

                // Update movement ledger
                var movement = _movements.FirstOrDefault(m => m.TransactionId == request.RequestId);
                if (movement != null)
                {
                    movement.Status = MovementStatus.Rejected;
                    movement.Remarks += $" [Rejected: {reason}]";
                }

                InventoryChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(true);
            }
        }

        public Task<List<string>> GetCategoriesAsync()
        {
            lock (_lock)
            {
                var cats = _products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
                return Task.FromResult(cats);
            }
        }

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_warehouses.ToList());
            }
        }

        public Task<List<Rack>> GetRacksByWarehouseAsync(string warehouseId)
        {
            lock (_lock)
            {
                var racks = _racks.Where(r => r.WarehouseId == warehouseId).ToList();
                return Task.FromResult(racks);
            }
        }

        public Task<List<RackSection>> GetSectionsByRackAsync(string rackId)
        {
            lock (_lock)
            {
                var rack = _racks.FirstOrDefault(r => r.Id == rackId);
                var sections = rack?.Sections.ToList() ?? new List<RackSection>();
                return Task.FromResult(sections);
            }
        }
    }
}

