using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using WpfApplication = System.Windows.Application;
using BloatBlocker.Models;
using BloatBlocker.Services;
using BloatBlocker.ViewModels;

namespace BloatBlocker;

public partial class App : WpfApplication
{
    private TrayIconService? _tray;
    private MainWindow? _mainWindow;

    private static System.Drawing.Icon LoadAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        // Falls back to a built-in icon if Assets/icon.ico wasn't published
        // alongside the exe - keeps the app running rather than crashing on
        // a missing-asset edge case.
        return File.Exists(path) ? new System.Drawing.Icon(path) : System.Drawing.SystemIcons.Shield;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--check", StringComparer.OrdinalIgnoreCase))
        {
            RunHeadlessCheck();
            return;
        }

        RunNormalStartup();
    }

    /// <summary>
    /// Invoked by the scheduled task registered at logon / daily / after updates
    /// (see the installer script). Checks status, shows a balloon if anything
    /// reverted, and exits - no window is ever created. Kept process-short so
    /// it doesn't show up as a lingering background app.
    /// </summary>
    private void RunHeadlessCheck()
    {
        var service = new DebloatService();
        var actions = ActionCatalog.All;
        int drifted = actions.Count(a => service.CheckStatus(a) == ActionStatus.Reverted);

        if (drifted > 0)
        {
            using var icon = new NotifyIcon
            {
                Icon = LoadAppIcon(),
                Visible = true,
                BalloonTipTitle = "BloatBlocker",
                BalloonTipText = drifted == 1
                    ? "A Windows update brought back a feature BloatBlocker had removed. Open BloatBlocker to review."
                    : $"A Windows update brought back {drifted} features BloatBlocker had removed. Open BloatBlocker to review.",
                BalloonTipIcon = ToolTipIcon.Warning
            };
            icon.ShowBalloonTip(8000);
            // Balloon needs the message pump alive briefly, then we exit.
            System.Threading.Thread.Sleep(8500);
        }

        Shutdown();
    }

    private void RunNormalStartup()
    {
        _mainWindow = new MainWindow();
        var viewModel = (MainViewModel)_mainWindow.DataContext;

        _tray = new TrayIconService(_mainWindow, LoadAppIcon());
        _tray.CheckNowRequested += (_, _) => viewModel.CheckNow();
        _tray.ExitRequested += (_, _) =>
        {
            _tray.Dispose();
            Shutdown();
        };

        viewModel.Watcher.DriftDetected += (_, drifted) => _tray.NotifyDrift(drifted.Count);

        _mainWindow.Closing += (_, args) =>
        {
            // Closing the window minimizes to tray instead of quitting - Exit
            // only happens from the tray menu, same pattern as SystemPulse.
            args.Cancel = true;
            _tray.HideWindow();
        };

        _mainWindow.Show();
    }
}
