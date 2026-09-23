using Microsoft.Win32;

namespace BloatBlocker.Models;

public enum ActionStatus
{
    /// Desired values are all in place.
    Protected,
    /// At least one value was reverted (drift detected) since last apply.
    Reverted,
    /// Never applied by BloatBlocker.
    NotApplied
}

/// <summary>
/// One registry policy write that makes up part of a debloat action.
/// Hive/SubKey/ValueName identify the setting; DesiredValue is what BloatBlocker
/// wants it set to. OriginalValue is captured the first time we touch the key so
/// undo can restore it exactly (including "the value did not exist before").
/// </summary>
public class RegistryEdit
{
    public required RegistryHive Hive { get; init; }
    public required string SubKeyPath { get; init; }
    public required string ValueName { get; init; }
    public required object DesiredValue { get; init; }
    public required RegistryValueKind ValueKind { get; init; }

    /// Null means "the value did not exist" — undo should delete it, not set null.
    public object? OriginalValue { get; set; }
    public bool OriginalValueCaptured { get; set; }
}

/// <summary>
/// A named, user-facing debloat action (e.g. "Copilot"). Bundles the registry
/// edits needed to disable a feature, plus the metadata the dashboard displays.
/// </summary>
public class DebloatAction
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required List<RegistryEdit> Edits { get; init; }

    /// Populated at runtime by DebloatService — not persisted on this object.
    public ActionStatus Status { get; set; } = ActionStatus.NotApplied;
    public DateTimeOffset? LastAppliedUtc { get; set; }
}
