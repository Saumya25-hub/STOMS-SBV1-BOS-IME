# STOMS BOS-IME — Engineering Changelog
## Module: Step 1 — Home Dashboard & Telemetry Hub
**Author:** Hardik Jariwala (*Core Engineering & Module Architecture*)  
**Branch:** `hardik/step-1-home-dashboard`  
**Date:** 2026-10-30  
**Status:** Feature Complete & Verified (8/8 Unit Tests Passing)

---

### 1. UI/UX Complete Redesign Milestone (Aaj Ka Kaam)

Today's milestone completely refines the **Home Dashboard** UI/UX into a world-class, clean, modern, and uncluttered industrial ERP workstation:

#### A. Header & Telemetry Refinements
- **Simple Page Title & Subtitle:** Clear, uncluttered title `Dashboard` with subtitle `Warehouse overview`.
- **Streamlined Global Search:** Sleek, integrated search field with quick shortcut tooltips.
- **Dynamic Theme Switcher Icon:** Accurately toggles between Moon (in Light mode) and Sun (in Dark mode) for seamless runtime switching.
- **Compact Telemetry Status Bar:** Clean 1-line connection bar replacing the previous oversized banner, preserving vertical screen real estate.

#### B. The 6 Essential Summary Metric Cards
- **Fixed Dimensions & Grid:** Engineered a 6-column balanced grid with zero text clipping (`TextTrimming="CharacterEllipsis"`).
- **Typography & Visual Hierarchy:**
  - `Total Products` (`9` active catalog SKUs)
  - `Total Stock` (`2,046` native units tracked)
  - `Low Stock` (`1` below reorder point)
  - `Near Expiry` (`1` expiring in $\le$ 30 days)
  - `Expired Stock` (`1` requiring quarantine)
  - `Pending Approvals` (`2` awaiting sign-off)
- **Subtle Pastel Badges:** Soft tinted status backgrounds (`#ECFDF5`, `#FFFBEB`, `#FEF2F2`, `#EFF6FF`) with high-contrast text in Light mode, and rich translucent darks in Dark mode.
- **Tactile States:** Hover elevation and focus border without exaggerated gradients or animations.

#### C. Interactive Warehouse Layout Canvas (Core Workspace)
- **Real 2D Floorplan Canvas:** Racks mapped to spatial `(CanvasX, CanvasY)` coordinates on a clean grid.
- **Drag-and-Drop Repositioning:** Activating **`Edit Layout`** unlocks interactive mouse dragging with automatic 20px grid snapping and boundary protection.
- **Smooth Zoom & Pan:** Viewport controls (`[-]`, `[ 100% ]`, `[+]`) with dynamic scale transformation.
- **Visual Rack Structures:** Steel frame bays with occupancy bars, tier status dots, and live SKU indicators.
- **Docked Shelf Tier Inspector:** Click any rack on the canvas to inspect tier loads, SKU details, batch numbers, and native units with zero visual clutter.

#### D. Recent Stock Movements Table
- Compact, high-readability data table with dedicated columns: `Date`, `Product & SKU`, `Type`, `Quantity`, `Location`, and `Status`.
- Clean badge styling with aligned numerical values and subtle row borders.

#### E. Restrained Alerts & Notifications Panel
- Soft accent stripes with severity ratings (`Critical`, `Warning`, `Info`) instead of loud, jarring backgrounds.
- Quick dismiss/acknowledge actions with relative timestamps (`1h ago`, `Just now`).

#### F. Unified Quick Actions Toolbar
- 4 prominent, consistently sized action buttons: **Receive Stock**, **Issue Stock**, **Transfer Stock**, and **Add Product**.

---

### 2. Automated Test Results

The test suite in [`STOMS-SBV1-BOS-IME.Tests`](file:///D:/IMS-V1/STOMS-SBV1-BOS-IME.Tests/DashboardTests.cs) was executed and verified:

```
Test run for D:\IMS-V1\STOMS-SBV1-BOS-IME.Tests\bin\Debug\net8.0-windows\STOMS-SBV1-BOS-IME.Tests.dll (.NETCoreApp,Version=v8.0)
Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 95 ms
```

All 8 tests pass with 0 warnings and 0 errors.

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
