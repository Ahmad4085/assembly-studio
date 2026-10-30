using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Documents.Recovery;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm;

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
        string settingsPath = options.SettingsPath ?? JsonLayoutStore.DefaultPath;

        // Settings first: an unreadable settings file is set aside before the layout and recent-files stores read it (FR-081).
        var settings = new JsonSettingsService(settingsPath);
        var themes = new ThemeManager(Resources, settings.Current.Theme);
        themes.Apply();
        themes.Watch();
        Exit += (_, _) => themes.Dispose();
        double textScale = TextScale.ReadFactor();
        TextScale.Apply(Resources, textScale);
        EditorAppearance.Apply(Resources, settings.Current, textScale);
        settings.Changed += (_, _) =>
        {
            themes.Preference = settings.Current.Theme;
            EditorAppearance.Apply(Resources, settings.Current, textScale);
        };

        // FR-079: the bundled NASM next to the application unless Settings names another one.
        string bundledNasm = Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "nasm.exe");
        AssemblerInfo CheckAssembler(string? configured) =>
            new NasmAssembler(new NasmOptions { BundledPath = bundledNasm, ConfiguredPath = configured }).Check();

        var store = new JsonLayoutStore(settingsPath);
        var dialogs = new WpfDialogService();
        var workspace = new WorkspaceViewModel(dialogs, new JsonRecentFilesStore(settingsPath));
        bool smoke = options.SmokeReportPath is not null;

        // NFR-011: autosave unsaved documents; after an unexpected exit, offer what the previous instance autosaved.
        RecoveryStore? recovery = StartRecovery(options.RecoveryPath ?? RecoveryStore.DefaultRoot);
        IReadOnlyList<RecoveredDocument> abandoned = recovery is null || smoke ? [] : recovery.FindAbandoned();
        if (recovery is not null)
        {
            var autosave = new AutosaveService(workspace, recovery);
            var timer = new DispatcherTimer { Interval = options.AutosaveInterval ?? AutosaveService.DefaultInterval };
            timer.Tick += (_, _) => autosave.Tick();
            timer.Start();
            Exit += (_, _) =>
            {
                timer.Stop();
                recovery.Dispose(); // a clean exit leaves nothing to recover
            };
        }

        if (abandoned.Count == 0)
        {
            workspace.New(); // FR-001: start with an editable document
        }

        MainWindow? window = null;
        ShellViewModel viewModel = null!;
        viewModel = new ShellViewModel(store, AppVersion(), workspace, () =>
            new SettingsWindow(new SettingsViewModel(settings, dialogs, CheckAssembler, () => viewModel.ResetLayoutCommand.Execute(null),
                [.. System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source).Order(StringComparer.CurrentCultureIgnoreCase)]))
                .ShowModal(window));
        window = new MainWindow(viewModel, persistLayout: !smoke);
        MainWindow = window;
        if (settings.InvalidFileBackup is { } backup && !smoke)
        {
            window.ContentRendered += (_, _) => dialogs.ShowError(Strings.SettingsInvalidTitle,
                string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.SettingsInvalidText), backup), Strings.SettingsInvalidNext);
        }
        if (abandoned.Count > 0)
        {
            window.ContentRendered += (_, _) => OfferRecovery(dialogs, workspace, abandoned);
        }

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

    /// <summary>The UI-021 prompt, once the main window is up: Restore opens the documents unsaved; Discard opens an
    /// empty document. Either way the abandoned session folders are deleted.</summary>
    private static void OfferRecovery(WpfDialogService dialogs, WorkspaceViewModel workspace, IReadOnlyList<RecoveredDocument> abandoned)
    {
        if (dialogs.AskRestore(abandoned))
        {
            foreach (RecoveredDocument document in abandoned)
            {
                workspace.Restore(document);
            }
        }
        else
        {
            workspace.New();
        }

        RecoveryStore.DeleteSessions(abandoned);
    }

    /// <summary>The recovery store, or <see langword="null"/> when the folder can't be used (the app runs without
    /// autosave rather than not at all; logging arrives with TASK-068).</summary>
    private static RecoveryStore? StartRecovery(string root)
    {
        try
        {
            return new RecoveryStore(root);
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            return null;
        }
    }

    private static string AppVersion() =>
        typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}
