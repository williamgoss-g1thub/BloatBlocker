using System.Windows.Threading;
using BloatBlocker.Models;

namespace BloatBlocker.Services;

/// <summary>
/// Periodically re-checks every action's status and raises DriftDetected when a
/// previously-protected action reverts. Runs on a DispatcherTimer while the app
/// is open (dashboard stays live), and is also invoked in "--check" CLI mode by
/// a scheduled task registered for logon + daily, same pattern as SystemPulse's
/// auto-startup task. Keeping both paths call the same CheckAll() avoids drift
/// between "what the tray icon shows" and "what the scheduled check reports".
/// </summary>
public class DriftWatcher
{
    private readonly DebloatService _service;
    private readonly DispatcherTimer _timer;
    private readonly List<DebloatAction> _actions;

    public event EventHandler<List<DebloatAction>>? DriftDetected;

    /// <summary>
    /// IMPORTANT: pass the same DebloatAction instances the dashboard's rows are
    /// bound to (not a fresh ActionCatalog.All()) — ActionCatalog.All builds new
    /// objects on every call, so checking a different set here would update
    /// .Status on objects the UI never sees.
    /// </summary>
    public DriftWatcher(DebloatService service, List<DebloatAction> actions, TimeSpan? interval = null)
    {
        _service = service;
        _actions = actions;
        _timer = new DispatcherTimer
        {
            Interval = interval ?? TimeSpan.FromHours(6)
        };
        _timer.Tick += (_, _) => CheckAll(_actions);
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    /// <summary>Manual re-check, e.g. from the tray menu's "Check now".</summary>
    public List<DebloatAction> CheckNow() => CheckAll(_actions);

    /// <summary>
    /// Refreshes Status on every action and returns the ones that drifted back
    /// (were Protected, are now Reverted). Fires DriftDetected if any did.
    /// </summary>
    public List<DebloatAction> CheckAll(List<DebloatAction> actions)
    {
        var drifted = new List<DebloatAction>();

        foreach (var action in actions)
        {
            var previous = action.Status;
            action.Status = _service.CheckStatus(action);

            if (previous == ActionStatus.Protected && action.Status == ActionStatus.Reverted)
                drifted.Add(action);
        }

        if (drifted.Count > 0)
            DriftDetected?.Invoke(this, drifted);

        return drifted;
    }
}
