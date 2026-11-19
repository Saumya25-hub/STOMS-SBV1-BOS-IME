# STOMS-SBV1-BOS-IME
### STOMS Business Operating System — Inventory Management Engine (BOS-IME)

A high-performance, enterprise-grade inventory and stock management desktop suite built on modern **.NET 8 WPF** architecture for ultra-fast warehousing, SKU lifecycle tracking, and seamless supply-chain operations.

---

## 👥 Core Engineering Team

- **Saumya Chaudhari** ([@Saumya25-hub](https://github.com/Saumya25-hub)) — *Founder & Chairman (Vision and System Architect)*
- **Jenil** — *Core Engineering & Feature Systems*
- **Hardik** — *Core Engineering & Module Architecture*

---

## ⚡ Technology Stack

| Component | Specification |
| :--- | :--- |
| **Framework** | .NET 8.0 Windows Desktop |
| **UI Subsystem** | Windows Presentation Foundation (WPF / XAML) |
| **Language** | C# 12 |
| **Architecture** | MVVM (Model-View-ViewModel) Pattern |
| **Ecosystem** | STOMS Business Operating System (BOS) Suite |

---

## 📦 Core Capabilities Implemented

- **Smart Visual Inventory & 2D Warehouse Canvas:** Interactive 2D drag-and-drop rack designer with dynamic shelf coding (`A1..A8`, `A-R1-S1`) and real-time occupancy heatmaps.
- **Multi-Level Unit Conversion Engine:** Base (`PCS`), Packaging (`BOX`), and Bulk (`CARTON`) unit hierarchy with exact decimal calculations and packaging breakdowns (`79 BOX + 14 PCS`).
- **High-Speed Barcode & EAN-13 System:** Keyboard-wedge POS scanner support, EAN-13 check digit calculator, and multi-barcode aliases per SKU.
- **Automated Goods Inward & Batching:** Automatic batch identifier generation (`{PREFIX}-{YYYY}-{AUTO_INCREMENT}`), future expiry validation, and dynamic rack/shelf mapping.
- **POS Billing & Automated FEFO Engine:** Barcode-driven point of sale with automated earliest-expiry-first (FEFO) allocation, multi-batch splitting, expired lot quarantine locks, and thread-safe deduction.
- **Dual-Authorization & Approval Ledger:** Supervisor sign-offs on high-value issues, stock reservations, and complete movement audit history.
- **Enterprise Dark & Light Themes:** Dynamic runtime theme toggle with persistent configuration, bilingual support, and solid non-transparent dialogs.

---

## 📚 Technical Documentation & Architecture Guides

Detailed engineering specifications, operational workflows, and changelogs:

- [Smart Visual Inventory Workflow & Guide](file:///d:/IMS-V1/docs/HARDIK_WORKFLOW_AND_NEW_CHANGES.md)
- [Step 2 Product & Stock Management Changelog](file:///d:/IMS-V1/docs/HARDIK_CHANGELOG_STEP2.md)
- [Step 2 Architecture Specification](file:///d:/IMS-V1/docs/STEP_2_PRODUCT_AND_STOCK_MANAGEMENT_ARCHITECTURE.md)
- [Step 1 Home Dashboard Architecture](file:///d:/IMS-V1/docs/STEP_1_HOME_DASHBOARD_ARCHITECTURE.md)
- [Step 1 Dashboard Changelog](file:///d:/IMS-V1/docs/HARDIK_CHANGELOG_STEP1.md)

---

## 🧪 Testing & Verification

The suite includes 32 automated unit and integration tests covering SKU generation, barcode uniqueness, multi-unit arithmetic, FEFO batch splitting, and overselling protection:

```bash
dotnet test STOMS-SBV1-BOS-IME.Tests/STOMS-SBV1-BOS-IME.Tests.csproj
```
*Current Status: 32 Passed, 0 Failed, 0 Skipped (100% Pass Rate).*

---

## 🛠️ Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with .NET Desktop Development workload) or VS Code / JetBrains Rider.

### Build and Run
```bash
# Clone the repository
git clone https://github.com/Saumya25-hub/STOMS-SBV1-BOS-IME.git

# Navigate to project directory
cd STOMS-SBV1-BOS-IME

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Launch the application
dotnet run --project STOMS-SBV1-BOS-IME.csproj
```

---

## 📜 Governance & Engineering Rules

This repository and all associated engineering deliverables adhere strictly to the **STOMS Developer Policy & Engineering Standards** (Code confidentiality, 7-day QA testing SLAs, and zero unauthorized IP leakage).
