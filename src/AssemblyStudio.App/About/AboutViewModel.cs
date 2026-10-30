using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.About;

/// <summary>The pages of the About window.</summary>
public enum AboutPage
{
    /// <summary>Versions as copyable text (FR-076).</summary>
    Diagnostics,

    /// <summary>The installed release notes (FR-087).</summary>
    ReleaseNotes,

    /// <summary>The installed third-party notices and license texts (FR-090).</summary>
    Notices,
}

/// <summary>Where the About window finds what it shows.</summary>
/// <param name="AppFolder">The application folder (installed files: release notes, notices, bundled tools).</param>
/// <param name="SettingsPath">The settings file in use.</param>
/// <param name="LogFolder">The diagnostics log folder.</param>
public sealed record AboutSources(string AppFolder, string SettingsPath, string LogFolder)
{
    /// <summary>The release notes installed with the application (written by the release procedure).</summary>
    public const string ReleaseNotesFile = "RELEASE-NOTES.md";

    /// <summary>The third-party notices installed with the application (ADR-0009, TASK-102).</summary>
    public const string NoticesFile = "THIRD-PARTY-NOTICES.txt";
}

/// <summary>
/// About / Diagnostics (UI-018): the versions of the application, assembler, execution engine, .NET and Windows as
/// one copyable text (FR-076, AC-038), and the release notes and third-party notices read from the installed files
/// (FR-087, FR-090). Everything is local; a missing file is reported with its path rather than replaced by made-up
/// text.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject
{
    private readonly Action<string> _copy;

    /// <summary>Builds the pages.</summary>
    /// <param name="sources">Where the files are.</param>
    /// <param name="assembler">The assembler in use (from the settings).</param>
    /// <param name="copy">Puts text on the clipboard.</param>
    /// <param name="page">The page to show first.</param>
    public AboutViewModel(AboutSources sources, AssemblerInfo assembler, Action<string> copy, AboutPage page = AboutPage.Diagnostics)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(assembler);
        ArgumentNullException.ThrowIfNull(copy);
        _copy = copy;
        Diagnostics = BuildDiagnostics(sources, assembler);
        ReleaseNotes = ReadInstalled(Path.Combine(sources.AppFolder, AboutSources.ReleaseNotesFile));
        Notices = BuildNotices(sources.AppFolder);
        SelectedPage = page;
    }

    /// <summary>The application's display version, without the source revision suffix.</summary>
    public static string AppVersion { get; } = VersionOf(typeof(AboutViewModel).Assembly);

    /// <summary>The diagnostics text.</summary>
    public string Diagnostics { get; }

    /// <summary>The release notes text, or why it can't be shown.</summary>
    public string ReleaseNotes { get; }

    /// <summary>The notices and license texts, or why they can't be shown.</summary>
    public string Notices { get; }

    /// <summary>The page shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiagnosticsPage))]
    public partial AboutPage SelectedPage { get; set; }

    /// <summary>Whether the diagnostics page is shown (Copy applies to it).</summary>
    public bool IsDiagnosticsPage => SelectedPage == AboutPage.Diagnostics;

    /// <summary>Whether the last Copy succeeded (shown next to the button).</summary>
    [ObservableProperty]
    public partial bool Copied { get; private set; }

    /// <summary>Copies the diagnostics text.</summary>
    [RelayCommand]
    private void CopyDiagnostics()
    {
        try
        {
            _copy(Diagnostics);
            Copied = true;
        }
        catch (COMException)
        {
            Copied = false; // the clipboard is held by another program; the text stays selectable
        }
    }

    /// <summary>The diagnostics text (FR-076): one "name: value" line per item, in English and invariant formats so
    /// it can be pasted into a bug report as-is.</summary>
    internal static string BuildDiagnostics(AboutSources sources, AssemblerInfo assembler)
    {
        Assembly engine = typeof(Engine.I8086.Cpu8086).Assembly;
        string assemblerLine = assembler.IsUsable
            ? string.Create(CultureInfo.InvariantCulture, $"NASM {assembler.Version ?? "(version unknown)"}, {assembler.Origin}, {assembler.Path}")
            : string.Create(CultureInfo.InvariantCulture, $"not usable ({assembler.Status}, {assembler.Origin}): {assembler.Problem}");
        var text = new StringBuilder();
        void Line(string name, string value) => text.Append(name).Append(": ").AppendLine(value);
        Line("Application", $"Assembly Studio {AppVersion}");
        Line("Assembler", assemblerLine);
        Line("Execution engine", $"{engine.GetName().Name} {VersionOf(engine)} (Intel 8086, real mode, interpreter)");
        Line("DOS services", $"{typeof(Os.Dos.DosServices).Assembly.GetName().Name} {VersionOf(typeof(Os.Dos.DosServices).Assembly)}");
        Line(".NET", $"{RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
        Line("Operating system", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Line("Settings", sources.SettingsPath);
        Line("Log folder", sources.LogFolder);
        return text.ToString();
    }

    /// <summary>The notices file, followed by the license file of each bundled tool (<c>tools\&lt;name&gt;\LICENSE*</c>).</summary>
    internal static string BuildNotices(string appFolder)
    {
        var text = new StringBuilder(ReadInstalled(Path.Combine(appFolder, AboutSources.NoticesFile)));
        string tools = Path.Combine(appFolder, "tools");
        if (Directory.Exists(tools))
        {
            foreach (string license in Directory.GetDirectories(tools).Order(StringComparer.OrdinalIgnoreCase)
                         .SelectMany(d => Directory.GetFiles(d, "LICENSE*").Order(StringComparer.OrdinalIgnoreCase)))
            {
                string tool = Path.GetFileName(Path.GetDirectoryName(license)!);
                text.AppendLine().AppendLine(new string('=', 72))
                    .AppendLine(string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.AboutLicenseOf), tool, license))
                    .AppendLine(new string('=', 72))
                    .AppendLine(ReadInstalled(license));
            }
        }

        return text.ToString();
    }

    /// <summary>A file's text, or a message naming the path when it is missing or unreadable.</summary>
    internal static string ReadInstalled(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.AboutFileMissing), path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.AboutFileUnreadable), path, ex.Message);
        }
    }

    private static string VersionOf(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? assembly.GetName().Version?.ToString()
        ?? "0.0.0";
}
