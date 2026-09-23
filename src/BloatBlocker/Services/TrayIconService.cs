using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace BloatBlocker.Services;

/// <summary>
/// Owns the NotifyIcon. The main window hides (not closes) to the tray on close;
/// double-clicking the tray icon or picking "Open" restores it. Exit is only
/// reachable from the tray menu, same pattern as SystemPulse.
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Window _mainWindow;

    public TrayIconService(Window mainWindow, Icon appIcon)
    {
        _mainWindow = mainWindow;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Check now", null, (_, _) => CheckNowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _icon = new NotifyIcon
        {
            Icon = appIcon,
            Text = "BloatBlocker",
            Visible = true,
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => ShowWindow();
    }

    public event EventHandler? CheckNowRequested;
    public event EventHandler? ExitRequested;

    public void ShowWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void HideWindow() => _mainWindow.Hide();

    /// <summary>Balloon tip shown when the background watcher finds drift.</summary>
    public void NotifyDrift(int driftedCount)
    {
        _icon.BalloonTipTitle = "BloatBlocker";
        _icon.BalloonTipText = driftedCount == 1
            ? "A Windows update brought back a feature BloatBlocker had removed."
            : $"A Windows update brought back {driftedCount} features BloatBlocker had removed.";
        _icon.BalloonTipIcon = ToolTipIcon.Warning;
        _icon.ShowBalloonTip(6000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
