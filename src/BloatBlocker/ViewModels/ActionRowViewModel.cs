using System.ComponentModel;
using System.Runtime.CompilerServices;
using BloatBlocker.Models;
using BloatBlocker.Services;

namespace BloatBlocker.ViewModels;

public class ActionRowViewModel : INotifyPropertyChanged
{
    private readonly DebloatAction _action;
    private readonly DebloatService _service;
    private readonly Action _onChanged;

    public ActionRowViewModel(DebloatAction action, DebloatService service, Action onChanged)
    {
        _action = action;
        _service = service;
        _onChanged = onChanged;

        ApplyCommand = new RelayCommand(_ => Apply());
        RevertCommand = new RelayCommand(_ => Revert());

        RefreshStatus();
    }

    public string Name => _action.Name;
    public string Description => _action.Description;
    public string Category => _action.Category;

    public ActionStatus Status => _action.Status;

    public string StatusText => Status switch
    {
        ActionStatus.Protected => "Protected",
        ActionStatus.Reverted => "Reverted",
        _ => "Not applied"
    };

    public RelayCommand ApplyCommand { get; }
    public RelayCommand RevertCommand { get; }

    public void RefreshStatus()
    {
        _action.Status = _service.CheckStatus(_action);
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusText));
    }

    private void Apply()
    {
        _service.Apply(_action);
        RefreshStatus();
        _onChanged();
    }

    private void Revert()
    {
        _service.Revert(_action);
        RefreshStatus();
        _onChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
