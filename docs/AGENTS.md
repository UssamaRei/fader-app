# Fader — Project Memory (AGENTS.md)

> **⚠️ RULE FOR ALL AI ASSISTANTS**: Read this file **before** touching any code.
> Update it **after** every meaningful change. This is the single source of truth.

---

## Project

| Field | Value |
|---|---|
| **Name** | Fader |
| **Vision** | Make Windows audio feel intelligent and invisible. |
| **Goal** | Automatically lower background music when another app produces important audio, then smoothly restore it — with zero manual intervention. |
| **Target Users** | Knowledge workers, developers, content creators, gamers — anyone who listens to music while working and gets interrupted by video/voice audio. |
| **Distribution** | Windows desktop (MVP). Commercial product in future. |

---

## Current Status

| Field | Value |
|---|---|
| **Phase** | Phase 1 — Foundation & Audio Session Enumeration |
| **Build Status** | ✅ Source complete, build verification in progress |
| **SDK** | .NET 10.0.302 installed at `C:\Program Files\dotnet\sdk` |
| **IDE** | Visual Studio 2026 Insiders |

### ✅ Completed
- Solution scaffold (`Fader.sln`, 3 projects)
- All domain models (`AudioSession`, `AppRole`, `FaderSettings`)
- `ProcessHelper` utility (display name resolution with 5-level fallback)
- `AudioSessionManager` (WASAPI session enumeration via NAudio)
- DI composition root (`App.xaml.cs`)
- `MainWindow` (Mica backdrop, custom title bar, Win10 fallback)
- `DashboardPage` (session card list, status header, empty state)
- `AppSessionCard` control (icon, volume bar, playing dot, role selector)
- `DashboardViewModel` (CommunityToolkit.Mvvm, thread-safe event handling)
- Design system (`AppStyles.xaml` — tokens, cards, typography)
- Unit test scaffold (xUnit + NSubstitute)
- `docs/AGENTS.md` (this file)

### 🔄 In Progress
- Build verification + NuGet restore

### 🎯 Next Milestone
- Phase 1 complete: App launches, lists all Windows audio sessions with names and volumes
- Phase 2: Volume read/write (slider on each card)

---

## Tech Stack

### Language & Runtime
| Component | Choice | Version |
|---|---|---|
| Language | C# 13 | Latest with .NET 10 |
| Runtime | .NET 10 | 10.0.302 |
| Target | Windows 10 (19041+) / Windows 11 (22621+) primary | |

### Frameworks & Libraries
| Library | Purpose | Version |
|---|---|---|
| **WinUI 3** (Windows App SDK) | Native Win11 UI with Mica, dark mode, rounded corners | 1.6.250228002 |
| **NAudio** | WASAPI / CoreAudio COM interop for audio session enumeration | 2.2.1 |
| **CommunityToolkit.Mvvm** | `ObservableObject`, `RelayCommand`, source generators | 8.4.0 |
| **CommunityToolkit.WinUI.UI** | WinUI helper utilities | 8.1.240916 |
| **H.NotifyIcon.WinUI** | System tray icon + context menu (Phase 7) | 2.2.0 |
| **Microsoft.Extensions.DependencyInjection** | DI container | 9.0.7 |
| **Microsoft.Extensions.Logging** | Logging abstractions | 9.0.7 |
| **Serilog** | File logging (rolling daily, 7-day retention) | 8.0.0 |
| **xUnit** | Unit tests | 2.9.0 |
| **NSubstitute** | Mocking for unit tests | 5.1.0 |

### Windows APIs Used
| API | Purpose |
|---|---|
| **WASAPI** (`IAudioSessionManager2`, `IAudioSessionEnumerator`) | Enumerate audio sessions |
| **IAudioSessionControl2** | Get session state, process ID, display name |
| **ISimpleAudioVolume** | Read and set per-session volume |
| **IAudioSessionEvents** | Event-driven playback detection (Phase 3) |
| **Shell_NotifyIconW** | System tray (via H.NotifyIcon.WinUI, Phase 7) |
| **MicaController** | Mica backdrop material |
| **QueryFullProcessImageNameW** | Get executable path for restricted processes |
| **StorageFile.GetThumbnailAsync** | Extract app icons from executables |

---

## Architecture

### Folder Structure
```
fader-app/
├── Fader.sln
├── nuget.config
├── .gitignore
├── docs/
│   └── AGENTS.md                    ← THIS FILE (project memory)
└── src/
    ├── Fader/                       ← WinUI 3 application (UI layer)
    │   ├── App.xaml / App.xaml.cs   ← DI composition root, app lifecycle
    │   ├── MainWindow.xaml / .cs    ← Mica backdrop, window chrome
    │   ├── Pages/
    │   │   └── DashboardPage.xaml/.cs  ← Main screen
    │   ├── Controls/
    │   │   └── AppSessionCard.xaml/.cs ← Session card widget
    │   ├── ViewModels/
    │   │   └── DashboardViewModel.cs   ← MVVM ViewModel
    │   ├── Styles/
    │   │   └── AppStyles.xaml          ← Design system tokens + styles
    │   ├── app.manifest                ← UAC + DPI settings
    │   └── Package.appxmanifest        ← MSIX manifest (future)
    │
    ├── Fader.Core/                  ← Business logic (no UI deps)
    │   ├── Audio/
    │   │   ├── AudioSessionManager.cs  ← WASAPI enumeration + volume control
    │   │   ├── AudioMonitor.cs         ← Playback detection (Phase 3)
    │   │   ├── DuckingService.cs       ← Ducking logic (Phase 5)
    │   │   └── FadeEngine.cs           ← Smooth volume interpolation (Phase 4)
    │   ├── Models/
    │   │   ├── AudioSession.cs         ← Primary domain model
    │   │   ├── AppRole.cs              ← None | Background | Trigger
    │   │   └── FaderSettings.cs        ← All user settings with defaults
    │   ├── Services/
    │   │   └── SettingsService.cs      ← JSON save/load (Phase 6)
    │   └── Utilities/
    │       └── ProcessHelper.cs        ← Process name + icon resolution
    │
    └── Fader.Tests/                 ← xUnit unit tests
        ├── Models/AudioSessionTests.cs
        └── Utilities/ProcessHelperTests.cs
```

### Component Responsibilities

| Component | Responsibility |
|---|---|
| `AudioSessionManager` | Enumerate WASAPI sessions; read/write volume; raise SessionAdded/Removed events |
| `AudioMonitor` | Subscribe to `IAudioSessionEvents`; detect when apps start/stop playing; notify DuckingService |
| `DuckingService` | Snapshot original volumes; apply duck when Trigger fires; restore on silence |
| `FadeEngine` | Smooth volume interpolation using `PeriodicTimer`; cancellable per-session |
| `SettingsService` | Load/save `FaderSettings` to `%AppData%\Fader\settings.json` |
| `ProcessHelper` | Resolve human-friendly names from PIDs; extract executable paths |
| `DashboardViewModel` | Wrap `AudioSessionManager` events into `ObservableCollection`; thread-safe dispatch |
| `AppSessionCard` | Display one session: icon, name, volume bar, playing dot, role ComboBox |
| `TrayService` | System tray icon + context menu; minimize-to-tray behavior (Phase 7) |

### Design Patterns
| Pattern | Where Used |
|---|---|
| **MVVM** | All UI → ViewModel → Model; `CommunityToolkit.Mvvm` source generators |
| **Dependency Injection** | All services registered as singletons in `App.ConfigureServices()` |
| **Event-driven** | WASAPI session events; no polling loops |
| **Observer** | `AudioSessionManager.SessionAdded/Removed/Refreshed` events |
| **Strategy** | `FadeEngine` easing functions (future: pluggable curves) |
| **Repository** | `SettingsService` abstracts JSON persistence |

---

## Coding Standards

### SOLID Principles
- **S** — Each class has one job. `AudioSessionManager` enumerates and controls volume only; ducking logic lives in `DuckingService`.
- **O** — `AppRole` enum is extensible for future profile modes without changing existing code.
- **L** — Interfaces for all services (will be added for testing); implementations are substitutable.
- **I** — Logging uses `ILogger<T>` (thin abstraction), not a concrete logger.
- **D** — All constructor arguments are interfaces/abstractions. No `new` inside service classes.

### Naming Conventions
| Item | Convention | Example |
|---|---|---|
| Classes | PascalCase | `AudioSessionManager` |
| Interfaces | `I` + PascalCase | `IAudioMonitor` |
| Private fields | `_camelCase` | `_sessions` |
| Properties | PascalCase | `DisplayName` |
| Methods | PascalCase | `RefreshSessionsAsync` |
| Events | PascalCase | `SessionAdded` |
| Constants | PascalCase | `DefaultDuckVolume` |
| XAML element names | PascalCase + suffix | `SessionsList`, `RefreshButton` |

### Dependency Injection Rules
- All services registered in `App.ConfigureServices()` only — never instantiated with `new` elsewhere.
- Singletons for: `AudioSessionManager`, `DuckingService`, `FadeEngine`, `SettingsService`.
- `App.Services` static property used only in XAML code-behind where constructor injection isn't available.
- ViewModels receive services via constructor; resolved from `App.Services` in page constructors.

### Async Guidelines
- All I/O and COM operations are `async`/`await` — no `.Result` or `.Wait()`.
- WASAPI callbacks (from audio engine threads) are marshalled to the UI thread via `DispatcherQueue.TryEnqueue`.
- `CancellationToken` passed to every async method that touches I/O.
- Long-running refresh operations run on `Task.Run()` to avoid UI blocking.

### Error Handling
- WASAPI calls wrapped in try/catch — audio sessions can disappear mid-operation.
- `ProcessHelper` swallows `UnauthorizedAccessException` (system processes are expected to deny access).
- Log all exceptions at appropriate levels: Warning for recoverable, Error for unexpected.
- Never let an audio engine callback throw — it would crash the audio host process.

### Logging
- Serilog file sink: `%AppData%\Fader\logs\fader-YYYYMMDD.log`
- Rolling daily, retain 7 days.
- Use `ILogger<T>` everywhere — never static `Log.*`.
- Levels: Debug (verbose trace), Information (session discovered/removed), Warning (recoverable failures), Error (unexpected failures).

### Performance Requirements
- CPU usage: < 0.1% at idle (event-driven, no polling).
- RAM: < 30 MB working set.
- Fade operation: smooth, non-blocking, GPU-composited where possible.
- No audio glitches — never call WASAPI APIs from the audio render thread callback.
- NuGet restore + build: must succeed in < 2 minutes on any developer machine.

---

## Product Requirements

### What Fader Does
1. Detects all Windows audio sessions (WASAPI).
2. Lets the user assign each app as **Background** (music) or **Trigger** (video/voice/alerts).
3. When any Trigger app produces audio continuously for ≥ 800ms → fade all Background apps to 20%.
4. When all Trigger apps stop → wait 800ms → smoothly restore Background apps to their original volumes.
5. Runs in the system tray; minimal window presence.
6. Saves all settings persistently.
7. Optionally starts with Windows.

### MVP Includes
- WASAPI session enumeration
- Volume read + write per session
- Event-driven playback detection
- Smooth fade engine (configurable duration/easing)
- Automatic ducking logic (with minimum playback debounce)
- Settings UI (Duck Volume, Fade Duration, Restore Delay, Min Playback Duration)
- System tray (hide to tray, tray menu)
- Settings persistence (JSON)
- Windows startup option

### Intentionally NOT in MVP
- Browser extension / cross-tab ducking
- AI features / speech detection
- Cloud sync / accounts / analytics
- Mobile support
- Multiple profiles / Work/Gaming modes
- Any cross-platform support

### User Experience Goals
- **Invisible**: user should barely notice Fader — it just works.
- **Zero-friction**: no complex setup. Install, assign roles, done.
- **Premium feel**: smooth animations, native Win11 aesthetics, Mica backdrop.
- **Reliable**: never permanently overwrite user's preferred volume.
- **Lightweight**: not felt in Task Manager.

### Default Settings
| Setting | Default |
|---|---|
| Duck Volume | 20% (0.20) |
| Fade Duration | 350 ms |
| Restore Delay | 800 ms |
| Min Playback Duration | 800 ms |
| Launch On Startup | Enabled |
| Start Minimized | Disabled |

---

## Development Roadmap

| Phase | Goal | Status |
|---|---|---|
| **Phase 1** | Solution scaffold + WASAPI session enumeration + display in UI | 🔄 In progress |
| **Phase 2** | Read and write volume per session (sliders in UI for testing) | ⬜ Pending |
| **Phase 3** | Detect when apps start/stop playing (event-driven via `IAudioSessionEvents`) | ⬜ Pending |
| **Phase 4** | Smooth fade engine (`PeriodicTimer`, configurable easing, cancellable) | ⬜ Pending |
| **Phase 5** | Automatic ducking logic (snapshot → duck → restore, debounce) | ⬜ Pending |
| **Phase 6** | Settings UI (`SettingsPage`) + `SettingsService` JSON persistence | ⬜ Pending |
| **Phase 7** | System tray (`H.NotifyIcon.WinUI`, minimize-to-tray, context menu) | ⬜ Pending |
| **Phase 8** | App role persistence, Windows startup registry, UI polish | ⬜ Pending |

---

## Decisions

| Date | Decision | Reason |
|---|---|---|
| 2026-08-04 | Use **NAudio** for WASAPI interop instead of raw COM P/Invoke | Cleaner API surface, MIT-licensed, hides boilerplate; raw COM still used for event subscriptions in Phase 3 |
| 2026-08-04 | Use **H.NotifyIcon.WinUI** for system tray | Well-maintained NuGet, handles edge cases (multi-monitor DPI, context menu lifetime) better than raw P/Invoke |
| 2026-08-04 | Target **Windows 11 primary** (`net10.0-windows10.0.22621.0`) with Win10 fallback | Mica requires 22000+; fallback is dark background brush on Win10 |
| 2026-08-04 | Use **CommunityToolkit.Mvvm** source generators | Reduces boilerplate `INotifyPropertyChanged` code; compile-time safe |
| 2026-08-04 | Use **Serilog** with file sink for logging | Production-grade structured logging; rolling files don't fill disk |
| 2026-08-04 | Store settings in `%AppData%\Fader\settings.json` | Standard Windows per-user location; survives app updates; human-readable |
| 2026-08-04 | Use **unpackaged** (non-MSIX) mode for MVP development | Simpler iteration; MSIX packaging deferred to commercial distribution |
| 2026-08-04 | Target **.NET 10** (not .NET 8 as originally planned) | VS 2026 Insiders requires .NET 10; .NET 9 SDK installer blocked by runtime conflicts |
| 2026-08-04 | Use **event-driven** WASAPI session detection, no polling | `IAudioSessionEvents` callbacks = zero idle CPU; polling would waste battery/CPU |
| 2026-08-04 | Volume snapshot stored in `DuckingService._originalVolumes` (also persisted to disk) | Crash recovery: volumes are never permanently lost even if Fader crashes mid-duck |

---

## Known Issues

| Issue | Severity | Notes |
|---|---|---|
| `AppSessionCard` icon load falls back silently | Low | `StorageFile.GetThumbnailAsync` may fail for some system processes; shows blank icon area |
| `Package.appxmanifest` included but MSIX not configured | Low | Won't affect unpackaged builds; needed later for Store |
| No `IAudioSessionEvents` subscription yet | Medium | Phase 3 work; IsPlaying is currently only set on refresh |
| `DashboardViewModel.DispatchToUiThread` falls back to direct call | Low | Correct behavior; just worth noting for tests that don't have a dispatcher |

---

## Future Features

### Version 2
- Multiple profiles (Work Mode, Study Mode, Gaming Mode, Editing Mode)
- Per-profile app role assignments
- Quick-switch tray menu

### Version 3
- Browser extension integration
- Cross-tab ducking (YouTube tab vs Spotify)

### Version 4
- Speech detection (don't duck during phone calls)

### Version 5
- AI-powered intelligent audio management (learn user patterns)

---

## Session Notes

### Session 1 — 2026-08-04

**Completed:**
- Full Phase 1 source code written (22 files across 3 projects)
- Solution created: `Fader.sln` with `Fader` (WinUI 3), `Fader.Core` (class library), `Fader.Tests` (xUnit)
- .NET 10 SDK installed (required admin UAC — winget couldn't elevate from sandboxed terminal)
- NuGet restore initiated

**Files Created:**
- `Fader.sln`, `nuget.config`, `.gitignore`
- `src/Fader.Core/Audio/AudioSessionManager.cs`
- `src/Fader.Core/Models/AppRole.cs`, `AudioSession.cs`, `FaderSettings.cs`
- `src/Fader.Core/Utilities/ProcessHelper.cs`
- `src/Fader/App.xaml`, `App.xaml.cs`
- `src/Fader/MainWindow.xaml`, `MainWindow.xaml.cs`
- `src/Fader/Pages/DashboardPage.xaml`, `DashboardPage.xaml.cs`
- `src/Fader/Controls/AppSessionCard.xaml`, `AppSessionCard.xaml.cs`
- `src/Fader/ViewModels/DashboardViewModel.cs`
- `src/Fader/Styles/AppStyles.xaml`
- `src/Fader/app.manifest`, `Package.appxmanifest`
- `src/Fader.Tests/Models/AudioSessionTests.cs`
- `src/Fader.Tests/Utilities/ProcessHelperTests.cs`
- `docs/AGENTS.md` (this file)

**Key Decisions:**
- Switched from .NET 8 → .NET 10 due to VS 2026 Insiders environment
- NAudio for WASAPI, H.NotifyIcon.WinUI for tray, CommunityToolkit.Mvvm for MVVM

**Next:**
- Verify build succeeds (currently running `dotnet restore`)
- Fix any compile errors surfaced by the build
- Run `dotnet test` to confirm unit tests pass
- Move to Phase 2: volume read/write with interactive sliders
