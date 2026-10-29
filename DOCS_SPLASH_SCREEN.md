# STOMS Business Operating System (BOS) — Inventory Management Engine (IME)
## Premium Animated Splash Screen & Enterprise Login Integration

### 1. System Overview
This deliverable implements a production-grade, premium animated splash screen and enterprise login architecture for **IMS (Inventor Management System)** in the **STOMS-SBV1-BOS-IME** ecosystem (.NET 8 WPF).

---

### 2. Visual Theme & Palette
- **Primary Accent:** Professional Electric Blue (`#0066FF`)
- **Secondary Accent:** Pale Sky Blue (`#38BDF8` / `#00D2FF`)
- **Deep Enterprise Navy:** `#0F172A` / `#0A1E4A` (for text and high-contrast structural framing)
- **Background:** Crisp White (`#FFFFFF`) with subtle radial ambient glow (`#140066FF`) and delicate borders (`#E2E8F0`)
- **Elevation:** Multi-layer soft ambient shadows (`DropShadowEffect` with `#0284C7` and `#0F172A`)

---

### 3. Architecture & File Structure

```
C:\IME_V1\
├── Assets\
│   └── ims_logo.png                  # High-res 512x512 transparent PNG logo asset
├── Models\
│   ├── StartupMilestone.cs           # Milestone progress model (percentage, step, status)
│   └── OperatorSession.cs            # Authenticated session token and credentials model
├── Services\
│   ├── IStartupService.cs            # Contract for real startup initialization
│   ├── StartupService.cs             # Real environment, config, local db, and stock cache engine
│   ├── IAuthenticationService.cs     # Contract for operator sign-in
│   └── AuthenticationService.cs      # Authentication provider with roles & session validation
├── ViewModels\
│   ├── ViewModelBase.cs              # INotifyPropertyChanged base
│   ├── RelayCommand.cs               # ICommand implementation with CanExecute updates
│   ├── SplashViewModel.cs            # Orchestrates startup progress, error state, and retry
│   └── LoginViewModel.cs             # Operator login state, warehouse selector, demo quick-fill
├── Views\
│   ├── SplashWindow.xaml             # WindowStyle="None", white/blue glass card, native animations
│   ├── SplashWindow.xaml.cs          # Storyboard triggers, progress gliding, transition dispatcher
│   └── MainWindow.xaml               # Split-pane enterprise IMS Login Window
├── Tests\
│   ├── STOMS-SBV1-BOS-IME.Tests.csproj # xUnit automated test project
│   ├── UnitTest1.cs                  # Milestone, failure recovery, auth & session tests
│   └── RenderPreview.cs              # Headless UI rendering test producing visual PNG artifacts
├── App.xaml / App.xaml.cs            # App lifecycle coordinator (ShutdownMode management)
├── STOMS-SBV1-BOS-IME.csproj         # Main .NET 8 WPF project definition
├── STOMS-SBV1-BOS-IME.sln            # Visual Studio Solution
└── STOMS-SBV1-BOS-IME.slnx           # Modern .NET XML Solution
```

---

### 4. Animation Sequence Specification

1. **PHASE A — Initial Reveal (0.0s – 0.4s)**
   - Window container fades in smoothly (`Opacity` 0.0 -> 1.0).
   - Radial ambient electric blue glow renders beneath the logo.
2. **PHASE B — Logo Emphasis (0.15s – 0.9s)**
   - Scale animation from 91% to 100% with `CubicEase` out.
   - Gentle translate transform (`Y` 10 -> 0).
   - Soft blue halo glow pulses to 50% opacity.
3. **PHASE C — Brand Reveal (0.45s – 1.1s)**
   - Staggered reveal of `"IMS"` title (`Y` 8 -> 0).
   - Subtitle `"INVENTOR MANAGEMENT SYSTEM"` fades in with tracking.
   - Tagline `"Smart Inventory. Seamless Management."` glides into place.
4. **PHASE D — Real Startup Loading (1.0s – Finish)**
   - Slim electric blue progress bar reveals.
   - Status messages update dynamically with genuine subsystem progress.
5. **TRANSITION — Hand-off to Login Page**
   - On completion, exit storyboard plays (`Opacity` 1.0 -> 0.0, Scale 1.0 -> 0.97 over 350ms).
   - `App.xaml.cs` switches `ShutdownMode` to `OnMainWindowClose`, shows `MainWindow`, and disposes `SplashWindow`.

---

### 5. Genuine Milestones Executed

No fake timers or arbitrary progress increments are used. Each milestone performs real operations:
- **15% (Environment & Architecture):** Verifies OS runtime, scaffolds local folders (`%LOCALAPPDATA%\STOMS_BOS_IME\Data`, `Logs`).
- **35% (Configuration Engine):** Loads/generates `appsettings.json` workspace policy file.
- **60% (Relational Data Safety):** Verifies local relational offline database ledger (`inventory.local.dat`).
- **85% (Stock Engine & SKU):** Prepares in-memory SKU indices and warehouse stock cache.
- **100% (Security & Session):** Initializes authentication provider, sets workspace ready.

---

### 6. Failure Recovery Mechanism

If a required startup dependency fails (such as database locking failure):
- The progress bar stops.
- An alert container is displayed with an explanation.
- User can click **[Retry]** to re-attempt initialization safely without duplicate service instantiation.
- User can click **[Exit]** to terminate cleanly without orphaned threads.

---

### 7. Test Verification Results

All 6 automated unit & lifecycle tests pass cleanly (`dotnet test`):
- `Test_LogoAsset_ExistsAndHasValidProperties` -> **PASSED**
- `Test_StartupService_ReportsGenuineMilestones` -> **PASSED**
- `Test_StartupService_FailureHandling_ReturnsFalseAndSetsError` -> **PASSED**
- `Test_SplashViewModel_FailureAndRetryRecovery` -> **PASSED**
- `Test_AuthenticationService_ValidAndInvalidCredentials` -> **PASSED**
- `Test_LoginViewModel_Execution` -> **PASSED**
- Visual headless rendering tests (`Render_SplashWindow_ToPng`, `Render_LoginWindow_ToPng`) -> **PASSED**

