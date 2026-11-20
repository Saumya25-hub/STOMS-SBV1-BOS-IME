# Engineering Changelog — Step 3: Warehouse & Rack Management Engine
**Engineer:** Hardik Jariwala  
**Role:** Senior WPF/.NET 8 Developer, Warehouse Management System Architect, Database Engineer, Professional ERP UI/UX Designer  
**Branch:** `hardik/step-2-product-stock-management`  
**Milestone:** Step 3 — Multi-Warehouse Hierarchy, Interactive 2D Floor-Plan Studio, Rack Slotting, Intelligent Storage Suggestions & Stock Relocation  
**Release Tag:** `v0.4.0-warehouse-rack-management`  
**Test Suite:** 52 Tests Passing (100% Pass Rate: 32 Step 2 + 20 Step 3)

---

### 1. Executive Overview of Step 3 Implementation

Step 3 delivers a complete, production-grade **Warehouse & Rack Management** workstation for the **STOMS Industrial Stock & Warehouse Management System**. Built on modern **.NET 8 WPF MVVM** architecture, this module elevates physical warehouse operations into an enterprise-class spatial studio with:
- Multi-warehouse facility management and deletion protection against active stock.
- Flexible, non-uniform hierarchical storage topology (`Warehouse -> Rack -> Section -> Sub-section -> Compartment -> Bin -> Products`).
- Interactive top-down 2D floor-plan canvas editor with free drag-and-drop movement, zoom/pan/fit, rotation ($90^\circ, 180^\circ, 270^\circ$), dimension adjustments, undo/redo history, and $20\text{px}$ grid snapping.
- **Strict Spatial Independence Rule:** Visually moving or rotating racks on the canvas updates layout coordinates without altering inventory balances, batches, or product-to-location mappings.
- Location Stock Inspector displaying SKU-level balances, batch numbers, serial lists, physical, available, reserved, and damaged/expired stock.
- Capacity & occupancy tracking across Quantity, Weight ($kg$), and Volume ($m^3$) metrics with live visual health indicators.
- Rule-based **Intelligent Storage Location Suggestion Engine** (`ILocationSuggestionEngine`) incorporating default bays, available capacity, SKU consolidation, and quarantine segregation.
- Stock Relocation modal supporting inter-bin, inter-section, inter-rack, and inter-warehouse stock transfers backed by atomic double-entry ledger logging (`MovementType.LocationRelocation`).
- Light Mode & Dark Mode consistency with solid opaque modal containers.

---

### 2. Files Created and Modified

| Directory / File | Type | Description |
| :--- | :--- | :--- |
| `Models/StorageLocation.cs` | Created | Unified hierarchical spatial model for Warehouse, Rack, Section, SubSection, Compartment, and Bin with dimensions, capacity limits, and recursive item enumerators. |
| `Models/StockRelocationDto.cs` | Created | DTOs for relocation requests, validation errors, and relocation outcome reporting. |
| `Models/LocationSuggestion.cs` | Created | Models for location recommendation requests, match scoring criteria, ranking, and human-readable reasoning. |
| `Models/Warehouse.cs` | Enhanced | Added `Description`, `Address`, `WarehouseType`, `TotalFloorArea`, `LayoutWidth`, `LayoutHeight`, `Notes`, `LayoutRevision`, `UpdatedAt`, `UpdatedBy`. |
| `Models/Rack.cs` | Enhanced | Added `RackType`, `RackWidth`, `RackHeight`, `RackDepth`, `NumberOfLevels`, `Rotation`, `Orientation`, `Description`, `IsActive`, `CapacityType`, and safe `Mode=OneWay` binding protection. |
| `Models/RackSection.cs` | Enhanced | Added `Description`, `IsActive`, dimensions, `LocationType`, `SubSections`, and nested `CompartmentBin` structures. |
| `Models/StockMovement.cs` | Enhanced | Added `LocationRelocation` to `MovementType` enum for tracking spatial transfers. |
| `Services/ILocationSuggestionEngine.cs` | Created | Algorithmic location recommendation engine with multi-factor scoring (default bay match, capacity fit, SKU affinity, quarantine rules). |
| `Services/IInventoryService.cs` | Enhanced | Added contracts for Warehouse CRUD, Rack CRUD, Section CRUD, layout persistence, location inventory queries, location suggestions, and stock relocations. |
| `Services/MockInventoryService.cs` | Enhanced | Thread-safe in-memory master repository implementing all Step 3 contracts, safe deletion guards, double-entry relocation logging, and rich seed data. |
| `Services/ApiInventoryService.cs` | Enhanced | Production HTTP REST client implementing Step 3 API endpoints with complete DTO mapping. |
| `ViewModels/WarehouseRackViewModel.cs` | Enhanced | Comprehensive 4-tab studio ViewModel: Floor-Plan Canvas, Facility Directory, Slotting Hierarchy Tree, Location Stock Inspector, and Modal Workflows. |
| `Views/WarehouseRackView.xaml` | Enhanced | Enterprise 4-tab XAML studio view featuring interactive floor-plan canvas, grid background, control overlay, facility cards, hierarchy tree, inventory table, and solid modal popups. |
| `Views/WarehouseRackView.xaml.cs` | Enhanced | Code-behind canvas mouse interaction handlers for rack drag-and-drop, zoom compensation, and grid snapping. |
| `STOMS-SBV1-BOS-IME.Tests/WarehouseAndRackTests.cs` | Created | 20 comprehensive unit and integration tests covering all 22 test criteria from Part 18. |

---

### 3. Detailed Architectural & Functional Highlights

#### A. Interactive 2D Floor-Plan Studio (Visual Workspace)
- **Interactive Top-Down Canvas:** Racks render on an interactive WPF Canvas with visual badges for Rack Code, Zone, Capacity, and Occupancy %.
- **Drag-and-Drop Movement:** Users can drag racks freely in Edit Mode. Coordinates are calculated with zoom compensation (`1 / Scale`) and snapped to a $20\text{px}$ grid.
- **Rack Operations:** Instant rotation ($90^\circ$ increments), dimension modifications, rack duplication, and safe deletion.
- **Undo / Redo Stack:** Layout changes push previous coordinates onto an undo stack, allowing full rollback before saving.
- **Unsaved Changes Bar:** Prominent banner informs users of pending layout changes with one-click Save and Revert/Cancel options.
- **Visual Independence:** Layout coordinates (`CanvasX`, `CanvasY`, `Rotation`) are completely decoupled from inventory data. Moving a rack never modifies stock quantities or location balances.

#### B. Flexible Non-Uniform Hierarchy Slotting
- Racks support non-uniform configurations:
  - Rack 1 can have 4 sections, each with 2 compartments and 4 bins.
  - Rack 2 can have 2 large open tiers with no bins.
- Full nested navigation: `Warehouse -> Rack -> Section -> SubSection -> Compartment -> Bin`.
- Unique hierarchical code paths generated automatically: `WH-01 / RCK-A-01 / SEC-01 / BIN-01`.

#### C. Backend Guards & Deletion Protection
- **Warehouse Deletion Guard:** Any attempt to delete a warehouse containing physical stock throws an `InvalidOperationException`.
- **Rack Deletion Guard:** Any attempt to delete a rack containing stored products or active batch records is strictly blocked.
- **Unique Code Enforcement:** Duplicate warehouse codes or duplicate rack codes within the same warehouse are strictly rejected.

#### D. Location Stock Inspector & Relocation Workflow
- Multi-warehouse, multi-rack, and section filtering.
- Displays SKU, product name, batch number, serial numbers, physical count, available count, reserved count, and health status (`Optimal`, `LowStock`, `NearExpiry`, `Expired`, `Damaged`).
- One-click **Relocate Stock** modal allows moving specific quantities (or whole batches) from source to destination location.
- Automatically logs a double-entry ledger record (`MovementType.LocationRelocation`) with Source, Destination, Quantity, Operator, and Reason.

#### E. Intelligent Storage Location Suggestion Engine
- Evaluates incoming products against available locations based on:
  1. Default location match (+50 score).
  2. Available capacity match (+30 score).
  3. SKU consolidation / slotting affinity (+20 score).
  4. Quarantine isolation: Expired or damaged lots are automatically suggested for designated quarantine racks/tiers (e.g., `SEC-07`).
- Ranks suggestions with confidence scores (e.g., $95\%$) and human-readable justification reasons, allowing full manual override by warehouse managers.

---

### 4. Automated Test Verification

All 52 automated tests in the test suite pass with 100% success rate:
- **Product & Stock Management Suite (Step 2):** 32 Tests Passed
- **Warehouse & Rack Management Suite (Step 3):** 20 Tests Passed
  - Test 01: Warehouse Master CRUD & Unique Code Validation
  - Test 02: Warehouse Deletion Protection (Active Stock Guard)
  - Test 03: Rack Master CRUD & Unique Code Validation
  - Test 04: Rack Deletion Protection (Occupied Rack Guard)
  - Test 05: Visual Floor-Plan Coordinate Persistence
  - Test 06: Spatial Independence (Moving Rack Does Not Modify Stock)
  - Test 07: Rack Dimension Adjustments
  - Test 08: Rack Rotation ($90^\circ$ increments)
  - Test 09: Rack Duplication with Geometry Cloning
  - Test 10: Non-Uniform Section Multi-Product Balance
  - Test 11: Capacity & Occupancy Tracking
  - Test 12: Location Suggestion Engine (Default Location Affinity)
  - Test 13: Location Suggestion Engine (SKU Consolidation)
  - Test 14: Location Suggestion Engine (Quarantine Segregation)
  - Test 15: Stock Relocation (Inter-Section Atomic Transfer)
  - Test 16: Stock Relocation (Over-Allocation Rejection)
  - Test 17: Stock Relocation (Double-Entry Movement Ledger Audit)
  - Test 18: Search & Multi-Filter across Spatial Topology
  - Test 19: Undo / Redo Layout State Stack
  - Test 20: Full End-to-End Inward-to-Relocation Lifecycle
