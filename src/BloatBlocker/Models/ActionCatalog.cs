using Microsoft.Win32;

namespace BloatBlocker.Models;

/// <summary>
/// v1 action list. Deliberately limited to documented Group Policy-style registry
/// values (HKLM\SOFTWARE\Policies\... and the matching HKCU tree) rather than
/// AppX/CBS package surgery — that's the part community debloat scripts get
/// flagged for breaking Windows Update on. Policy keys are the "safe" layer:
/// Microsoft-supported toggles, reversible, and unlikely to break servicing.
///
/// IMPORTANT: verify each path against the current Windows build before shipping —
/// Microsoft periodically moves these (this list reflects 24H2/25H2-era paths as
/// documented publicly as of mid-2026). Treat this file as a starting point, not
/// a final source of truth.
/// </summary>
public static class ActionCatalog
{
    public static List<DebloatAction> All => new()
    {
        new DebloatAction
        {
            Id = "copilot",
            Name = "Copilot",
            Description = "Taskbar button, sidebar and app integration",
            Category = "AI features",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot",
                    ValueName = "TurnOffWindowsCopilot",
                    DesiredValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new()
                {
                    Hive = RegistryHive.CurrentUser,
                    SubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    ValueName = "ShowCopilotButton",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        },
        new DebloatAction
        {
            Id = "recall",
            Name = "Recall",
            Description = "Screen snapshot indexing (Copilot+ PCs)",
            Category = "AI features",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
                    ValueName = "DisableAIDataAnalysis",
                    DesiredValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
                    ValueName = "AllowRecallEnablement",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        },
        new DebloatAction
        {
            Id = "telemetry",
            Name = "Telemetry",
            Description = "Diagnostic data collection level",
            Category = "Privacy",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    ValueName = "AllowTelemetry",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        },
        new DebloatAction
        {
            Id = "cortana-search",
            Name = "Cortana in search",
            Description = "Cortana / web results in Windows Search",
            Category = "Privacy",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    ValueName = "AllowCortana",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        },
        new DebloatAction
        {
            Id = "onedrive-nagging",
            Name = "OneDrive nagging",
            Description = "Backup prompts and forced startup",
            Category = "Startup and nagging",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.LocalMachine,
                    SubKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
                    ValueName = "DisableFileSyncNGSC",
                    DesiredValue = 1,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        },
        new DebloatAction
        {
            Id = "suggested-content",
            Name = "Suggested apps and tips",
            Description = "Start menu ads, lock screen tips, suggestions",
            Category = "Startup and nagging",
            Edits = new List<RegistryEdit>
            {
                new()
                {
                    Hive = RegistryHive.CurrentUser,
                    SubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                    ValueName = "SubscribedContent-338388Enabled",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                },
                new()
                {
                    Hive = RegistryHive.CurrentUser,
                    SubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                    ValueName = "SilentInstalledAppsEnabled",
                    DesiredValue = 0,
                    ValueKind = RegistryValueKind.DWord
                }
            }
        }
    };
}
