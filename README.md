# BloatBlocker

Removes Windows 11 AI features and bloat, and — unlike the free scripts it
competes with — keeps checking whether Windows quietly turned them back on.

## What's scaffolded

- **Models/** — `DebloatAction` + `ActionCatalog`: v1 list is Copilot, Recall,
  Telemetry, Cortana in search, OneDrive nagging, suggested content. Each is a
  set of `RegistryEdit`s against documented `HKLM\SOFTWARE\Policies\...` keys —
  deliberately not AppX/CBS package removal, which is the layer that breaks
  Windows Update in the free debloat scripts.
- **Services/DebloatService.cs** — apply / check-status / revert, backed by a
  JSON undo log (`%ProgramData%\BloatBlocker\undo-log.json`) that captures each
  value's exact original state — including "it didn't exist" — so revert is
  precise, not a guess.
- **Services/DriftWatcher.cs** — re-checks status on a timer while the app is
  running, and via a manual `CheckNow()` from the tray menu. Fires
  `DriftDetected` when a protected setting reverts.
- **Services/TrayIconService.cs** — tray icon with Open / Check now / Exit.
  Closing the window minimizes to tray instead of quitting (same pattern as
  SystemPulse); Exit is only reachable from the tray menu.
- **App.xaml.cs** — two entry paths: normal launch shows the dashboard + tray
  icon; launching with `--check` runs a headless status check, shows a balloon
  if anything reverted, and exits without ever creating a window. That's the
  mode the scheduled task calls.
- **ViewModels/ + MainWindow.xaml + Themes/Theme.xaml** — full MVVM dashboard:
  metric cards, a drift banner, a status-badged feature list. Same visual
  language as the dashboard mockup shown in chat.
- **installer/BloatBlocker.iss + register-task.ps1** — Inno Setup script that
  installs to Program Files and registers the "BloatBlocker Drift Check"
  scheduled task (logon + daily, elevated) via `Register-ScheduledTask` in
  PowerShell — not `schtasks.exe`, which mangled the quoted exe path on
  SystemPulse's installer and silently produced a task that never launched
  anything. Same fix applied here from the start.

- **Assets/icon.ico + tools/verify-registry-paths.ps1** — a flat shield +
  checkmark app icon (matches the dashboard's accent color), wired into the
  exe, the tray icon, and the drift balloon. The verify script is read-only:
  run it (`powershell -File tools\verify-registry-paths.ps1`) on a real,
  current Windows install to see which catalog paths/values exist before you
  trust `ActionCatalog.cs` against that build — it never writes anything.

## Not yet built

- **Post-update-specific trigger.** The scheduled task currently runs at logon
  + daily, which is a reasonable v1 cadence, but doesn't fire *specifically*
  right after a Windows Update finishes. A tighter version would watch the
  Windows Update event log (or the "UsoSvc" service completing) and trigger a
  check then. Worth adding once the v1 cadence proves the concept.
- **Code signing** — unsigned installer will trigger SmartScreen warnings;
  plan for a code-signing cert before public release.

## Before you ship

Registry paths in `ActionCatalog.cs` reflect what's publicly documented as of
mid-2026 (24H2/25H2-era builds). Microsoft moves these periodically —
**verify each path on a real, current Windows build before release**, ideally
with a VM snapshot so you can test apply → revert → verify cleanly.

## Building and running

Requires the .NET 8 SDK and Windows (WPF doesn't build/run outside Windows —
this was authored in a Linux sandbox but can't be compiled or tested there).

```
git clone <repo>
cd BloatBlocker
dotnet build
dotnet run --project src/BloatBlocker
```

The app requests admin elevation on launch (see `app.manifest`) — most
debloat policy keys live under `HKLM`, which needs elevation to write.

## Building the installer

```
dotnet publish src/BloatBlocker -c Release -r win-x64 --self-contained false -o publish
```

Then open `installer/BloatBlocker.iss` in Inno Setup and compile. The output
is `BloatBlocker-Setup.exe`, which installs the app and registers the
background drift-check task automatically.
