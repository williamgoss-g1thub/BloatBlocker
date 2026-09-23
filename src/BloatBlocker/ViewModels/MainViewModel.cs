using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BloatBlocker.Models;
using BloatBlocker.Services;

namespace BloatBlocker.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DebloatService _service = new();
    private readonly List<DebloatAction> _actions = ActionCatalog.All;

    /// Exposed so App.xaml.cs can subscribe DriftDetected to the tray balloon
    /// without MainViewModel needing to know the tray icon exists.
    public DriftWatcher Watcher { get; }

    public ObservableCollection<ActionRowViewModel> Actions { get; } = new();

    public MainViewModel()
    {
        foreach (var action in _actions)
            Actions.Add(new ActionRowViewModel(action, _service, RefreshSummary));

        Watcher = new DriftWatcher(_service, _actions);
        Watcher.DriftDetected += (_, drifted) =>
        {
            foreach (var row in Actions)
                row.RefreshStatus();
            RefreshSummary();
        };
        Watcher.Start();

        ReapplyDriftedCommand = new RelayCommand(_ => ReapplyDrifted());

        RefreshSummary();
        LastCheckedUtc = DateTimeOffset.UtcNow;
    }

    public RelayCommand ReapplyDriftedCommand { get; }

    private int _protectedCount;
    public int ProtectedCount
    {
        get => _protectedCount;
        private set { _protectedCount = value; OnPropertyChanged(); }
    }

    public int TotalCount => Actions.Count;

    private int _needsAttentionCount;
    public int NeedsAttentionCount
    {
        get => _needsAttentionCount;
        private set { _needsAttentionCount = value; OnPropertyChanged(); }
    }

    private DateTimeOffset _lastCheckedUtc;
    public DateTimeOffset LastCheckedUtc
    {
        get => _lastCheckedUtc;
        private set { _lastCheckedUtc = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastCheckedDisplay)); }
    }

    /// Converts to whatever timezone the machine is actually in, so this is
    /// correct no matter where the app runs — not hardcoded to any one zone.
    public string LastCheckedDisplay => " " + LastCheckedUtc.ToLocalTime().ToString("HH:mm");

    public bool HasDrift => NeedsAttentionCount > 0;

    private void RefreshSummary()
    {
        ProtectedCount = Actions.Count(a => a.Status == ActionStatus.Protected);
        NeedsAttentionCount = Actions.Count(a => a.Status == ActionStatus.Reverted);
        LastCheckedUtc = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasDrift));
    }

    private void ReapplyDrifted()
    {
        _service.ReapplyDrifted(_actions);
        foreach (var row in Actions)
            row.RefreshStatus();
        RefreshSummary();
    }

    /// <summary>Manual re-check from the tray menu's "Check now" — refresh only, no auto-apply.</summary>
    public void CheckNow()
    {
        Watcher.CheckNow();
        foreach (var row in Actions)
            row.RefreshStatus();
        RefreshSummary();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
