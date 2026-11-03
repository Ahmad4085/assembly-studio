using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm.Diagnostics;
using AssemblyStudio.Toolchain.Nasm.SourceMapping;

namespace AssemblyStudio.Toolchain.Nasm;

/// <summary>
/// Runs NASM (ADR-0005) to build a flat 8086 .COM image (FR-026). The configured path overrides the
/// bundled one (FR-023); the version is checked before each build (cached per executable file), with a
/// warning below 3.02 (FR-024). Each build gets its own temporary directory, deleted afterwards; NASM
/// runs without a shell, with an argument list, a timeout with process-tree kill, and capped output
/// (FR-025, NFR-014). Unsaved text is assembled from a temporary copy with the source file's folder as
/// working directory (untitled documents: the temporary directory).
/// <para>
/// Arguments: <c>-f bin --before "cpu 8086" --before "[map all]" -l &lt;tmp&gt;\program.lst -Xgnu
/// --reproducible -o &lt;tmp&gt;\program.com &lt;source&gt;</c>. The map goes to standard output, because
/// NASM's map directive can't take a file name that contains spaces; diagnostics go to standard error.
/// </para>
/// </summary>
public sealed partial class NasmAssembler : IAssembler
{
    /// <summary>The oldest NASM version without a warning (FR-024, ADR-0005 §1).</summary>
    public static readonly Version MinimumVersion = new(3, 2);

    private const string UntitledFileName = "untitled.asm";
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static async Task<bool> StartsWithUtf8BomAsync(string path, CancellationToken cancellationToken)
    {
        var head = new byte[3];
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        int read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        return read == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
    }

    private readonly NasmOptions _options;
    private readonly Lock _cacheLock = new();
    private (string Path, DateTime Written, long Length, AssemblerInfo Info)? _checked;

    /// <summary>Creates the assembler integration with <paramref name="options"/>.</summary>
    public NasmAssembler(NasmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Checks a path the user enters in Settings (AC-015): it must be a full local path to an existing
    /// <c>.exe</c> file. Returns <see langword="null"/> when acceptable, else the reason. Nothing is run.
    /// </summary>
    public static string? ValidateConfiguredPath(string? path) =>
        FormatProblem(path) ?? (File.Exists(path) ? null : AssemblerProblems.Text("PathNotFound", path));

    /// <summary>Checks the form of a configured path without touching the file system (a network
    /// path is rejected before any lookup).</summary>
    private static string? FormatProblem(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return AssemblerProblems.Text("PathEmpty");
        }

        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return AssemblerProblems.Text("PathNotFull");
        }

        return string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
            ? null
            : AssemblerProblems.Text("PathNotExe");
    }

    /// <summary>The build's NASM arguments (documented in the help page "Building with NASM", TASK-093).</summary>
    internal static string[] Arguments(string listing, string image, string source) =>
    [
        "-f", "bin",
        "--before", "cpu 8086",
        "--before", "[map all]",
        "-l", listing,
        "-Xgnu",
        "--reproducible",
        "-o", image,
        source,
    ];

    /// <summary>Reads the version from <c>nasm -v</c> output, such as "NASM version 3.02 compiled on …".</summary>
    internal static Version? ParseVersion(string output)
    {
        Match match = VersionPattern().Match(output);
        if (!match.Success)
        {
            return null;
        }

        int major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int minor = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return new Version(major, minor);
    }

    /// <inheritdoc />
    public AssemblerInfo Check()
    {
        bool configured = !string.IsNullOrWhiteSpace(_options.ConfiguredPath);
        AssemblerOrigin origin = configured ? AssemblerOrigin.Configured : AssemblerOrigin.Bundled;
        string? path = configured ? _options.ConfiguredPath : _options.BundledPath;

        if (configured && ValidateConfiguredPath(path) is { } invalid)
        {
            return new AssemblerInfo(path, origin, null, FormatProblem(path) is null ? AssemblerStatus.Missing : AssemblerStatus.InvalidPath,
                AssemblerProblems.Text("ConfiguredUnusable", invalid));
        }

        if (path is null || !File.Exists(path))
        {
            return new AssemblerInfo(path, origin, null, AssemblerStatus.Missing,
                path is null
                    ? AssemblerProblems.Text("NoneAvailable")
                    : AssemblerProblems.Text("BundledMissing", path));
        }

        var file = new FileInfo(path);
        lock (_cacheLock)
        {
            if (_checked is { } cached && cached.Path == path && cached.Written == file.LastWriteTimeUtc && cached.Length == file.Length)
            {
                return cached.Info;
            }
        }

        AssemblerInfo info = QueryVersion(path, origin);
        lock (_cacheLock)
        {
            _checked = info.Status == AssemblerStatus.NotRunnable ? null : (path, file.LastWriteTimeUtc, file.Length, info);
        }

        return info;
    }

    /// <inheritdoc />
    public async Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourcePath is null && request.UnsavedText is null)
        {
            throw new ArgumentException("An untitled document must supply its text.", nameof(request));
        }

        if (request.SourcePath is not null && !Path.IsPathFullyQualified(request.SourcePath))
        {
            throw new ArgumentException("The source path must be a full path.", nameof(request));
        }

        var stopwatch = Stopwatch.StartNew();
        AssemblerInfo assembler = Check();
        if (!assembler.IsUsable)
        {
            return new BuildResult(BuildOutcome.AssemblerMissing, ReadOnlyMemory<byte>.Empty, string.Empty, false, null, stopwatch.Elapsed, assembler)
            {
                Diagnostics = [BuildMessages.Create(BuildMessages.AssemblerMissing, null, null, assembler.Problem)],
            };
        }

        string work = Path.Combine(_options.TempRoot, "build-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            string source = request.SourcePath is null ? string.Empty : Path.GetFullPath(request.SourcePath);
            string workingDirectory = request.SourcePath is null ? work : Path.GetDirectoryName(source)!;

            // DEF-017: NASM reads a UTF-8 byte order mark as text ("instruction expected"), and the editor keeps a file's BOM
            // when it saves (FR-019). A saved file with a BOM is assembled from a copy without it, like unsaved text.
            string? text = request.UnsavedText;
            if (text is null && request.SourcePath is not null && await StartsWithUtf8BomAsync(source, cancellationToken).ConfigureAwait(false))
            {
                text = await File.ReadAllTextAsync(source, Encoding.UTF8, cancellationToken).ConfigureAwait(false); // the BOM is dropped
            }

            if (text is not null)
            {
                string copyFolder = Path.Combine(work, "src");
                Directory.CreateDirectory(copyFolder);
                source = Path.Combine(copyFolder, request.SourcePath is null ? UntitledFileName : Path.GetFileName(source));
                await File.WriteAllTextAsync(source, text, Utf8NoBom, cancellationToken).ConfigureAwait(false);
            }

            ProcessRun preprocess = await ProcessRunner.RunAsync(
                assembler.Path!, ["-Xgnu", "-E", source], workingDirectory, _options.Timeout, _options.OutputCap, cancellationToken).ConfigureAwait(false);

            BuildOutcome preprocessOutcome = Classify(preprocess);
            if (preprocessOutcome is BuildOutcome.Cancelled or BuildOutcome.TimedOut or BuildOutcome.Crashed)
            {
                var failDiagnostics = new List<Diagnostic>(BuildMessagesFor(preprocessOutcome, preprocess, [], request));
                if (assembler.Status != AssemblerStatus.Ready)
                {
                    failDiagnostics.Add(BuildMessages.Create(BuildMessages.VersionWarning, null, null, assembler.Problem));
                }

                return new BuildResult(preprocessOutcome, ReadOnlyMemory<byte>.Empty, preprocess.StandardError, preprocess.Truncated, preprocess.ExitCode, stopwatch.Elapsed, assembler)
                {
                    Diagnostics = failDiagnostics,
                };
            }

            IReadOnlyList<Diagnostic> cpuDiagnostics = CpuDirectiveGuard.Scan(preprocess.StandardOutput, source, request.SourcePath, workingDirectory);
            if (cpuDiagnostics.Count > 0)
            {
                var failDiagnostics = new List<Diagnostic>(cpuDiagnostics);
                if (assembler.Status != AssemblerStatus.Ready)
                {
                    failDiagnostics.Add(BuildMessages.Create(BuildMessages.VersionWarning, null, null, assembler.Problem));
                }

                return new BuildResult(BuildOutcome.Failed, ReadOnlyMemory<byte>.Empty, preprocess.StandardError, preprocess.Truncated, preprocess.ExitCode, stopwatch.Elapsed, assembler)
                {
                    Diagnostics = failDiagnostics,
                };
            }

            string image = Path.Combine(work, "program.com");
            string[] arguments = Arguments(Path.Combine(work, "program.lst"), image, source);

            ProcessRun run = await ProcessRunner.RunAsync(
                assembler.Path!, arguments, workingDirectory, _options.Timeout, _options.OutputCap, cancellationToken).ConfigureAwait(false);

            BuildOutcome outcome = Classify(run);
            ReadOnlyMemory<byte> bytes = outcome == BuildOutcome.Succeeded && File.Exists(image)
                ? await File.ReadAllBytesAsync(image, CancellationToken.None).ConfigureAwait(false)
                : ReadOnlyMemory<byte>.Empty;
            if (outcome == BuildOutcome.Succeeded && !File.Exists(image))
            {
                outcome = BuildOutcome.Failed;
            }

            var diagnostics = new List<Diagnostic>(NasmDiagnosticParser.Parse(run.StandardError, source, request.SourcePath));
            if (outcome == BuildOutcome.Succeeded && bytes.Length > BuildMessages.MaxComImageSize)
            {
                outcome = BuildOutcome.Failed;
                diagnostics.Add(BuildMessages.Create(BuildMessages.ImageTooLarge, request.SourcePath, null, bytes.Length));
                bytes = ReadOnlyMemory<byte>.Empty;
            }

            diagnostics.AddRange(BuildMessagesFor(outcome, run, diagnostics, request));
            if (assembler.Status != AssemblerStatus.Ready)
            {
                diagnostics.Add(BuildMessages.Create(BuildMessages.VersionWarning, null, null, assembler.Problem));
            }

            ISourceMap? sourceMap = null;
            if (outcome == BuildOutcome.Succeeded)
            {
                sourceMap = await ReadSourceMapAsync(Path.Combine(work, "program.lst"), run, source, request.SourcePath, workingDirectory).ConfigureAwait(false);
                if (sourceMap is null)
                {
                    diagnostics.Add(BuildMessages.Create(BuildMessages.SourceMapUnavailable, request.SourcePath, null));
                }
            }

            return new BuildResult(outcome, bytes, run.StandardError, run.Truncated, run.ExitCode, stopwatch.Elapsed, assembler)
            {
                Diagnostics = diagnostics,
                SourceMap = sourceMap,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new BuildResult(BuildOutcome.Cancelled, ReadOnlyMemory<byte>.Empty, string.Empty, false, null, stopwatch.Elapsed, assembler)
            {
                Diagnostics = [BuildMessages.Create(BuildMessages.Cancelled, null, null)],
            };
        }
        finally
        {
            TryDelete(work);
        }
    }

    /// <summary>Builds the source map from the listing file and the map NASM wrote to standard output (ADR-0005 §5).
    /// Returns <see langword="null"/> when they can't be read or parsed; the build still succeeds (FR-053).</summary>
    private static async Task<ISourceMap?> ReadSourceMapAsync(string listingFile, ProcessRun run, string assembledFile, string? documentFile, string workingDirectory)
    {
        if (run.Truncated || !File.Exists(listingFile))
        {
            return null;
        }

        try
        {
            string listing = await File.ReadAllTextAsync(listingFile, CancellationToken.None).ConfigureAwait(false);
            return NasmSourceMap.Build(listing, run.StandardOutput, assembledFile, documentFile, workingDirectory);
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>The build-level messages for an outcome (FR-030), and the MASM hint for a failed build (FR-021).</summary>
    private IEnumerable<Diagnostic> BuildMessagesFor(BuildOutcome outcome, ProcessRun run, IReadOnlyList<Diagnostic> parsed, BuildRequest request)
    {
        switch (outcome)
        {
            case BuildOutcome.TimedOut:
                yield return BuildMessages.Create(BuildMessages.TimedOut, null, null, _options.Timeout.TotalSeconds);
                break;

            case BuildOutcome.Crashed:
                yield return BuildMessages.Create(BuildMessages.Crashed, null, null, run.ExitCode);
                break;

            case BuildOutcome.Cancelled:
                yield return BuildMessages.Create(BuildMessages.Cancelled, null, null);
                break;

            case BuildOutcome.Failed when !parsed.Any(d => d.Severity == DiagnosticSeverity.Error):
                yield return BuildMessages.Create(BuildMessages.FailedWithoutDiagnostics, null, null, run.ExitCode);
                break;
        }

        if (outcome == BuildOutcome.Failed && run.ExitCode is not 0 && ReadSource(request) is { } text && MasmHint.Find(text) is { } hint)
        {
            yield return BuildMessages.Create(BuildMessages.MasmHint, request.SourcePath, hint.Line, hint.Construct);
        }
    }

    private static string? ReadSource(BuildRequest request)
    {
        if (request.UnsavedText is not null)
        {
            return request.UnsavedText;
        }

        try
        {
            return File.ReadAllText(request.SourcePath!);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static BuildOutcome Classify(ProcessRun run) => run switch
    {
        { Cancelled: true } => BuildOutcome.Cancelled,
        { TimedOut: true } => BuildOutcome.TimedOut,
        { ExitCode: 0 } => BuildOutcome.Succeeded,
        { ExitCode: int code } when IsAbnormalTermination(code) => BuildOutcome.Crashed,
        _ => BuildOutcome.Failed,
    };

    /// <summary>Windows reports an abnormal termination as an NTSTATUS error code (0xC0000000 and up,
    /// such as 0xC0000005 for an access violation); NASM's own failures exit with 1.</summary>
    private static bool IsAbnormalTermination(int exitCode) => unchecked((uint)exitCode) >= 0xC0000000;

    private AssemblerInfo QueryVersion(string path, AssemblerOrigin origin)
    {
        ProcessRun run;
        try
        {
            run = ProcessRunner.RunAsync(path, ["-v"], Path.GetDirectoryName(path)!, _options.VersionCheckTimeout, _options.OutputCap, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new AssemblerInfo(path, origin, null, AssemblerStatus.NotRunnable, AssemblerProblems.Text("NotStarted", path, ex.Message));
        }

        if (run.ExitCode != 0)
        {
            return new AssemblerInfo(path, origin, null, AssemblerStatus.NotRunnable,
                run.TimedOut
                    ? AssemblerProblems.Text("VersionTimedOut", path, _options.VersionCheckTimeout.TotalSeconds)
                    : AssemblerProblems.Text("VersionFailed", path, run.ExitCode));
        }

        Version? version = ParseVersion(run.StandardOutput + run.StandardError);
        if (version is null)
        {
            return new AssemblerInfo(path, origin, null, AssemblerStatus.UnknownVersion,
                AssemblerProblems.Text("NoVersion", path, $"{MinimumVersion.Major}.{MinimumVersion.Minor:00}"));
        }

        string text = $"{version.Major}.{version.Minor:00}";
        return version < MinimumVersion
            ? new AssemblerInfo(path, origin, text, AssemblerStatus.OlderVersion,
                AssemblerProblems.Text("OlderVersion", text, $"{MinimumVersion.Major}.{MinimumVersion.Minor:00}"))
            : new AssemblerInfo(path, origin, text, AssemblerStatus.Ready, null);
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // A killed process may still hold a file for a moment; the folder is under the temp root.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [GeneratedRegex(@"NASM version (\d+)\.(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
