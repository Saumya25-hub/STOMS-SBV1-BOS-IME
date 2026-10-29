# STOMS BOS-IME — Engineering Changelog
## Module: Step 1 — Home Dashboard & Telemetry Hub
**Author:** Hardik Jariwala (*Core Engineering & Module Architecture*)  
**Branch:** `hardik/step-1-home-dashboard`  
**Date:** 2026-10-29  
**Status:** Feature Complete & Verified (8/8 Unit Tests Passing)

---

### 1. Summary of Work Delivered Today (Aaj Ka Kaam)

Today's milestone establishes the entire architectural foundation and the user experience for the **STOMS Business Operating System — Inventory Management Engine (BOS-IME)**.

#### A. Architecture & Foundation
1. **MVVM Foundation (`Common/`):**
   - Engineered lightweight `ObservableObject`, `RelayCommand`, and `AsyncRelayCommand` eliminating unnecessary 3rd party bloat.
2. **Centralized Theme Engine (`Resources/` & `Services/ThemeService.cs`):**
   - Implemented dynamic runtime switching between **Industrial Dark Mode** (slate/charcoal) and **High-Contrast Light Mode** without restarting the application.
   - Built a comprehensive resolution-independent vector path icon repository (`Icons.xaml`).
3. **Shell & Navigation (`MainWindow.xaml` & `ViewModels/MainWindowViewModel.cs`):**
   - Created a responsive master shell with a **collapsible left sidebar** (240px $\leftrightarrow$ 64px icon-only mode).
   - Designed top header with breadcrumbs, real-time SignalR connection pill, user identity (`Saumya Chaudhari — System Administrator`), and global search.
   - Built **honest placeholder pages** for future modules (`Product Management`, `Stock Movements`, `Approvals`, `Reports`, `Users`, `Settings`) showing upcoming architectural deliverables rather than fake functionality.

#### B. Dashboard Operational Capabilities
1. **6 Key Inventory Summary Cards:**
   - **Total Products:** Counts distinct active SKU records.
   - **Total Stock Quantity:** Unit-aware aggregate with click-to-view breakdown (Pcs, Drums, Bars, Kits) preventing erroneous unit combining.
   - **Low Stock Items:** Triggered by configured reorder thresholds.
   - **Near Expiry:** Batches expiring within the 30-day window.
   - **Expired Stock:** Batches past their expiry date requiring quarantine.
   - **Pending Approvals:** Inter-warehouse transfers and adjustments awaiting sign-off.
2. **Warehouse Rack Topology & Bin Visualizer:**
   - Visualized vertical storage structures with tier levels, capacity bars, and occupancy percentages.
   - Implemented strict occupancy rules: displays exact percentage when capacity is configured; displays `"Capacity not configured"` when unconfigured.
   - Interactive Shelf Tier Inspector: clicking any tier displays assigned SKUs, batch numbers, category, native units, expiry dates, and quarantine tags. Empty tiers display an explicit *"Available for allocation"* state.
   - Live filtering by warehouse facility, SKU keyword, and status.
3. **Recent Stock Movements Ledger:**
   - Auditable transaction ledger showing Transaction ID, Timestamp, Product Name, SKU, Type badge (`Stock In`, `Stock Out`, `Transfer`, `Damage`, `Adjustment`), Signed Quantity, Source/Destination, Initiator, and Status.
4. **Active Alerts Panel:**
   - Exception handling panel with severity ratings (`Info`, `Warning`, `Critical`) and individual dismiss/acknowledge actions.
5. **Non-Destructive Quick Actions:**
   - Action buttons for Inward, Outward, Transfer, Add SKU, Topology, and Approvals that guide users through validated workflows without direct, dangerous stock mutations.

---

### 2. Automated Test Results

The test suite in [`STOMS-SBV1-BOS-IME.Tests`](file:///D:/IMS-V1/STOMS-SBV1-BOS-IME.Tests/DashboardTests.cs) was executed and verified:

```
Test run for D:\IMS-V1\STOMS-SBV1-BOS-IME.Tests\bin\Debug\net8.0-windows\STOMS-SBV1-BOS-IME.Tests.dll (.NETCoreApp,Version=v8.0)
Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 74 ms
```

All 8 tests passed covering summary math, rack occupancy, status bubbling, search filters, and routing logic.

---

### 3. Step 2 Roadmap & Next Actions (Next Kya Karna He)

1. **Step 2 — Product & Master SKU Management:**
   - Centralized SKU master catalog with category & brand classifications.
   - Dynamic reorder point (ROP) & safety stock formulas.
   - EAN-13, QR Code & custom thermal barcode generator.
2. **Step 2 — Stock Ledger & Central SQLite API:**
   - Connect `ApiDashboardService` to the central ASP.NET Core Web API.
   - Implement Goods Receipt Note (GRN) inward workflow with supplier PO verification.
   - Wire SignalR hub for live multi-computer workstation updates.
