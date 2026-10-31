using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using AssemblyStudio.App.About;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Documents.Recovery;
using AssemblyStudio.App.Learn;
using AssemblyStudio.App.Logging;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Content;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm;

namespace AssemblyStudio.App;

/// <summary>
/// Application entry point and composition root (ADR-0002, ADR-0007: manual composition, no
/// service locator).
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Whether <see cref="OnStartup"/> composes and shows the application. WPF raises Startup for every
    /// <see cref="Application"/> once its dispatcher runs, including the one the WPF tests create for the theme
    /// resources; they turn this off so that no hidden instance uses the user's settings, recovery and log folders.
    /// </summary>
    internal static bool ComposeOnStartup { get; set; } = true;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!ComposeOnStartup)
        {
            return;
        }

        StartupOptions options = StartupOptions.Parse(e.Args);
        string settingsPath = options.SettingsPath ?? JsonLayoutStore.DefaultPath;

        // NFR-012: the local diagnostics log, first, so that every later failure can be written to it.
        string logFolder = options.LogFolder ?? FileLog.DefaultFolder;
        var log = new FileLog(logFolder);
        log.Write(LogLevel.Info, $"Assembly Studio {AboutViewModel.AppVersion} started.");
        DispatcherUnhandledException += (_, args) => log.Write(LogLevel.Error, "Unhandled exception on the UI thread.", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            log.Write(LogLevel.Error, "Unhandled exception.", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) => log.Write(LogLevel.Error, "Unobserved task exception.", args.Exception);

        // Settings first: an unreadable settings file is set aside before the layout and recent-files stores read it (FR-081).
        var settings = new JsonSettingsService(settingsPath);
        if (settings.InvalidFileBackup is { } invalid)
        {
            log.Write(LogLevel.Warning, $"The settings file could not be read; defaults are used. Backup: {invalid}");
        }
        var themes = new ThemeManager(Resources, settings.Current.Theme);
        themes.Apply();
        themes.Watch();
        Exit += (_, _) => themes.Dispose();
        double textScale = TextScale.ReadFactor();
        TextScale.Apply(Resources, textScale);
        double codeSize = (double)Resources["FontSize.Code"];
        double zoom = 1.0;
        EditorAppearance.Apply(Resources, settings.Current, textScale, zoom);
        settings.Changed += (_, _) =>
        {
            themes.Preference = settings.Current.Theme;
            EditorAppearance.Apply(Resources, settings.Current, textScale, zoom);
        };

        // FR-079: the bundled NASM next to the application unless Settings names another one.
        string bundledNasm = Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "nasm.exe");
        AssemblerInfo CheckAssembler(string? configured) =>
            new NasmAssembler(new NasmOptions { BundledPath = bundledNasm, ConfiguredPath = configured }).Check();

        var store = new JsonLayoutStore(settingsPath);
        var dialogs = new WpfDialogService();
        var workspace = new WorkspaceViewModel(dialogs, new JsonRecentFilesStore(settingsPath), log);
        bool smoke = options.SmokeReportPath is not null;

        // NFR-011: autosave unsaved documents; after an unexpected exit, offer what the previous instance autosaved.
        RecoveryStore? recovery = StartRecovery(options.RecoveryPath ?? RecoveryStore.DefaultRoot, log);
        IReadOnlyList<RecoveredDocument> abandoned = recovery is null || smoke ? [] : recovery.FindAbandoned();
        if (recovery is not null)
        {
            var autosave = new AutosaveService(workspace, recovery, log);
            var timer = new DispatcherTimer { Interval = options.AutosaveInterval ?? AutosaveService.DefaultInterval };
            timer.Tick += (_, _) => autosave.Tick();
            timer.Start();
            Exit += (_, _) =>
            {
                timer.Stop();
                recovery.Dispose(); // a clean exit leaves nothing to recover
            };
        }

        // FR-084: files given on the command line (the .asm association, "Open with") open at startup; a file that
        // can't be opened is reported like any other open failure.
        foreach (string file in options.Files)
        {
            workspace.OpenFile(file);
        }

        if (abandoned.Count == 0 && workspace.Documents.Count == 0)
        {
            workspace.New(); // FR-001: start with an editable document
        }

        MainWindow? window = null;
        ShellViewModel viewModel = null!;
        var aboutSources = new AboutSources(AppContext.BaseDirectory, Path.GetFullPath(settingsPath), Path.GetFullPath(logFolder));
        viewModel = new ShellViewModel(
            store,
            AboutViewModel.AppVersion,
            workspace,
            openSettings: () =>
                new SettingsWindow(new SettingsViewModel(settings, dialogs, CheckAssembler, () => viewModel.ResetLayoutCommand.Execute(null),
                    [.. System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source).Order(StringComparer.CurrentCultureIgnoreCase)]))
                    .ShowModal(window),
            openAbout: page => new AboutWindow(new AboutViewModel(aboutSources, CheckAssembler(settings.Current.AssemblerPath), Clipboard.SetText, page))
            {
                Owner = window,
            }.ShowDialog(),
            learn: new LearnViewModel(LoadContent(log), workspace));
        // FR-022: the zoom scales the editor and the debug panels' code font.
        void ApplyZoom()
        {
            zoom = viewModel.ZoomPercent / 100.0;
            EditorAppearance.Apply(Resources, settings.Current, textScale, zoom);
            EditorAppearance.ApplyCodeZoom(Resources, codeSize, zoom);
        }

        ApplyZoom();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShellViewModel.ZoomPercent))
            {
                ApplyZoom();
            }
        };
        window = new MainWindow(viewModel, persistLayout: !smoke, log);
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

    /// <summary>The built-in content (UI-015). A broken catalog is a build defect that the content tests catch; if one
    /// ships anyway, the application runs with an empty Learn sidebar and logs why.</summary>
    private static IContentCatalog LoadContent(FileLog log)
    {
        try
        {
            return EmbeddedContentCatalog.BuiltIn();
        }
        catch (ContentCatalogException ex)
        {
            log.Write(LogLevel.Error, "The built-in content could not be loaded.", ex);
            return EmptyContentCatalog.Instance;
        }
    }

    /// <summary>The recovery store, or <see langword="null"/> when the folder can't be used (the app runs without
    /// autosave rather than not at all, and the reason is logged).</summary>
    private static RecoveryStore? StartRecovery(string root, FileLog log)
    {
        try
        {
            return new RecoveryStore(root);
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            log.Write(LogLevel.Warning, $"Crash recovery is off: the recovery folder {root} can't be used.", ex);
            return null;
        }
    }
}
