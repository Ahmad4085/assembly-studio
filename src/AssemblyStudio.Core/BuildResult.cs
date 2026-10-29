namespace AssemblyStudio.Core;

/// <summary>Where the assembler path came from (FR-023).</summary>
public enum AssemblerOrigin
{
    /// <summary>The assembler shipped with the application.</summary>
    Bundled,

    /// <summary>A path the user entered in Settings; it overrides the bundled one (FR-075).</summary>
    Configured,
}

/// <summary>Result of checking the assembler (FR-024, AC-015, AC-016).</summary>
public enum AssemblerStatus
{
    /// <summary>Found, runs, and reports a supported version.</summary>
    Ready,

    /// <summary>Found and runs, but its version is older than the minimum supported one (a warning; builds still run).</summary>
    OlderVersion,

    /// <summary>Found and runs, but its version could not be read (a warning; builds still run).</summary>
    UnknownVersion,

    /// <summary>No assembler file at the path.</summary>
    Missing,

    /// <summary>The configured path is not acceptable (not a full path, or not an .exe file).</summary>
    InvalidPath,

    /// <summary>The file exists but could not be run, or it failed or timed out when asked for its version.</summary>
    NotRunnable,
}

/// <summary>The assembler a build uses: its path, origin, reported version and status (FR-023, FR-024).</summary>
/// <param name="Path">Full path of the assembler executable, or <see langword="null"/> when none is configured or bundled.</param>
/// <param name="Origin">Whether the path is the bundled or the configured one.</param>
/// <param name="Version">The version the assembler reported (for example <c>3.02</c>), or <see langword="null"/>.</param>
/// <param name="Status">The check result.</param>
/// <param name="Problem">Why the assembler is unusable or why a warning applies; <see langword="null"/> when <see cref="AssemblerStatus.Ready"/>.</param>
public sealed record AssemblerInfo(string? Path, AssemblerOrigin Origin, string? Version, AssemblerStatus Status, string? Problem)
{
    /// <summary>Whether builds can run with this assembler (possibly with a version warning).</summary>
    public bool IsUsable => Status is AssemblerStatus.Ready or AssemblerStatus.OlderVersion or AssemblerStatus.UnknownVersion;
}

/// <summary>A document to build (FR-025, FR-026).</summary>
/// <param name="SourcePath">Full path of the saved file, or <see langword="null"/> for an untitled document.
/// Its folder is the assembler's working directory, so <c>%include</c> resolves as it does for the saved file.</param>
/// <param name="UnsavedText">The editor text when it differs from the file (or the whole text of an untitled
/// document); it is assembled from a temporary copy without saving. <see langword="null"/> builds the file on disk.</param>
public sealed record BuildRequest(string? SourcePath, string? UnsavedText);

/// <summary>How a build ended (FR-030).</summary>
public enum BuildOutcome
{
    /// <summary>The assembler exited with 0 and produced an image.</summary>
    Succeeded,

    /// <summary>The assembler exited with an error code; its output explains why.</summary>
    Failed,

    /// <summary>No usable assembler: no build was attempted (AC-016). See <see cref="BuildResult.Assembler"/>.</summary>
    AssemblerMissing,

    /// <summary>The assembler ran past the timeout and its process tree was killed (AC-019).</summary>
    TimedOut,

    /// <summary>The assembler process terminated abnormally (AC-019).</summary>
    Crashed,

    /// <summary>The user cancelled the build; the process tree was killed.</summary>
    Cancelled,
}

/// <summary>The result of one build (FR-030, NFR-006).</summary>
/// <param name="Outcome">How the build ended.</param>
/// <param name="Image">The flat .COM image when <see cref="BuildOutcome.Succeeded"/>, else empty.</param>
/// <param name="Output">The assembler's diagnostic output (capped, NFR-014), raw.</param>
/// <param name="OutputTruncated">Whether the output reached the cap and was cut.</param>
/// <param name="ExitCode">The assembler's exit code, or <see langword="null"/> when it was not run or was killed.</param>
/// <param name="Duration">Wall-clock build time, including the assembler check (NFR-006).</param>
/// <param name="Assembler">The assembler used, or the reason none could be used.</param>
public sealed record BuildResult(
    BuildOutcome Outcome,
    ReadOnlyMemory<byte> Image,
    string Output,
    bool OutputTruncated,
    int? ExitCode,
    TimeSpan Duration,
    AssemblerInfo Assembler);
