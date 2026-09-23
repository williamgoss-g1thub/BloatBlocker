; BloatBlocker installer (Inno Setup)
; Mirrors SystemPulse's setup shape: install to Program Files, register a
; scheduled task via PowerShell for the background check, offer a desktop
; shortcut and "launch on finish" checkbox.

#define MyAppName "BloatBlocker"
#define MyAppVersion "0.1.0"
#define MyAppExeName "BloatBlocker.exe"

[Setup]
AppId={{B7C1A9E2-4F3D-4C1A-9E2B-BLOATBLOCKER}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=BloatBlockerInstaller
Compression=lzma
SolidCompression=yes
; Most debloat edits are HKLM policy keys - installer and app both need admin.
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
; Publish output goes here before compiling the installer:
;   dotnet publish -c Release -r win-x64 --self-contained false -o publish
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "register-task.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon



[Run]
; -ExecutionPolicy Bypass scopes to this one process call only, not a system-wide policy change.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\register-task.ps1"" -InstallDir ""{app}"""; \
    Flags: runhidden waituntilterminated; StatusMsg: "Registering background drift check..."
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Unregister-ScheduledTask -TaskName 'BloatBlocker Drift Check' -Confirm:$false -ErrorAction SilentlyContinue"""; \
    Flags: runhidden waituntilterminated
