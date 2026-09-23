# BloatBlocker

A Windows desktop app that detects when Microsoft silently re-enables removed AI/bloat features (like Copilot and Recall) after Windows Updates, and automatically re-applies your debloat settings — so your tweaks actually stick.

## Features

- Detects when Microsoft quietly restores bloat features after a Windows Update
- Automatically re-applies your chosen debloat settings
- Clean dashboard UI showing current system status
- Safe undo — revert any change with one click
- Runs quietly in the background with a scheduled check

## Tech Stack

- C# / WPF
- .NET 8

## Getting Started

### Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Build and run from source

```bash
git clone https://github.com/williamgoss-g1thub/BloatBlocker.git
cd BloatBlocker
dotnet build
dotnet run --project src/BloatBlocker
```

The app requests admin permissions on launch, since most of the settings it manages require elevated access to change.

### Installer

A packaged Windows installer is also available, which installs the app and sets up the background check automatically.

## License

All rights reserved.
