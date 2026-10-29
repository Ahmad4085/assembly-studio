using System.Diagnostics;
using System.Text;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// TASK-040: the NASM runner (ADR-0005 §1–2). Builds use the bundled NASM 3.02 from
/// <c>tools\nasm</c>; the hang, crash, old-version and noisy cases use <c>.cmd</c> stubs in place of
/// nasm.exe (as in POC-002). Each test uses its own folders under the system temp directory.
/// </summary>
public sealed class NasmAssemblerTests : IDisposable
{
    private static readonly byte[] ExitImage = [0xB8, 0x00, 0x4C, 0xCD, 0x21]; // mov ax,4C00h / int 21h
    private const string ExitSource = "org 100h\nmov ax, 4C00h\nint 21h\n";

    private readonly string _root = Directory.CreateTempSubdirectory("asmstudio-task040-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string TempRoot => Path.Combine(_root, "builds");

    private NasmAssembler Assembler(string? bundled = null, string? configured = null, TimeSpan? timeout = null, int? cap = null) => new(new NasmOptions
    {
        BundledPath = bundled ?? NasmLocator.GetNasmPath(),
        ConfiguredPath = configured,
        Timeout = timeout ?? NasmOptions.DefaultTimeout,
        OutputCap = cap ?? NasmOptions.DefaultOutputCap,
        TempRoot = TempRoot,
    });

    private string Folder(string name)
    {
        string path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private string WriteSource(string folder, string fileName, string text)
    {
        string path = Path.Combine(Folder(folder), fileName);
        File.WriteAllText(path, text);
        return path;
    }

    /// <summary>A stub that answers <c>-v</c> like NASM 3.02 and runs <paramref name="build"/> for a build.</summary>
    private string Stub(string name, string build, string version = "NASM version 3.02 compiled on Jun 28 2026")
    {
        string path = Path.Combine(Folder("stubs"), name + ".cmd");
        File.WriteAllText(path, $"@echo off\r\nif \"%~1\"==\"-v\" (\r\n  echo {version}\r\n  exit /b 0\r\n)\r\n{build}\r\n");
        return path;
    }

    private static void AssertNoBuildFolders(string tempRoot) =>
        Assert.True(!Directory.Exists(tempRoot) || Directory.GetFileSystemEntries(tempRoot).Length == 0, "Per-build temp folders must be deleted.");

    // ---- Successful builds (FR-025, FR-026) ----

    [Fact]
    public async Task Builds_a_saved_file_into_a_flat_com_image()
    {
        string source = WriteSource("project", "exit.asm", ExitSource);

        BuildResult result = await Assembler().BuildAsync(new BuildRequest(source, null), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(ExitImage, result.Image.ToArray());
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Output);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.Equal(AssemblerStatus.Ready, result.Assembler.Status);
        AssertNoBuildFolders(TempRoot);
        Assert.Equal(["exit.asm"], Directory.GetFiles(Path.GetDirectoryName(source)!).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Unsaved_text_is_assembled_from_a_temp_copy_without_touching_the_file_or_its_folder()
    {
        string source = WriteSource("project", "exit.asm", "org 100h\nint 20h\n");
        string folder = Path.GetDirectoryName(source)!;

        BuildResult result = await Assembler().BuildAsync(new BuildRequest(source, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(ExitImage, result.Image.ToArray());
        Assert.Equal("org 100h\nint 20h\n", File.ReadAllText(source));
        Assert.Equal(["exit.asm"], Directory.GetFiles(folder).Select(Path.GetFileName));
        AssertNoBuildFolders(TempRoot);
    }

    [Fact]
    public async Task An_untitled_document_builds_from_its_text()
    {
        BuildResult result = await Assembler().BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(ExitImage, result.Image.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Includes_resolve_from_the_source_folder_for_saved_and_unsaved_builds(bool unsaved)
    {
        string source = WriteSource("with include", "main.asm", "org 100h\n%include \"exit.inc\"\n");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(source)!, "exit.inc"), "mov ax, 4C00h\nint 21h\n");

        BuildResult result = await Assembler().BuildAsync(
            new BuildRequest(source, unsaved ? File.ReadAllText(source) : null), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(ExitImage, result.Image.ToArray());
    }

    [Fact]
    public async Task The_8086_restriction_is_injected()
    {
        BuildResult result = await Assembler().BuildAsync(new BuildRequest(null, "org 100h\npush 1234h\n"), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Failed, result.Outcome); // PUSH imm16 is a 186 instruction
        Assert.Contains("error", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_source_error_fails_with_the_assembler_output_and_no_image()
    {
        BuildResult result = await Assembler().BuildAsync(new BuildRequest(null, "org 100h\nmov ax,\n"), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.Equal(1, result.ExitCode);
        Assert.True(result.Image.IsEmpty);
        Assert.Contains("untitled.asm:2: error:", result.Output, StringComparison.Ordinal);
        AssertNoBuildFolders(TempRoot);
    }

    // ---- NFR-014: malicious file names reach NASM as one argument, with no shell ----

    [Theory]
    [InlineData("& echo pwned & del x & .asm")]
    [InlineData("-o evil.asm")]
    [InlineData("a;b'c %PATH% $(x).asm")]
    public async Task A_file_name_with_shell_or_option_characters_is_just_a_file_name(string fileName)
    {
        string source = WriteSource("hostile", fileName, ExitSource);
        string folder = Path.GetDirectoryName(source)!;

        BuildResult result = await Assembler().BuildAsync(new BuildRequest(source, null), TestContext.Current.CancellationToken);
        BuildResult unsaved = await Assembler().BuildAsync(new BuildRequest(source, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(ExitImage, result.Image.ToArray());
        Assert.Equal(BuildOutcome.Succeeded, unsaved.Outcome);
        Assert.Equal([fileName], Directory.GetFiles(folder).Select(Path.GetFileName));
    }

    // ---- FR-023, FR-024, AC-015, AC-016: locating and checking the assembler ----

    [Fact]
    public void Check_reports_the_bundled_path_origin_and_version()
    {
        string bundled = NasmLocator.GetNasmPath();

        AssemblerInfo info = Assembler(bundled).Check();

        Assert.Equal(new AssemblerInfo(bundled, AssemblerOrigin.Bundled, "3.02", AssemblerStatus.Ready, null), info);
        Assert.True(info.IsUsable);
    }

    [Fact]
    public async Task A_configured_path_overrides_the_bundled_one()
    {
        string configured = NasmLocator.GetNasmPath();
        NasmAssembler assembler = Assembler(bundled: Path.Combine(_root, "missing", "nasm.exe"), configured: configured);

        AssemblerInfo info = assembler.Check();
        BuildResult result = await assembler.BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(AssemblerOrigin.Configured, info.Origin);
        Assert.Equal(configured, info.Path);
        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
    }

    [Fact]
    public async Task AC_016_a_missing_bundled_assembler_attempts_no_build()
    {
        NasmAssembler assembler = Assembler(bundled: Path.Combine(_root, "missing", "nasm.exe"));

        BuildResult result = await assembler.BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.AssemblerMissing, result.Outcome);
        Assert.Equal(AssemblerStatus.Missing, result.Assembler.Status);
        Assert.Contains("Settings", result.Assembler.Problem, StringComparison.Ordinal);
        Assert.Null(result.ExitCode);
        Assert.False(Directory.Exists(TempRoot)); // nothing was prepared
        Diagnostic missing = Assert.Single(result.Diagnostics);
        Assert.Equal(("BLD001", result.Assembler.Problem), (missing.Code, missing.What));
    }

    [Theory]
    [InlineData("nasm.exe", AssemblerStatus.InvalidPath)]                // not a full path
    [InlineData(@"\\server\share\nasm.exe", AssemblerStatus.InvalidPath)] // network path
    [InlineData("{root}\\missing\\nasm.exe", AssemblerStatus.Missing)]
    [InlineData("{root}\\stubs\\notnasm.cmd", AssemblerStatus.InvalidPath)] // exists, but not an .exe
    public async Task AC_016_an_invalid_configured_path_is_rejected_without_falling_back(string configured, AssemblerStatus status)
    {
        Stub("notnasm", "exit /b 0");
        configured = configured.Replace("{root}", _root, StringComparison.Ordinal);
        NasmAssembler assembler = Assembler(configured: configured); // the bundled NASM exists

        BuildResult result = await assembler.BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.AssemblerMissing, result.Outcome);
        Assert.Equal(AssemblerOrigin.Configured, result.Assembler.Origin);
        Assert.Equal(status, result.Assembler.Status);
        Assert.Contains("Settings", result.Assembler.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateConfiguredPath_accepts_a_full_path_to_an_existing_exe()
    {
        Assert.Null(NasmAssembler.ValidateConfiguredPath(NasmLocator.GetNasmPath()));
        Assert.NotNull(NasmAssembler.ValidateConfiguredPath(""));
        Assert.NotNull(NasmAssembler.ValidateConfiguredPath(@"C:\no\such\nasm.exe"));
    }

    [Theory]
    [InlineData("NASM version 3.02 compiled on Jun 28 2026", 3, 2)]
    [InlineData("NASM version 2.16.03 compiled on Apr 17 2024", 2, 16)]
    [InlineData("NASM version 3.10rc1", 3, 10)]
    public void ParseVersion_reads_major_and_minor(string output, int major, int minor)
    {
        Assert.Equal(new Version(major, minor), NasmAssembler.ParseVersion(output));
    }

    [Fact]
    public void ParseVersion_returns_null_for_other_output()
    {
        Assert.Null(NasmAssembler.ParseVersion("YASM 1.3.0"));
    }

    [Fact]
    public async Task An_older_version_warns_but_still_builds()
    {
        string stub = Stub("old", "exit /b 0", version: "NASM version 2.16.03 compiled on Apr 17 2024");

        BuildResult result = await Assembler(bundled: stub).BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(AssemblerStatus.OlderVersion, result.Assembler.Status);
        Assert.Equal("2.16", result.Assembler.Version);
        Assert.Contains("3.02", result.Assembler.Problem, StringComparison.Ordinal);
        Assert.True(result.Assembler.IsUsable);
        Assert.Equal(BuildOutcome.Failed, result.Outcome); // the stub exits 0 without writing an image
        Assert.Equal(["BLD004", "BLD008"], result.Diagnostics.Select(d => d.Code));
    }

    [Fact]
    public void A_file_that_fails_the_version_request_is_not_runnable()
    {
        string stub = Path.Combine(Folder("stubs"), "broken.cmd");
        File.WriteAllText(stub, "@exit /b 3\r\n");

        AssemblerInfo info = Assembler(bundled: stub).Check();

        Assert.Equal(AssemblerStatus.NotRunnable, info.Status);
        Assert.False(info.IsUsable);
    }

    // ---- AC-019, FR-030, FR-095: hang, crash, cancel, and the next build still works ----

    [Fact]
    public async Task AC_019_a_hanging_assembler_is_killed_at_the_timeout_and_the_next_build_works()
    {
        string stub = Stub("hang", "ping -n 30 127.0.0.1 >nul");
        var stopwatch = Stopwatch.StartNew();

        BuildResult hung = await Assembler(bundled: stub, timeout: TimeSpan.FromSeconds(1))
            .BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.TimedOut, hung.Outcome);
        Assert.Null(hung.ExitCode);
        Assert.Equal(["BLD002"], hung.Diagnostics.Select(d => d.Code));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The timeout kill took {stopwatch.Elapsed}.");
        AssertNoBuildFolders(TempRoot);

        BuildResult next = await Assembler().BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);
        Assert.Equal(BuildOutcome.Succeeded, next.Outcome);
    }

    [Fact]
    public async Task AC_019_a_crashing_assembler_is_reported_and_the_next_build_works()
    {
        string stub = Stub("crash", "exit /b -1073741819"); // 0xC0000005, access violation

        BuildResult crashed = await Assembler(bundled: stub).BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Crashed, crashed.Outcome);
        Assert.Equal(unchecked((int)0xC0000005), crashed.ExitCode);
        Diagnostic crash = Assert.Single(crashed.Diagnostics);
        Assert.Equal("BLD003", crash.Code);
        Assert.Contains("0xC0000005", crash.What, StringComparison.Ordinal);

        BuildResult next = await Assembler().BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);
        Assert.Equal(BuildOutcome.Succeeded, next.Outcome);
    }

    [Fact]
    public async Task A_non_zero_exit_without_output_is_a_failure_with_the_raw_output()
    {
        string stub = Stub("silent", "exit /b 2");

        BuildResult result = await Assembler(bundled: stub).BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.Output);
        Assert.Equal(["BLD004"], result.Diagnostics.Select(d => d.Code));
    }

    [Fact]
    public async Task Cancelling_kills_the_build()
    {
        string stub = Stub("hang", "ping -n 30 127.0.0.1 >nul");
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancel.CancelAfter(TimeSpan.FromMilliseconds(500));
        var stopwatch = Stopwatch.StartNew();

        BuildResult result = await Assembler(bundled: stub).BuildAsync(new BuildRequest(null, ExitSource), cancel.Token);

        Assert.Equal(BuildOutcome.Cancelled, result.Outcome);
        Assert.Equal(["BLD005"], result.Diagnostics.Select(d => d.Code));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Cancelling took {stopwatch.Elapsed}.");
        AssertNoBuildFolders(TempRoot);
    }

    [Fact]
    public async Task Output_beyond_the_cap_is_cut_and_flagged()
    {
        string noise = Path.Combine(Folder("stubs"), "noise.txt");
        File.WriteAllText(noise, string.Concat(Enumerable.Repeat(new string('x', 99) + "\n", 1000))); // 100,000 chars
        string stub = Stub("noisy", $"type \"{noise}\" 1>&2\r\nexit /b 1");

        BuildResult result = await Assembler(bundled: stub, cap: 10_000).BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken);

        Assert.True(result.OutputTruncated);
        Assert.InRange(result.Output.Length, 9_000, 10_000);
        Assert.Equal(BuildOutcome.Failed, result.Outcome);
    }

    // ---- NFR-006: build time is measured ----

    [Fact]
    public async Task A_1000_line_source_builds_within_the_2_second_target()
    {
        var text = new StringBuilder("org 100h\n");
        for (int i = 0; i < 999; i++)
        {
            text.Append("add ax, ").Append(i).Append('\n');
        }

        NasmAssembler assembler = Assembler();
        await assembler.BuildAsync(new BuildRequest(null, ExitSource), TestContext.Current.CancellationToken); // first launch excluded (NFR-006)

        BuildResult result = await assembler.BuildAsync(new BuildRequest(null, text.ToString()), TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper?.WriteLine($"1,000-line build: {result.Duration.TotalMilliseconds:F0} ms (NFR-006 target 2000 ms)");
        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.True(result.Duration < TimeSpan.FromSeconds(2), $"Build took {result.Duration}.");
    }
}
