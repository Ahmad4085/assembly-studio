using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App;

/// <summary>
/// Application entry point and composition root (ADR-0002, ADR-0007: manual composition, no
/// service locator).
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupOptions options = StartupOptions.Parse(e.Args);
        var themes = new ThemeManager(Resources); // the Settings override arrives with TASK-067
        themes.Apply();
        themes.Watch();
        Exit += (_, _) => themes.Dispose();
        TextScale.Apply(Resources, TextScale.ReadFactor());

        string settingsPath = options.SettingsPath ?? JsonLayoutStore.DefaultPath;
        var store = new JsonLayoutStore(settingsPath);
        var workspace = new WorkspaceViewModel(new WpfDialogService(), new JsonRecentFilesStore(settingsPath));
        workspace.New(); // FR-001: start with an editable document
        var viewModel = new ShellViewModel(store, AppVersion(), workspace);
        var window = new MainWindow(viewModel, persistLayout: options.SmokeReportPath is null);
        MainWindow = window;

        // NFR-005: cold start to an editable main window. Measured from process start to the
        // first rendered frame.
        window.ContentRendered += (_, _) =>
        {
            double startupMs = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            Trace.WriteLine($"Startup: {startupMs:F0} ms");
            if (options.SmokeReportPath is not null)
            {
                File.WriteAllText(options.SmokeReportPath, startupMs.ToString("F0", CultureInfo.InvariantCulture));
                window.Close();
            }
        };
        window.Show();
    }

    private static string AppVersion() =>
        typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}
