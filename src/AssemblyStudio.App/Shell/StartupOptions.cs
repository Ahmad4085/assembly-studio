namespace AssemblyStudio.App.Shell;

/// <summary>
/// Command-line options. <c>--settings &lt;path&gt;</c> uses another settings file (tests, portable
/// use). <c>--smoke-report &lt;path&gt;</c> is the startup smoke test (NFR-005): once the main window
/// has rendered, the app writes the startup time in milliseconds to the file and exits without
/// saving the layout. <c>--recovery &lt;folder&gt;</c> uses another crash-recovery folder and
/// <c>--autosave-seconds &lt;n&gt;</c> another autosave interval (tests of TASK-063). <c>--log &lt;folder&gt;</c> uses another
/// diagnostics log folder (tests of TASK-068).
/// </summary>
/// <param name="SettingsPath">Settings file to use, or <see langword="null"/> for the default.</param>
/// <param name="SmokeReportPath">Smoke-test report file, or <see langword="null"/>.</param>
/// <param name="RecoveryPath">Crash-recovery folder, or <see langword="null"/> for the default.</param>
/// <param name="AutosaveInterval">Autosave interval, or <see langword="null"/> for the default.</param>
/// <param name="LogFolder">Diagnostics log folder, or <see langword="null"/> for the default.</param>
public sealed record StartupOptions(
    string? SettingsPath, string? SmokeReportPath, string? RecoveryPath = null, TimeSpan? AutosaveInterval = null, string? LogFolder = null)
{
    /// <summary>Parses the arguments; unknown arguments are ignored (file paths to open arrive with
    /// TASK-062).</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? settings = null, smoke = null, recovery = null, log = null;
        TimeSpan? autosave = null;
        for (int i = 0; i < args.Count - 1; i++)
        {
            switch (args[i])
            {
                case "--settings":
                    settings = args[++i];
                    break;
                case "--smoke-report":
                    smoke = args[++i];
                    break;
                case "--recovery":
                    recovery = args[++i];
                    break;
                case "--log":
                    log = args[++i];
                    break;
                case "--autosave-seconds" when double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds) && seconds > 0:
                    autosave = TimeSpan.FromSeconds(seconds);
                    i++;
                    break;
            }
        }

        return new StartupOptions(settings, smoke, recovery, autosave, log);
    }
}
