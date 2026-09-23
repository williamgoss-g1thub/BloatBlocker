using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using BloatBlocker.Models;

namespace BloatBlocker.Services;

/// <summary>
/// One row of the undo log: the original state of a single registry value,
/// captured the first time BloatBlocker ever wrote to it. "Existed = false"
/// means undo should delete the value entirely, not write a null.
/// </summary>
public class UndoLogEntry
{
    public required string ActionId { get; set; }
    public required string Hive { get; set; }
    public required string SubKeyPath { get; set; }
    public required string ValueName { get; set; }
    public bool Existed { get; set; }
    public string? OriginalValueJson { get; set; }
    public string? ValueKind { get; set; }
}

public class DebloatService
{
    private readonly string _logPath;
    private List<UndoLogEntry> _log;

    public DebloatService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BloatBlocker");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "undo-log.json");
        _log = LoadLog();
    }

    private List<UndoLogEntry> LoadLog()
    {
        if (!File.Exists(_logPath)) return new List<UndoLogEntry>();
        try
        {
            var json = File.ReadAllText(_logPath);
            return JsonSerializer.Deserialize<List<UndoLogEntry>>(json) ?? new List<UndoLogEntry>();
        }
        catch
        {
            // Corrupt log should never crash the app — start fresh rather than
            // block the user from applying/undoing anything.
            return new List<UndoLogEntry>();
        }
    }

    private void SaveLog()
    {
        var json = JsonSerializer.Serialize(_log, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_logPath, json);
    }

    private static RegistryKey OpenBaseKey(RegistryHive hive) =>
        RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

    /// <summary>
    /// Compares each edit's current registry value against its desired value.
    /// Protected = every edit matches. Reverted = at least one previously-applied
    /// edit no longer matches (drift). NotApplied = we have never applied this action.
    /// </summary>
    public ActionStatus CheckStatus(DebloatAction action)
    {
        bool everApplied = _log.Any(e => e.ActionId == action.Id);
        if (!everApplied) return ActionStatus.NotApplied;

        foreach (var edit in action.Edits)
        {
            using var baseKey = OpenBaseKey(edit.Hive);
            using var subKey = baseKey.OpenSubKey(edit.SubKeyPath);
            var current = subKey?.GetValue(edit.ValueName);

            if (current == null || !ValuesEqual(current, edit.DesiredValue))
                return ActionStatus.Reverted;
        }

        return ActionStatus.Protected;
    }

    private static bool ValuesEqual(object current, object desired) =>
        Convert.ToString(current) == Convert.ToString(desired);

    /// <summary>
    /// Writes every edit's desired value. Before the first-ever write for a given
    /// value, captures its original state into the undo log so RevertAsync can
    /// restore it later — including restoring "did not exist" by deleting the value.
    /// </summary>
    public void Apply(DebloatAction action)
    {
        foreach (var edit in action.Edits)
        {
            using var baseKey = OpenBaseKey(edit.Hive);
            using var subKey = baseKey.CreateSubKey(edit.SubKeyPath, writable: true)
                ?? throw new InvalidOperationException($"Could not open/create {edit.SubKeyPath}");

            bool alreadyLogged = _log.Any(e =>
                e.ActionId == action.Id && e.ValueName == edit.ValueName && e.SubKeyPath == edit.SubKeyPath);

            if (!alreadyLogged)
            {
                var existingValue = subKey.GetValue(edit.ValueName);
                _log.Add(new UndoLogEntry
                {
                    ActionId = action.Id,
                    Hive = edit.Hive.ToString(),
                    SubKeyPath = edit.SubKeyPath,
                    ValueName = edit.ValueName,
                    Existed = existingValue != null,
                    OriginalValueJson = existingValue != null ? JsonSerializer.Serialize(existingValue) : null,
                    ValueKind = edit.ValueKind.ToString()
                });
            }

            subKey.SetValue(edit.ValueName, edit.DesiredValue, edit.ValueKind);
        }

        SaveLog();
        action.LastAppliedUtc = DateTimeOffset.UtcNow;
        action.Status = ActionStatus.Protected;
    }

    /// <summary>
    /// Restores every edit belonging to this action back to its pre-BloatBlocker
    /// state, using the undo log. Removes the action's entries from the log once done.
    /// </summary>
    public void Revert(DebloatAction action)
    {
        var entries = _log.Where(e => e.ActionId == action.Id).ToList();

        foreach (var entry in entries)
        {
            var hive = Enum.Parse<RegistryHive>(entry.Hive);
            using var baseKey = OpenBaseKey(hive);
            using var subKey = baseKey.OpenSubKey(entry.SubKeyPath, writable: true);
            if (subKey == null) continue;

            if (entry.Existed && entry.OriginalValueJson != null && entry.ValueKind != null)
            {
                var kind = Enum.Parse<RegistryValueKind>(entry.ValueKind);
                var originalValue = JsonSerializer.Deserialize<object>(entry.OriginalValueJson);
                if (originalValue != null)
                    subKey.SetValue(entry.ValueName, originalValue, kind);
            }
            else
            {
                subKey.DeleteValue(entry.ValueName, throwOnMissingValue: false);
            }
        }

        _log.RemoveAll(e => e.ActionId == action.Id);
        SaveLog();
        action.LastAppliedUtc = null;
        action.Status = ActionStatus.NotApplied;
    }

    /// <summary>Re-applies every reverted (drifted) action currently tracked.</summary>
    public void ReapplyDrifted(IEnumerable<DebloatAction> actions)
    {
        foreach (var action in actions)
        {
            action.Status = CheckStatus(action);
            if (action.Status == ActionStatus.Reverted)
                Apply(action);
        }
    }
}
