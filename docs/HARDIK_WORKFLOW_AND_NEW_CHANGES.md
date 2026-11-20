# Complete Engineering Documentation & Workflow Guide
## Smart Visual Inventory + Rack Topology + Barcode + Automatic Batch + Automated FEFO Sales & POS System + Warehouse & Rack Management Studio

**Lead Engineer:** Hardik Jariwala  
**Branch:** `hardik/step-2-product-stock-management`  
**Milestone:** Phase 3 — Production Warehouse & Rack Management Studio, Multi-Level Hierarchy, Spatial Canvas, and Stock Relocation  
**Release Tag:** `v0.4.0-warehouse-rack-management`  
**Test Suite:** 52 Tests Passing (100% Pass Rate: 32 Step 2 + 20 Step 3)

---

## 1. Executive Summary & What Was Built

Today's engineering sprint delivered a comprehensive, end-to-end industrial inventory ecosystem integrated into the **STOMS Business Operating System (BOS-IME)**. The system transitions warehouse operations from manual spreadsheet-style entries to an automated, visual, barcode-driven ERP workflow with strict negative inventory prevention, multi-unit arithmetic, and automatic FIFO/FEFO stock allocation.

### Core Modules Developed
1. **Interactive 2D Visual Rack Designer & Setup Wizard**:
   - First-login guided rack topology builder.
   - Toolbox with drag-and-drop elements (`Rack`, `Shelf`, `User Position`, `Storage Area`, `Custom Label`).
   - Dynamic shelf numbering generation supporting simple (`A1..A8`) and advanced alphanumeric (`A-R1-S1`) schemes.
   - Live occupancy heatmaps (Green $<70\%$, Orange $70-90\%$, Red $>90\%$).
   - Stock protection guards preventing accidental deletion of occupied racks or shelves.
   - Full JSON persistence via `IRackLayoutService` with bi-directional synchronization into the inventory master ledger.

2. **Multi-Level Unit Conversion Engine**:
   - Exact mathematical hierarchy supporting Base Units (`PCS`), Packaging Units (`BOX`), and Bulk Units (`CARTON`).
   - High-precision 28-decimal calculations preventing rounding drift or fractional penny loss.
   - Industrial packaging breakdown generator (e.g., displaying `79 BOX + 14 PCS` instead of raw units).

3. **High-Speed Barcode & EAN-13 Subsystem**:
   - Hardware keyboard-wedge scanner compatibility (< 10ms lookup).
   - EAN-13 checksum calculator, generator, and validator.
   - Multi-barcode aliases per SKU with global duplicate barcode collision prevention.

4. **Goods Inward (GRN) & Automated Batch Generator**:
   - Live packaging conversion banner on scan (e.g. entering 80 BOX instantly previews 1,920 PCS).
   - Automatic batch identifier generation based on the ERP pattern: `{ITEM_PREFIX}-{YYYY}-{AUTO_INCREMENT}`.
   - Supplier batch number preservation.
   - Perishable stock validation ensuring expiry dates are in the future and strictly after manufacturing dates.

5. **Point of Sale (POS) & Automated FEFO Billing**:
   - High-throughput billing interface designed for zero cashier cognitive overhead.
   - Scanning an item automatically increments quantity, applies tax, and calculates line totals without requiring the cashier to pick batches or shelves manually.
   - Automated FEFO allocation engine selects the earliest-expiring valid batches first and splits orders across multiple batches seamlessly (e.g. 168 units $\rightarrow$ 100 from Batch 1, 68 from Batch 2).
   - Automated quarantine locks preventing expired inventory from ever being sold.
   - High-concurrency overselling protection via `SemaphoreSlim`.

6. **Invoice Generation & Double-Entry Stock Movement Ledger**:
   - Unique invoice generation (`INV-YYYYMMDD-XXXX`) tracking payment modes (`Cash`, `Card`, `UPI`, `Credit`).
   - Double-entry ledger audit: every checkout logs a `MovementType.Sale` record linked to the invoice number.
   - Full sales return and cancellation reversal restoring exact allocated quantities back to their originating batches and shelf locations.

7. **Complete UI/UX & Pop-up Solidification Overhaul**:
   - Completely resolved popup translucency / backdrop transparency bug where background data tables bled through modal dialogs.
   - Solid, opaque modal containers (`Theme.Surface.Modal`: `#151E2E` Dark, `#FFFFFF` Light).
   - Dark/Light theme engine harmonization with persistent state and bilingual support.
   - Added missing vector geometry (`Icon.Cart`) for POS navigation.

8. **Warehouse & Rack Management Studio (Step 3)**:
   - Multi-warehouse facility management with safe-deletion protection (blocks deletion when active stock exists).
   - Flexible non-uniform physical hierarchy: `Warehouse -> Rack -> Section -> Sub-section -> Compartment -> Bin -> Products`.
   - Interactive 2D floor-plan canvas editor with free positioning, zoom compensation, $20\text{px}$ grid snapping, $90^\circ$ rotation, and undo/redo history.
   - Spatial independence: Visual rack movement updates coordinates only, never modifying product quantities or inventory ledger records.
   - Rule-based Storage Location Suggestion Engine (`ILocationSuggestionEngine`) with scoring for default bays, capacity, SKU consolidation, and quarantine locks.
   - Location Stock Inspector and Stock Relocation workflow with double-entry ledger logging (`MovementType.LocationRelocation`).

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
|  Visual Rack Setup Wizard  ──────►  Design 2D Warehouse Canvas (Racks, Shelves, Capacities)        |
|                                     └─► Dynamic Shelf Codes (A1..A8 or A-R1-S1)                    |
|                                                                                                    |
|  [Step 2: Product Master]                                                                          |
|       │                                                                                            |
|       ▼                                                                                            |
|  Product Master Creation   ──────►  Define Units (Base: PCS, Packaging: BOX = 24 PCS)              |
|                                     Configure Rates (MRP, Selling Price, Purchase Price)           |
|                                     Scan/Generate Barcode (EAN-13 Uniqueness Check)                |
|                                                                                                    |
|  [Step 3: Goods Inward]                                                                            |
|       │                                                                                            |
|       ▼                                                                                            |
|  Stock Inward (GRN)        ──────►  Scan Barcode on Delivery                                       |
|                                     Enter Inward Quantity in Packaging Unit (e.g. 80 BOX)          |
|                                     Live Conversion Banner Calculates Base Units (1,920 PCS)        |
|                                     Specify Mfg & Expiry Dates (Validated in Future)               |
|                                     Assign Destination Rack & Shelf (e.g. Rack A -> Shelf A1)      |
|                                     System Auto-Generates Batch ID (e.g. ZOZ-2026-0001)            |
|                                                                                                    |
|  [Step 4: Inventory Ready]                                                                         |
|       │                                                                                            |
|       ▼                                                                                            |
|  Inventory Balanced        ──────►  Racks & Shelves reflect physical & available stock             |
|                                     Visual Occupancy Heatmaps update in real time                  |
|                                                                                                    |
|  [Step 5: Sales & Billing]                                                                         |
|       │                                                                                            |
|       ▼                                                                                            |
|  POS Barcode Scan          ──────►  Cashier Scans Product Barcode at Checkout                      |
|                                     Cart Auto-Populates with Pricing & Taxes                       |
|                                     Zero manual batch or rack selection required from cashier      |
|                                                                                                    |
|  [Step 6: Automated FEFO Deduction]                                                                |
|       │                                                                                            |
|       ▼                                                                                            |
|  FEFO Engine Allocation    ──────►  Sorts Batches by Earliest Expiry First                         |
|                                     Locks & Quarantines any Expired Batches                        |
|                                     Splits Quantity across Batches if necessary (e.g. 100 + 68)     |
|                                     Thread-Safe atomic deduction prevents overselling              |
|                                                                                                    |
|  [Step 7: Settlement & Audit]                                                                      |
|       │                                                                                            |
|       ▼                                                                                            |
|  Invoice & Ledger Audit    ──────►  Generates Invoice (INV-YYYYMMDD-XXXX)                          |
|                                     Updates Master Stock Movement Ledger with Audit Trace          |
|                                     Allocation Breakdown Modal displays exact batch/shelf origin   |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

---

## 3. Detailed Architecture & File Reference

### Models (`Models/`)
| File | Class | Purpose |
|---|---|---|
| [`Models/ProductUnit.cs`](file:///d:/IMS-V1/Models/ProductUnit.cs) | `ProductUnit` | Defines unit hierarchy, conversion factors, and packaging breakdown math. |
| [`Models/ProductBarcode.cs`](file:///d:/IMS-V1/Models/ProductBarcode.cs) | `ProductBarcode` | Represents barcode aliases (Code128, Ean13, UpcA, QrCode) per SKU. |
| [`Models/RackShelf.cs`](file:///d:/IMS-V1/Models/RackShelf.cs) | `RackShelf` | Models individual shelves, weight/unit capacities, and contained batches. |
| [`Models/RackLayout.cs`](file:///d:/IMS-V1/Models/RackLayout.cs) | `WarehouseRackLayout` | Canvas persistence model storing coordinates, dimensions, types, and zones. |
| [`Models/SaleInvoice.cs`](file:///d:/IMS-V1/Models/SaleInvoice.cs) | `SaleInvoice`, `SaleInvoiceItem`, `StockAllocationRecord` | Complete invoice header, line items, batch allocations, and payment details. |
| [`Models/Product.cs`](file:///d:/IMS-V1/Models/Product.cs) | `Product` | Enhanced with multi-unit hierarchy, `FindUnit`, and packaging breakdown. |
| [`Models/StockBatch.cs`](file:///d:/IMS-V1/Models/StockBatch.cs) | `StockBatch` | Enhanced with `ShelfId`, `ShelfCode`, and `SupplierBatchNumber`. |
| [`Models/StockMovement.cs`](file:///d:/IMS-V1/Models/StockMovement.cs) | `StockMovement` | Expanded with `Sale`, `SalesReturn`, `RackTransfer`, and `ShelfTransfer`. |

### Services & Engines (`Services/`)
| File | Class | Purpose |
|---|---|---|
| [`Services/IUnitConversionEngine.cs`](file:///d:/IMS-V1/Services/IUnitConversionEngine.cs) | `UnitConversionEngine` | High-precision base conversion for metric and packaging hierarchies. |
| [`Services/IBarcodeService.cs`](file:///d:/IMS-V1/Services/IBarcodeService.cs) | `BarcodeService` | EAN-13 checksum generator, barcode validator, and label formatter. |
| [`Services/IFefoAllocationEngine.cs`](file:///d:/IMS-V1/Services/IFefoAllocationEngine.cs) | `FefoAllocationEngine` | Earliest-expiry-first automated allocation, multi-batch splitting, and quarantine lock. |
| [`Services/ISalesService.cs`](file:///d:/IMS-V1/Services/ISalesService.cs) | `SalesService` | POS billing orchestration, thread-safe stock deduction, and returns. |
| [`Services/IRackLayoutService.cs`](file:///d:/IMS-V1/Services/IRackLayoutService.cs) | `RackLayoutService` | JSON layout persistence and layout-to-inventory synchronization. |
| [`Services/MockInventoryService.cs`](file:///d:/IMS-V1/Services/MockInventoryService.cs) | `MockInventoryService` | In-memory master repository with auto-batching, shelf resolution, and ledger audits. |

### Views & ViewModels (`Views/` & `ViewModels/`)
| File | Class | Purpose |
|---|---|---|
| [`ViewModels/RackDesignerViewModel.cs`](file:///d:/IMS-V1/ViewModels/RackDesignerViewModel.cs) | `RackDesignerViewModel` | Manages visual canvas toolbox, layout items, shelf generation, and save workflows. |
| [`Views/RackDesignerWizardView.xaml`](file:///d:/IMS-V1/Views/RackDesignerWizardView.xaml) | `RackDesignerWizardView` | Dark blueprint warehouse canvas with drag-and-drop toolbox and properties drawer. |
| [`ViewModels/SalesInvoiceViewModel.cs`](file:///d:/IMS-V1/ViewModels/SalesInvoiceViewModel.cs) | `SalesInvoiceViewModel` | POS billing view model handling barcode scanning, live cart, and FEFO checkout. |
| [`Views/SalesInvoiceView.xaml`](file:///d:/IMS-V1/Views/SalesInvoiceView.xaml) | `SalesInvoiceView` | Enterprise POS cashier terminal with live scanning, tax totals, and allocation dialogs. |
| [`Views/ProductFormDialog.xaml`](file:///d:/IMS-V1/Views/ProductFormDialog.xaml) | `ProductFormDialog` | Solid opaque Add/Edit product modal with auto-SKU and EAN barcode generators. |
| [`Views/StockReceiveDialog.xaml`](file:///d:/IMS-V1/Views/StockReceiveDialog.xaml) | `StockReceiveDialog` | Solid Goods Inward modal with live unit conversion preview and batch generation. |
| [`Views/StockIssueDialog.xaml`](file:///d:/IMS-V1/Views/StockIssueDialog.xaml) | `StockIssueDialog` | Solid Goods Outward modal with FEFO batch selection and supervisor sign-offs. |

---

## 4. UI/UX Solidification & Bug Fixes

### 1. Transparent Popup Elimination
- **Issue:** Modal popups (`ProductFormDialog`, `StockIssueDialog`, `StockReceiveDialog`) suffered from translucent backdrops, causing background catalog tables to bleed through and clash with input fields.
- **Resolution:** Replaced translucent brushes with solid, high-contrast modal surfaces (`Theme.Surface.Modal`: `#151E2E` Dark, `#FFFFFF` Light), reinforced with crisp borders and deep shadow elevation (`DropShadowEffect BlurRadius="35" Opacity="0.65"`).

### 2. Missing Static Resources
- **Issue:** Runtime `XamlParseException` on startup due to missing `OutlineButton`, `DangerButton`, and `Icon.Cart` resources.
- **Resolution:**
  - Added button styles and typography headers in [`Resources/Styles.xaml`](file:///d:/IMS-V1/Resources/Styles.xaml).
  - Added vector path geometry for `Icon.Cart` in [`Resources/Icons.xaml`](file:///d:/IMS-V1/Resources/Icons.xaml#L71).

### 3. Theme Engine Synchronization
- Complete light/dark theme persistence stored in local user configuration.
- Comprehensive styling for `DataGrid`, `ComboBox`, `ListBox`, `TextBox`, and action buttons to ensure readability across all screens.

---

## 5. Automated Test Suite Verification

Full test suite executed and validated with `dotnet test`:

```powershell
dotnet test STOMS-SBV1-BOS-IME.Tests\STOMS-SBV1-BOS-IME.Tests.csproj
```

```text
Passed!  - Failed: 0, Passed: 32, Skipped: 0, Total: 32, Duration: 509 ms
```

### Complete Test Matrix (32/32 Passing)
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

---

## 6. How to Build & Run

```bash
# 1. Switch to Hardik's feature branch
git checkout hardik/step-2-product-stock-management

# 2. Restore NuGet dependencies
dotnet restore

# 3. Execute all automated tests
dotnet test STOMS-SBV1-BOS-IME.Tests/STOMS-SBV1-BOS-IME.Tests.csproj

# 4. Launch the application
dotnet run --project STOMS-SBV1-BOS-IME.csproj
```

---

## 7. Version Control & Release Metadata

- **Author / Engineer:** Hardik Jariwala
- **Branch:** `hardik/step-2-product-stock-management`
- **Upstream:** `origin/hardik/step-2-product-stock-management`
- **Release Tag:** `v0.3.0-smart-visual-inventory-pos`
- **Clean Tree Guarantee:** Zero untracked or uncommitted working files.

