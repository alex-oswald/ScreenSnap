# ScreenSnap

A lightweight Windows tray utility that switches between saved **display-configuration presets** —
which monitors are enabled, which one is primary, and how they extend — from a taskbar menu or a
global keyboard shortcut.

Built for the everyday "desk vs. couch" flip: enable the desk monitors for work, then with one
click (or a hotkey) disable them and light up the TV for gaming.

## Features

- **Taskbar tray icon** with a right-click menu listing every preset, plus **Settings** and **Exit**.
- **Per-monitor presets** — each preset records, for every attached display, whether it's enabled,
  which one is primary, and its position/size in the virtual desktop.
- **Global hotkeys** — cycle presets without leaving your game or app:
  **Ctrl + Alt + `+`** for the next preset, **Ctrl + Alt + `-`** for the previous one
  (main-row and numpad keys both work). The modifier chord is configurable; an optional
  **Ctrl + Alt + 1…9** jumps straight to a preset.
- **Settings window** to capture the current layout as a preset, rename/reorder/delete presets,
  tweak per-monitor options, and apply a preset live.
- **Switch notifications** — a tray balloon confirms each switch and surfaces failures
  (e.g. a monitor that's no longer attached).
- **Start with Windows** (optional) via the per-user `Run` key.
- **Minimal dependencies** — native Windows APIs are called through
  [Microsoft.Windows.CsWin32](https://github.com/microsoft/CsWin32) (source-generated P/Invoke);
  there's no third-party UI or interop framework.

## How it works

The display engine uses the Windows **Connecting and Configuring Displays (CCD)** APIs
(`QueryDisplayConfig` / `SetDisplayConfig`) to enable/disable specific displays, set the primary
monitor, and position extended displays. Monitors are identified by their stable device path so
presets keep working across reconnects and reboots.

The tray icon is a classic `Shell_NotifyIcon` on a hidden message-only window, and global hotkeys
are registered with `RegisterHotKey` on that same window — no global keyboard hook, so it coexists
cleanly with Windows' own shortcuts. (The original idea of `Win+S` was dropped because Windows
reserves it for Search.)

## Getting started

### Prerequisites

- Windows 10 version 1809 (build 17763) or later.
- [.NET SDK 10](https://dotnet.microsoft.com/download) (the app targets `net10.0-windows10.0.19041.0`).
- The [Windows App SDK 2.2 runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)
  (the unpackaged app needs the runtime installed).

### Build

```powershell
# from the repository root
dotnet build ScreenSnap.slnx -c Debug -p:Platform=x64
```

The executable is produced at
`src\ScreenSnap\bin\x64\Debug\net10.0-windows10.0.19041.0\ScreenSnap.exe`.

### Run

Launch `ScreenSnap.exe`. No window appears at startup — look for the ScreenSnap icon in the
notification area (you may need to expand the tray overflow). Right-click it to switch presets or
open **Settings**; double-click opens Settings directly.

To create your first preset: arrange your displays the way you want using Windows, open
**Settings → “Add current”**, give it a name, and repeat for each layout. Then switch between them
from the tray menu or with **Ctrl + Alt + `+`/`-`**.

### Run tests

```powershell
dotnet test tests\ScreenSnap.Core.Tests\ScreenSnap.Core.Tests.csproj -c Debug -p:Platform=x64
```

## Where data is stored

Presets and settings live under `%LOCALAPPDATA%\ScreenSnap`:

- `presets.json` — your saved display configurations.
- `settings.json` — hotkey chord, enabled toggles, and the start-with-Windows preference.

## Project layout

| Project | Purpose |
| --- | --- |
| `src/ScreenSnap.Core` | Display engine (CCD via CsWin32), preset/settings models + JSON stores, and service interfaces. No UI or packaging dependencies. |
| `src/ScreenSnap` | The WinUI 3 tray app: tray icon, global hotkeys, Settings window, autostart, app lifecycle. |
| `tests/ScreenSnap.Core.Tests` | xUnit tests for preset/settings serialization and cycling logic. |

The split is deliberate: `ScreenSnap.Core` knows nothing about WinUI or how the app is deployed.
Packaging-specific behavior sits behind interfaces (`IStorageLocations`, `IAutostartService`), so a
future **MSIX** build can swap the `%LOCALAPPDATA%` storage for `ApplicationData` and the registry
`Run` key for the `StartupTask` API without touching the core.

## Tech stack

- **WinUI 3** / Windows App SDK 2.2 (unpackaged)
- **.NET 10** (`net10.0-windows10.0.19041.0`), C#
- **Microsoft.Windows.CsWin32** for source-generated native interop
- Windows **CCD** display APIs, `Shell_NotifyIcon`, `RegisterHotKey`

## Roadmap

- MSIX packaging with `StartupTask`-based autostart.
- A custom tray menu / WinUI flyout for richer preset previews.
- Rebindable per-preset hotkeys.

## License

No license has been chosen yet.
