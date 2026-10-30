# Engineering Changelog — Step 2: Product & Stock Management Engine
**Engineer:** Hardik Jariwala  
**Role:** Senior C#/.NET 8 WPF Developer, Inventory Management Architect, Database Engineer  
**Branch:** `hardik/step-2-product-stock-management`  
**Milestone:** Step 2 — Production Product Master, Stock Inward/Outward, FEFO Batches, Serial Numbers, and Approval Ledger  

---

### 1. Overview of Step 2 Implementation
Step 2 delivers a complete, production-grade **Product & Stock Management** module for the **STOMS Industrial Stock & Warehouse Management System**. Built on .NET 8 WPF MVVM architecture with an auditable central ledger pattern, the system integrates seamlessly with the existing Step 1 Home Dashboard, theme switcher, and warehouse topology without breaking existing functionality.

---

### 2. Files Created and Modified

| Directory / File | Type | Description |
| :--- | :--- | :--- |
| `Models/ProductTrackingType.cs` | Created | Enum specifying tracking granularity: `QuantityOnly`, `BatchTracked`, `SerialTracked`, `BatchAndSerial`. |
| `Models/Product.cs` | Created | Full product master entity with auto-SKU, optional barcode, category, unit, safety stock, reorder levels, default bay locations, and computed status rollups. |
| `Models/StockBatch.cs` | Created | Batch/lot entity with received/remaining quantities, manufacturing/expiry dates, days remaining, and FEFO sorting keys. |
| `Models/StockSerialNumber.cs` | Created | Individual item serial entity tracking bay locations, status (`Available`, `Reserved`, `Issued`, `Damaged`, `Quarantined`), and issue transaction IDs. |
| `Models/StockLocationBalance.cs` | Created | Granular balance ledger breakdown across warehouse, rack, and tier section. |
| `Models/StockApprovalRequest.cs` | Created | Dual-authorization workflow entity for supervisor sign-offs on high-value issues or reservations. |
| `Models/StockOperationDtos.cs` | Created | Clean DTOs for `StockReceiveDto`, `StockIssueDto`, `StockReceiveResult`, `StockIssueResult`, and `ProductFilterCriteria`. |
| `Services/IInventoryService.cs` | Created | Central inventory interface defining contracts for SKU generation, barcode uniqueness, product CRUD, stock receive, stock issue, FEFO batches, serial registry, and approvals. |
| `Services/MockInventoryService.cs` | Created | Thread-safe in-memory master repository seeded with realistic industrial items, negative-inventory guards, FEFO prioritization, and atomic transaction records. |
| `Services/ApiInventoryService.cs` | Created | Production HTTP REST client prepared for the central ASP.NET Core Web API endpoints. |
| `ViewModels/ProductManagementViewModel.cs` | Created | Master ViewModel managing the Catalog table, Location Balances table, Batches explorer, Serial numbers registry, Approvals table, and modal workflows. |
| `ViewModels/ProductFormViewModel.cs` | Created | Add/Edit modal ViewModel featuring automatic SKU generation, barcode uniqueness checks, and dynamic tracking configuration. |
| `ViewModels/StockReceiveViewModel.cs` | Created | Guided Stock In (GRN) wizard adapting dynamically to tracking modes, validating batch numbers, future expiry dates, and serial counts. |
| `ViewModels/StockIssueViewModel.cs` | Created | Guided Stock Out wizard enforcing available stock limits, FEFO batch suggestions, serial number selection, and supervisor approval routing. |
| `ViewModels/ProductDetailsViewModel.cs` | Created | Comprehensive 5-tab product inspector (Overview, Locations, Batches, Serials, Movement History). |
| `Views/ProductManagementView.xaml` & `.cs` | Created | Industrial ERP workstation view with top KPI bar, tabs ribbon, multi-filter bar, interactive tables, and frosted backdrop modal overlays. |
| `Views/ProductFormDialog.xaml` & `.cs` | Created | Clean, accessible Add/Edit modal dialog with real-time field validation. |
| `Views/StockReceiveDialog.xaml` & `.cs` | Created | Guided Goods Inward modal dialog with automated serial generation and batch tracking. |
| `Views/StockIssueDialog.xaml` & `.cs` | Created | Guided Goods Outward modal dialog with FEFO batch selection and serial multi-picker. |
| `Views/ProductDetailsView.xaml` & `.cs` | Created | Multi-tab detail modal showcasing full inventory lifecycle and transaction history. |
| `Common/AsyncRelayCommand.cs` | Modified | Added generic `AsyncRelayCommand<T>` implementation for strongly-typed command parameter handling. |
| `Converters/Converters.cs` | Modified | Added `StockSerialStatus` support for color and background converters. |
| `Resources/Icons.xaml` | Modified | Added vector path geometries for `Eye`, `Barcode`, `Tag`, and `Close`. |
| `MainWindow.xaml` | Modified | Registered `DataTemplate` mapping `ProductManagementViewModel` to `ProductManagementView`. |
| `ViewModels/MainWindowViewModel.cs` | Modified | Routed sidebar `"Products"` and `"Stock"` to `ProductManagementViewModel`, and added deep-links for quick action workflows (`Products_Add`, `Stock_Receive`, `Stock_Issue`). |
| `ViewModels/DashboardViewModel.cs` | Modified | Linked Home Dashboard quick action buttons directly to real Step 2 workflows. |
| `STOMS-SBV1-BOS-IME.Tests/ProductAndStockTests.cs` | Created | 11 comprehensive automated tests covering SKU generation, barcode checks, search/filter, stock receipt, negative inventory prevention, FEFO batching, serial duplicates, and approval workflows. |

---

### 3. Key Features Delivered

#### A. Master Product Catalog
- **Automated SKU Generator:** Generates unique category-prefixed alphanumeric codes (e.g., `SKU-BRG-ITEM-01`).
- **Optional Barcode Handling:** Fully supports products with or without barcodes; validates uniqueness when barcodes are provided.
- **Dynamic Tracking Modes:**
  1. *Quantity Only:* For bulk items and standard hardware.
  2. *Batch / Lot Tracked:* Mandates unique batch numbers and optional/enforced expiry dates.
  3. *Serial Tracked:* Enforces individual serial numbers for every received unit.
  4. *Batch + Serial Tracked:* High-precision aerospace/automotive dual tracking.
- **Multi-Field Filtering:** Live responsive search across Name, SKU, Barcode, Category, Tracking Mode, Stock Status, and Warehouse.

#### B. Goods Inward (Stock Receive — GRN)
- Guided wizard with destination selection (`Warehouse -> Rack -> Shelf Tier`).
- Adapts inputs based on product tracking configuration.
- Validates future expiration dates for perishable items.
- Auto-generates serial number series or accepts line-by-line manual entry.
- Rejects duplicate serial numbers in submission or existing registry.
- Atomically creates batch records, serial numbers, location balances, and movement ledger transactions.

#### C. Goods Outward (Stock Issue & Requisition)
- Available stock validation: Physical holdings minus Reserved/Damaged/Expired.
- **Strict Negative Inventory Guard:** Prevents issuing more than available stock.
- **FEFO (First Expired, First Out) Batch Prioritization:** Automatically recommends the batch with the nearest valid expiration date while locking expired batches.
- **Dual-Authorization Supervisor Sign-Off:** Allows routing high-value or regulated issues to supervisor approval. Places stock in a `Reserved` state, keeping physical items on the shelf while preventing double allocation.
- Approved requests atomically deduct stock; rejected requests release reservations back to available inventory.

---

### 4. Verification and Test Results

```powershell
dotnet test STOMS-SBV1-BOS-IME.Tests\STOMS-SBV1-BOS-IME.Tests.csproj
```
**Results:**
- **Total Tests:** 19
- **Passed:** 19
- **Failed:** 0
- **Skipped:** 0
- **Duration:** 250 ms
- **Compiler Warnings:** 0
- **Compiler Errors:** 0
