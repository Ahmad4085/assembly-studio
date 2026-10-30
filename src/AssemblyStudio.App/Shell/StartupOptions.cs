namespace AssemblyStudio.App.Shell;

/// <summary>
/// Command-line options. Arguments that aren't options are files to open: the .asm file association and "Open with"
/// start the application with the file's path (FR-084, TASK-101). <c>--settings &lt;path&gt;</c> uses another settings
/// file (tests, portable use). <c>--smoke-report &lt;path&gt;</c> is the startup smoke test (NFR-005): once the main
/// window has rendered, the app writes the startup time in milliseconds to the file and exits without saving the layout.
/// <c>--recovery &lt;folder&gt;</c> uses another crash-recovery folder and <c>--autosave-seconds &lt;n&gt;</c> another
/// autosave interval (tests of TASK-063). <c>--log &lt;folder&gt;</c> uses another diagnostics log folder (tests of
/// TASK-068).
/// </summary>
/// <param name="SettingsPath">Settings file to use, or <see langword="null"/> for the default.</param>
/// <param name="SmokeReportPath">Smoke-test report file, or <see langword="null"/>.</param>
/// <param name="RecoveryPath">Crash-recovery folder, or <see langword="null"/> for the default.</param>
/// <param name="AutosaveInterval">Autosave interval, or <see langword="null"/> for the default.</param>
/// <param name="LogFolder">Diagnostics log folder, or <see langword="null"/> for the default.</param>
public sealed record StartupOptions(
    string? SettingsPath, string? SmokeReportPath, string? RecoveryPath = null, TimeSpan? AutosaveInterval = null, string? LogFolder = null)
{
    private static readonly string[] OptionsWithValue = ["--settings", "--smoke-report", "--recovery", "--log", "--autosave-seconds"];

    /// <summary>The files to open, in the order given.</summary>
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>Parses the arguments. An option without its value, or an unknown option (starting with --), is ignored.</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? settings = null, smoke = null, recovery = null, log = null;
        TimeSpan? autosave = null;
        var files = new List<string>();
        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            if (OptionsWithValue.Contains(arg))
            {
                if (i + 1 >= args.Count)
                {
                    break;
                }

                string value = args[++i];
                switch (arg)
                {
                    case "--settings":
                        settings = value;
                        break;
                    case "--smoke-report":
                        smoke = value;
                        break;
                    case "--recovery":
                        recovery = value;
                        break;
                    case "--log":
                        log = value;
                        break;
                    case "--autosave-seconds" when double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds) && seconds > 0:
                        autosave = TimeSpan.FromSeconds(seconds);
                        break;
                }
            }
            else if (!arg.StartsWith("--", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(arg))
            {
                files.Add(arg);
            }
        }

        return new StartupOptions(settings, smoke, recovery, autosave, log) { Files = files };
    }
}
