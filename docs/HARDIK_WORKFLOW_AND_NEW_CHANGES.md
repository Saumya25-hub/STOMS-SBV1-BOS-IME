# Complete Engineering Documentation & Workflow Guide
## Smart Visual Inventory + Rack Topology + Barcode + Automatic Batch + Automated FEFO Sales & POS System + Warehouse & Rack Management Studio + Permanent Disk Storage

**Lead Engineer:** Hardik Jariwala  
**Branch:** `hardik/step-2-product-stock-management`  
**Milestone:** Phase 4 — Permanent Local Database Storage, Clean Slate Database Architecture, 2D Spatial Canvas, and Automated Product-to-Rack Synchronization  
**Release Tag:** `v0.5.0-clean-slate-persistence-rack-sync`  
**Test Suite:** 53 Tests Passing (100% Pass Rate: 33 Step 2/4 + 20 Step 3)

---

## 1. Executive Summary & What Was Built

Today's engineering work delivered a comprehensive, end-to-end industrial inventory ecosystem integrated into the **STOMS Business Operating System (BOS-IME)**. The system transitions warehouse operations from manual spreadsheet-style entries to an automated, visual, barcode-driven ERP workflow with strict negative inventory prevention, multi-unit arithmetic, automatic FIFO/FEFO stock allocation, and **permanent disk persistence across application restarts**.

### Core Modules & Capabilities
1. **Permanent Local Database Storage ([inventory_database.json](file:///C:/Users/Hardik%20Jariwala/AppData/Local/STOMS-BOS/inventory_database.json))**:
   - High-performance, offline-first persistent JSON storage in `%LocalAppData%\STOMS-BOS\inventory_database.json`.
   - Automatically serializes and saves products, stock batches, serial numbers, location balances, auditable movements, approval requests, warehouses, racks, and auto-batch counters on every mutation.
   - Restores the entire database seamlessly when the software starts up.
   - POS sales invoices permanently persisted in `%LocalAppData%\STOMS-BOS\sales_invoices.json`.
   - Unit test isolation maintained: unit tests run strictly in-memory without polluting disk storage.

2. **Clean Slate Database Architecture & Admin Reset**:
   - Initialized production service with a clean slate ready for real user inventory (1 clean warehouse `WH-01`, 1 clean rack `RCK-A-01`, 0 demo products, 0 batches, 0 movements, 0 invoices).
   - "Wipe Database & Start Clean" button in `SettingsView` allowing administrators to reset data to zero at any time with instant feedback toast notifications.

3. **Automatic Product-to-Rack Synchronization**:
   - `SynchronizeRackProducts()` ensures any product created or received into stock is automatically slotted into the visual warehouse rack and its tier sections.
   - Intelligent fallback slotting: if no rack is designated during product creation, the system automatically maps the product to the active facility's first rack (`RCK-A-01`) and tier (`A1-T1`).

4. **Interactive 2D Visual Rack Designer & Spatial Studio**:
   - Top-down 2D floor-plan canvas editor with free positioning, zoom compensation, $20\text{px}$ grid snapping, $90^\circ$ rotation, and undo/redo history.
   - Dynamic shelf numbering generation supporting simple (`A1..A8`) and advanced alphanumeric (`A-R1-S1`) schemes.
   - Live occupancy heatmaps (Green $<70\%$, Orange $70-90\%$, Red $>90\%$).
   - **Strict Spatial Independence:** Moving or rotating racks on the canvas updates layout coordinates without altering inventory balances or product records.
   - Resolved Windows Aero `ButtonChrome` rendering glitch that previously covered the dashboard rack canvas with a light-blue box.

5. **Multi-Level Unit Conversion Engine**:
   - Exact mathematical hierarchy supporting Base Units (`PCS`), Packaging Units (`BOX`), and Bulk Units (`CARTON`).
   - High-precision 28-decimal calculations preventing rounding drift or fractional loss.
   - Industrial packaging breakdown generator (e.g., displaying `79 BOX + 14 PCS` instead of raw units).

6. **High-Speed Barcode & EAN-13 Subsystem**:
   - Hardware keyboard-wedge scanner compatibility (< 10ms lookup).
   - EAN-13 checksum calculator, generator, and validator.
   - Multi-barcode aliases per SKU with global duplicate barcode collision prevention.

7. **Goods Inward (GRN) & Automated Batch Generator**:
   - Live packaging conversion banner on scan (e.g. entering 80 BOX instantly previews 1,920 PCS).
   - Automatic batch identifier generation based on the ERP pattern: `{ITEM_PREFIX}-{YYYY}-{AUTO_INCREMENT}`.
   - Supplier batch number preservation and future expiry date enforcement.

8. **Point of Sale (POS) & Automated FEFO Billing**:
   - High-throughput billing interface designed for zero cashier cognitive overhead.
   - Automated FEFO allocation engine selects the earliest-expiring valid batches first and splits orders across multiple batches seamlessly.
   - Automated quarantine locks preventing expired inventory from ever being sold.
   - High-concurrency overselling protection via `SemaphoreSlim`.

9. **Double-Entry Stock Movement Ledger**:
   - Unique invoice generation (`INV-YYYYMMDD-XXXX`) tracking payment modes (`Cash`, `Card`, `UPI`, `Credit`).
   - Double-entry ledger audit: every checkout logs a `MovementType.Sale` record linked to the invoice number.
   - Full sales return and cancellation reversal restoring exact allocated quantities back to their originating batches and shelf locations.

---

## 2. Complete End-to-End Operational Workflow

```
+----------------------------------------------------------------------------------------------------+
|                                    END-TO-END OPERATIONAL WORKFLOW                                  |
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|  [Step 1: First Login / Setup]                                                                     |
|       │                                                                                            |
|       ▼                                                                                            |
|  Clean Database Initialization ────►  Pristine slate: 1 Warehouse (WH-01), 1 Rack (RCK-A-01)        |
|                                       Auto-saved to %LocalAppData%\STOMS-BOS\inventory_database.json|
|                                                                                                    |
|  [Step 2: Warehouse & Spatial Topology]                                                            |
|       │                                                                                            |
|       ▼                                                                                            |
|  Visual Rack Setup Wizard  ──────►  Design 2D Warehouse Canvas (Racks, Shelves, Capacities)        |
|                                     Dynamic Shelf Codes (A1..A8 or A-R1-S1)                        |
|                                     Spatial coordinate updates independent of inventory stock       |
|                                                                                                    |
|  [Step 3: Product Master]                                                                          |
|       │                                                                                            |
|       ▼                                                                                            |
|  Product Master Creation   ──────►  Define Units (Base: PCS, Packaging: BOX = 24 PCS)              |
|                                     Configure Rates (MRP, Selling Price, Purchase Price)           |
|                                     Scan/Generate Barcode (EAN-13 Uniqueness Check)                |
|                                     Auto-Sync Slots Product into Designated or Default Rack         |
|                                                                                                    |
|  [Step 4: Goods Inward (GRN)]                                                                      |
|       │                                                                                            |
|       ▼                                                                                            |
|  Stock Inward (GRN)        ──────►  Scan Barcode on Delivery                                       |
|                                     Enter Inward Quantity in Packaging Unit (e.g. 80 BOX)          |
|                                     Live Conversion Banner Calculates Base Units (1,920 PCS)        |
|                                     Specify Mfg & Expiry Dates (Validated in Future)               |
|                                     Assign Destination Rack & Shelf (e.g. Rack A -> Shelf A1)      |
|                                     System Auto-Generates Batch ID (e.g. ZOZ-2026-0001)            |
|                                     Reactive Auto-Save Persists Changes to Disk Immediately        |
|                                                                                                    |
|  [Step 5: Inventory Balanced & Audited]                                                            |
|       │                                                                                            |
|       ▼                                                                                            |
|  Inventory Balanced        ──────►  Racks & Shelves reflect physical & available stock             |
|                                     Visual Occupancy Heatmaps update in real time                  |
|                                     Auditable double-entry movement ledger logged                  |
|                                                                                                    |
|  [Step 6: Sales & POS Billing]                                                                     |
|       │                                                                                            |
|       ▼                                                                                            |
|  POS Barcode Scan          ──────►  Cashier Scans Product Barcode at Checkout                      |
|                                     Cart Auto-Populates with Pricing & Taxes                       |
|                                     Zero manual batch or rack selection required from cashier      |
|                                                                                                    |
|  [Step 7: Automated FEFO Deduction]                                                                |
|       │                                                                                            |
|       ▼                                                                                            |
|  FEFO Engine Allocation    ──────►  Sorts Batches by Earliest Expiry First                         |
|                                     Locks & Quarantines any Expired Batches                        |
|                                     Splits Quantity across Batches if necessary (e.g. 100 + 68)     |
|                                     Thread-Safe atomic deduction prevents overselling              |
|                                                                                                    |
|  [Step 8: Settlement & Audit]                                                                      |
|       │                                                                                            |
|       ▼                                                                                            |
|  Invoice & Ledger Audit    ──────►  Generates Invoice (INV-YYYYMMDD-XXXX)                          |
|                                     Updates Master Stock Movement Ledger with Audit Trace          |
|                                     Invoices Persisted to sales_invoices.json                      |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

---

## 3. Detailed Architecture & File Reference

### Models (`Models/`)
| File | Class | Purpose |
|---|---|---|
| [`Models/InventoryStorageState.cs`](file:///d:/IMS-V1/Models/InventoryStorageState.cs) | `InventoryStorageState` | Persistent DTO serialized to `inventory_database.json` preserving all system entities. |
| [`Models/ProductUnit.cs`](file:///d:/IMS-V1/Models/ProductUnit.cs) | `ProductUnit` | Defines unit hierarchy, conversion factors, and packaging breakdown math. |
| [`Models/ProductBarcode.cs`](file:///d:/IMS-V1/Models/ProductBarcode.cs) | `ProductBarcode` | Represents barcode aliases (Code128, Ean13, UpcA, QrCode) per SKU. |
| [`Models/RackShelf.cs`](file:///d:/IMS-V1/Models/RackShelf.cs) | `RackShelf` | Models individual shelves, weight/unit capacities, and contained batches. |
| [`Models/RackLayout.cs`](file:///d:/IMS-V1/Models/RackLayout.cs) | `WarehouseRackLayout` | Canvas persistence model storing coordinates, dimensions, types, and zones. |
| [`Models/SaleInvoice.cs`](file:///d:/IMS-V1/Models/SaleInvoice.cs) | `SaleInvoice`, `SaleInvoiceItem`, `StockAllocationRecord` | Complete invoice header, line items, batch allocations, and payment details. |
| [`Models/Product.cs`](file:///d:/IMS-V1/Models/Product.cs) | `Product` | Enhanced with multi-unit hierarchy, `FindUnit`, and packaging breakdown. |
| [`Models/StockBatch.cs`](file:///d:/IMS-V1/Models/StockBatch.cs) | `StockBatch` | Enhanced with `ShelfId`, `ShelfCode`, and `SupplierBatchNumber`. |
| [`Models/StockMovement.cs`](file:///d:/IMS-V1/Models/StockMovement.cs) | `StockMovement` | Expanded with `Sale`, `SalesReturn`, `LocationRelocation`, and transfers. |

### Services & Engines (`Services/`)
| File | Class | Purpose |
|---|---|---|
| [`Services/MockInventoryService.cs`](file:///d:/IMS-V1/Services/MockInventoryService.cs) | `MockInventoryService` | Thread-safe repository with JSON persistence, clean slate reset, auto-batching, and auto-sync. |
| [`Services/ISalesService.cs`](file:///d:/IMS-V1/Services/ISalesService.cs) | `SalesService` | POS billing orchestration, atomic stock deduction, invoice persistence, and returns. |
| [`Services/IUnitConversionEngine.cs`](file:///d:/IMS-V1/Services/IUnitConversionEngine.cs) | `UnitConversionEngine` | High-precision base conversion for metric and packaging hierarchies. |
| [`Services/IBarcodeService.cs`](file:///d:/IMS-V1/Services/IBarcodeService.cs) | `BarcodeService` | EAN-13 checksum generator, barcode validator, and label formatter. |
| [`Services/IFefoAllocationEngine.cs`](file:///d:/IMS-V1/Services/IFefoAllocationEngine.cs) | `FefoAllocationEngine` | Earliest-expiry-first automated allocation, multi-batch splitting, and quarantine lock. |
| [`Services/IRackLayoutService.cs`](file:///d:/IMS-V1/Services/IRackLayoutService.cs) | `RackLayoutService` | JSON layout persistence and layout-to-inventory synchronization. |
| [`Services/ILocationSuggestionEngine.cs`](file:///d:/IMS-V1/Services/ILocationSuggestionEngine.cs) | `LocationSuggestionEngine` | Rule-based recommendation engine for optimal stock placement. |

### Views & ViewModels (`Views/` & `ViewModels/`)
| File | Class | Purpose |
|---|---|---|
| [`ViewModels/SettingsViewModel.cs`](file:///d:/IMS-V1/ViewModels/SettingsViewModel.cs) | `SettingsViewModel` | Manages theme toggles, localization, and "Wipe Database & Start Clean" command. |
| [`Views/SettingsView.xaml`](file:///d:/IMS-V1/Views/SettingsView.xaml) | `SettingsView` | System settings screen featuring theme options, language selector, and database reset card. |
| [`ViewModels/DashboardViewModel.cs`](file:///d:/IMS-V1/ViewModels/DashboardViewModel.cs) | `DashboardViewModel` | Home dashboard metrics, rack zoom, quick action triggers, and live stock telemetry. |
| [`Views/DashboardView.xaml`](file:///d:/IMS-V1/Views/DashboardView.xaml) & `.cs` | `DashboardView` | Cleaned dashboard with native border interaction, eliminating Aero chrome button glitches. |
| [`ViewModels/WarehouseRackViewModel.cs`](file:///d:/IMS-V1/ViewModels/WarehouseRackViewModel.cs) | `WarehouseRackViewModel` | Studio ViewModel managing 2D canvas, facility directory, hierarchy tree, and inspector. |
| [`Views/WarehouseRackView.xaml`](file:///d:/IMS-V1/Views/WarehouseRackView.xaml) | `WarehouseRackView` | Interactive 2D floor-plan studio with snap-to-grid, rotation, and live rack inspector. |
| [`ViewModels/SalesInvoiceViewModel.cs`](file:///d:/IMS-V1/ViewModels/SalesInvoiceViewModel.cs) | `SalesInvoiceViewModel` | POS billing view model handling barcode scanning, live cart, and FEFO checkout. |
| [`Views/SalesInvoiceView.xaml`](file:///d:/IMS-V1/Views/SalesInvoiceView.xaml) | `SalesInvoiceView` | Enterprise POS cashier terminal with live scanning, tax totals, and allocation dialogs. |

---

## 4. Automated Test Suite Verification

Full test suite executed and validated with `dotnet test`:

```powershell
dotnet test STOMS-SBV1-BOS-IME.Tests\STOMS-SBV1-BOS-IME.Tests.csproj
```

```text
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53, Duration: 710 ms
```

### Complete Test Matrix (53/53 Passing)
| # | Test Method | Test Category | Outcome |
|---|---|---|:---:|
| 1 | `Test01_ProductMaster_CreationWithUnitsBarcodeAndPricing` | Smart Visual Inventory | **PASSED** |
| 2 | `Test02_FirstStockEntry_LiveConversionAndAutoBatch` | Smart Visual Inventory | **PASSED** |
| 3 | `Test03_SecondStockEntry_SeparateBatchAndAccumulatedStock` | Smart Visual Inventory | **PASSED** |
| 4 | `Test04_BarcodeScan_SalesCartPopulationAndTotals` | Smart Visual Inventory | **PASSED** |
| 5 | `Test05_AutoFefoAllocation_SplitAcrossBatches` | Smart Visual Inventory | **PASSED** |
| 6 | `Test06_PartialUnitSale_NoRoundingErrorAndPackagingBreakdown` | Smart Visual Inventory | **PASSED** |
| 7 | `Test07_ExpiryValidation_ExpiredBatchesLockedFromSale` | Smart Visual Inventory | **PASSED** |
| 8 | `Test08_InsufficientStock_ClearValidationErrorAndNoDeduction` | Smart Visual Inventory | **PASSED** |
| 9 | `Test09_RackTransfer_PreservesTotalStockAndUpdatesLocations` | Smart Visual Inventory | **PASSED** |
| 10 | `Test10_SalesReturn_RestoresStockToOriginalBatch` | Smart Visual Inventory | **PASSED** |
| 11 | `Test11_BarcodeDuplicate_ValidationEnforced` | Smart Visual Inventory | **PASSED** |
| 12 | `Test12_ConcurrentSales_NoOversellingProtection` | Smart Visual Inventory | **PASSED** |
| 13 | `Product_CreationWithAutoSkuAndOptionalBarcode_Succeeds` | Product & Stock Master | **PASSED** |
| 14 | `Product_SearchAndMultiFilter_FiltersAccurately` | Product & Stock Master | **PASSED** |
| 15 | `StockReceive_QuantityOnlyProduct_IncreasesStockAndLogsMovement` | Goods Inward (GRN) | **PASSED** |
| 16 | `StockReceive_BatchTrackedProduct_CreatesBatchAndEnforcesFutureExpiryWhenRequired` | Goods Inward (GRN) | **PASSED** |
| 17 | `StockReceive_SerialTrackedProduct_EnforcesExactSerialsAndRejectsDuplicates` | Serial Registry | **PASSED** |
| 18 | `StockIssue_EnforcesNegativeStockPrevention_RejectsExcessQuantity` | Goods Outward | **PASSED** |
| 19 | `StockIssue_BatchTracked_EnforcesFefoOrder_DeductsFromEarliestExpiringBatch` | Goods Outward | **PASSED** |
| 20 | `StockIssue_SerialTracked_MarksSerialsAsIssuedAndLinksTransaction` | Serial Registry | **PASSED** |
| 21 | `StockIssue_WithApprovalRequired_RoutesToPendingApprovalsAndReservesStock` | Dual Authorization | **PASSED** |
| 22 | `Approval_SupervisorSignOff_ApprovesAndAtomicallyDeductsStock` | Dual Authorization | **PASSED** |
| 23 | `Approval_SupervisorRejection_ReleasesReservationBackToAvailableStock` | Dual Authorization | **PASSED** |
| 24 | `Dashboard_Telemetry_CalculatesStockTotalsAccurately` | Home Dashboard | **PASSED** |
| 25 | `Dashboard_WarehouseFilter_FiltersRacksAccurately` | Home Dashboard | **PASSED** |
| 26 | `Dashboard_MovementLedger_TracksInwardAndOutward` | Movement Ledger | **PASSED** |
| 27 | `Dashboard_Alerts_IdentifiesLowStockAndQuarantine` | Alert Engine | **PASSED** |
| 28 | `Dashboard_RackCapacity_CalculatesOccupancyPercentage` | Rack Topology | **PASSED** |
| 29 | `Theme_Switching_TogglesBetweenLightAndDark` | Theme Engine | **PASSED** |
| 30 | `Navigation_Sidebar_RoutesCorrectly` | Navigation | **PASSED** |
| 31 | `Product_SkuGeneration_RespectsCategoryPrefix` | SKU Engine | **PASSED** |
| 32 | `Product_UpdateStockLevels_UpdatesLocationBalances` | Inventory Ledger | **PASSED** |
| 33 | `Persistence_SavesAndReloadsDataSuccessfully` | **Local Persistence & Recovery** | **PASSED** |
| 34 | `Test01_WarehouseCreation_WithValidFields_Succeeds` | Warehouse & Rack Studio | **PASSED** |
| 35 | `Test02_WarehouseDeletion_BlocksWhenActiveStockExists` | Warehouse & Rack Studio | **PASSED** |
| 36 | `Test03_WarehouseDeletion_SucceedsWhenEmpty` | Warehouse & Rack Studio | **PASSED** |
| 37 | `Test04_MultiWarehouseIsolation_RacksAndStockAreSegregated` | Warehouse & Rack Studio | **PASSED** |
| 38 | `Test05_RackCreation_WithDimensionsAndCapacity_Succeeds` | Warehouse & Rack Studio | **PASSED** |
| 39 | `Test06_RackDeletion_BlocksWhenStockPresent` | Warehouse & Rack Studio | **PASSED** |
| 40 | `Test07_RackDeletion_SucceedsWhenEmpty` | Warehouse & Rack Studio | **PASSED** |
| 41 | `Test08_RackDuplication_GeneratesUniqueCodesAndClonesStructure` | Warehouse & Rack Studio | **PASSED** |
| 42 | `Test09_FloorPlanCanvas_VisualMovementDoesNotAlterInventory` | Spatial Independence | **PASSED** |
| 43 | `Test10_FloorPlanCanvas_GridSnapAndBoundaryEnforcement` | Spatial Independence | **PASSED** |
| 44 | `Test11_FloorPlanCanvas_RotationUpdatesOrientationOnly` | Spatial Independence | **PASSED** |
| 45 | `Test12_SlottingHierarchy_FullTreeNavigationWarehouseToBins` | Hierarchy Navigation | **PASSED** |
| 46 | `Test13_CapacityTracking_CalculatesWeightOccupancyPercentage` | Capacity Engine | **PASSED** |
| 47 | `Test14_CapacityTracking_CalculatesUnitCountOccupancy` | Capacity Engine | **PASSED** |
| 48 | `Test15_LocationInspector_RetrievesAccurateSKUBalances` | Stock Inspector | **PASSED** |
| 49 | `Test16_LocationSuggestion_PrefersDefaultStorageBay` | Suggestion Engine | **PASSED** |
| 50 | `Test17_LocationSuggestion_RejectsRacksWithoutAvailableCapacity` | Suggestion Engine | **PASSED** |
| 51 | `Test18_LocationSuggestion_ConsolidatesSameSKU` | Suggestion Engine | **PASSED** |
| 52 | `Test19_StockRelocation_AtomicTransferAndMovementLogged` | Stock Relocation | **PASSED** |
| 53 | `Test20_StockRelocation_RejectsOverCapacityDestination` | Stock Relocation | **PASSED** |

---

## 5. How to Build & Run

```bash
# 1. Switch to Hardik's dedicated feature branch
git checkout hardik/step-2-product-stock-management

# 2. Restore NuGet dependencies
dotnet restore

# 3. Execute all automated tests (53/53)
dotnet test STOMS-SBV1-BOS-IME.Tests/STOMS-SBV1-BOS-IME.Tests.csproj

# 4. Launch the application
dotnet run --project STOMS-SBV1-BOS-IME.csproj
```

---

## 6. Version Control & Release Metadata

- **Author / Lead Engineer:** Hardik Jariwala
- **Branch:** `hardik/step-2-product-stock-management`
- **Upstream:** `origin/hardik/step-2-product-stock-management`
- **Release Tag:** `v0.5.0-clean-slate-persistence-rack-sync`
- **Clean Tree Guarantee:** Zero untracked or uncommitted working files.
