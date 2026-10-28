namespace AssemblyStudio.App.Shell;

/// <summary>
/// Command-line options. <c>--settings &lt;path&gt;</c> uses another settings file (tests, portable
/// use). <c>--smoke-report &lt;path&gt;</c> is the startup smoke test (NFR-005): once the main window
/// has rendered, the app writes the startup time in milliseconds to the file and exits without
/// saving the layout.
/// </summary>
/// <param name="SettingsPath">Settings file to use, or <see langword="null"/> for the default.</param>
/// <param name="SmokeReportPath">Smoke-test report file, or <see langword="null"/>.</param>
public sealed record StartupOptions(string? SettingsPath, string? SmokeReportPath)
{
    /// <summary>Parses the arguments; unknown arguments are ignored (file paths to open arrive with
    /// TASK-062).</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? settings = null, smoke = null;
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
            }
        }

        return new StartupOptions(settings, smoke);
    }
}
