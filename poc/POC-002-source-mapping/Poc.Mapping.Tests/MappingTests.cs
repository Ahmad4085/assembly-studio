using Xunit;

namespace Poc.Mapping.Tests;

/// <summary>POC-002 exit criteria 1–4 against the real NASM 3.02 (requires nasm.exe on PATH or NASM_PATH).</summary>
public class MappingTests
{
    private static readonly string FixtureDir = FindFixtures();
    private static readonly Nasm Assembler = new(Nasm.FindOnPath(), TimeSpan.FromSeconds(10));
    private static string NdisasmPath => Path.Combine(Path.GetDirectoryName(Assembler.NasmPath)!, "ndisasm.exe");

    private static string FindFixtures()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "fixtures"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("fixtures/ not found"), "fixtures");
    }

    private static string Fixture(string name) => Path.Combine(FixtureDir, name);

    private static (SourceMap Map, byte[] Image) BuildAndMap(string fixture)
    {
        var build = Assembler.Build(Fixture(fixture));
        Assert.True(build.ExitCode == 0, $"{fixture}: nasm exit {build.ExitCode}\n{build.RawOutput}");
        return (SourceMap.Build(Fixture(fixture), build.ListingText!, build.MapText!), build.Image!);
    }

    public static TheoryData<string> BuildableFixtures => new() { "01-single.asm", "02-sections.asm", "03-times-align.asm", "04-macro.asm", "05-include.asm" };

    [Theory]
    [MemberData(nameof(BuildableFixtures))]
    public void Every_mapped_byte_matches_the_binary(string fixture)
    {
        var (sm, image) = BuildAndMap(fixture);
        Assert.NotEmpty(sm.Ranges);
        Assert.Empty(Verification.CheckBytes(sm, image));
    }

    [Theory]
    [MemberData(nameof(BuildableFixtures))]
    public void Label_addresses_agree_with_the_map_symbols(string fixture)
    {
        var (sm, _) = BuildAndMap(fixture);
        Assert.Empty(Verification.CheckSymbols(sm));
    }

    [Theory]
    [MemberData(nameof(BuildableFixtures))]
    public void Code_ranges_start_on_instruction_boundaries(string fixture)
    {
        var (sm, image) = BuildAndMap(fixture);
        Assert.Empty(Verification.CheckInstructionBoundaries(sm, image, NdisasmPath));
    }

    [Fact]
    public void Single_section_lines_map_to_expected_addresses()
    {
        var (sm, _) = BuildAndMap("01-single.asm");
        string f = Fixture("01-single.asm");
        Assert.Equal(0x100, sm.FirstAddressOf(f, 4));          // mov ax, 3
        Assert.Equal(0x103, sm.FirstAddressOf(f, 5));          // add ax, 4
        Assert.Null(sm.FirstAddressOf(f, 3));                  // label line: no bytes
        Assert.Equal(new SourceLocation(Path.GetFullPath(f), 8), sm.LineAt(0x10C)); // mov ah, 4Ch (B4 4C at 10C)
    }

    [Fact]
    public void Second_section_and_section_reentry_use_their_own_base()
    {
        var (sm, _) = BuildAndMap("02-sections.asm");
        string f = Fixture("02-sections.asm");
        Assert.Equal(sm.Map.Symbols["msg"], sm.FirstAddressOf(f, 11));
        Assert.Equal(0x10F, sm.FirstAddressOf(f, 15));         // nop after re-entering .text continues at offset 0Fh
    }

    [Fact]
    public void Times_and_align_cover_their_full_length()
    {
        var (sm, _) = BuildAndMap("03-times-align.asm");
        string f = Fixture("03-times-align.asm");
        var times = sm.Ranges.Single(r => r.Location.Line == 4 && r.Location.File == Path.GetFullPath(f));
        Assert.Equal((0x102, 5), (times.Address, times.Length));
        Assert.Equal(sm.Map.Symbols["main"], sm.FirstAddressOf(f, 7));
        Assert.Equal(new SourceLocation(Path.GetFullPath(f), 8), sm.LineAt(0x114)); // middle of "times 3 inc ax"
    }

    [Fact]
    public void Macro_expansion_maps_to_the_invoking_line()
    {
        var (sm, _) = BuildAndMap("04-macro.asm");
        string f = Path.GetFullPath(Fixture("04-macro.asm"));
        Assert.Equal(new SourceLocation(f, 8), sm.LineAt(0x100));   // first PRINT
        Assert.Equal(new SourceLocation(f, 8), sm.LineAt(0x105));
        Assert.Equal(new SourceLocation(f, 9), sm.LineAt(0x107));   // second PRINT
        Assert.True(sm.Ranges.Where(r => r.Location.Line is 8 or 9).All(r => r.FromMacroExpansion));
        Assert.Equal(new SourceLocation(f, 10), sm.LineAt(0x10E));
        Assert.Null(sm.FirstAddressOf(f, 4));                       // macro definition body itself has no address
    }

    [Fact]
    public void Included_file_lines_map_to_the_included_file()
    {
        var (sm, _) = BuildAndMap("05-include.asm");
        string main = Path.GetFullPath(Fixture("05-include.asm"));
        string inc = Path.GetFullPath(Fixture("inc/helpers.inc"));
        Assert.Equal(new SourceLocation(inc, 3), sm.LineAt(sm.Map.Symbols["newline"]));
        Assert.Equal(new SourceLocation(inc, 6), sm.LineAt(0x10E));  // ret
        Assert.Equal(new SourceLocation(main, 7), sm.LineAt(0x10F)); // nop back in the main file
    }

    [Fact]
    public void Errors_are_parsed_with_lines_and_severities_and_no_image_is_produced()
    {
        var build = Assembler.Build(Fixture("06-errors.asm"));
        Assert.Equal(1, build.ExitCode);
        Assert.Null(build.Image);
        Assert.True(string.IsNullOrEmpty(build.MapText));            // measured: map file created but left empty on failure
        Assert.NotNull(build.ListingText);                           // measured: listing still written
        Assert.Collection(build.Diagnostics,
            d => Assert.Equal((3, Severity.Error, (string?)null), (d.Line, d.Severity, d.WarningClass)),
            d => Assert.Equal((4, Severity.Error, (string?)null), (d.Line, d.Severity, d.WarningClass)),
            d => Assert.Equal((5, Severity.Warning, (string?)"number-overflow"), (d.Line, d.Severity, d.WarningClass)));
        Assert.Contains(Listing.Parse(build.ListingText!), r => r.ErrorText is not null && r.LineNumber == 3);
    }

    [Fact]
    public void Cpu_directive_in_source_overrides_the_injected_8086_level()
    {
        var build = Assembler.Build(Fixture("07-cpu386.asm"));
        Assert.Equal(0, build.ExitCode);                             // the bypass exists: NASM accepts pusha
        Assert.Equal(["386"], Verification.CpuDirectives(Assembler.Preprocess(Fixture("07-cpu386.asm"))));
    }

    [Fact]
    public void Cpu_directive_hidden_in_a_macro_is_still_detected()
    {
        string dir = Directory.CreateTempSubdirectory("poc002-hidden-").FullName;
        string src = Path.Combine(dir, "hidden.asm");
        File.WriteAllText(src, "%macro SETCPU 0\ncpu 386\n%endmacro\norg 100h\nSETCPU\npusha\n");
        Assert.Equal(["386"], Verification.CpuDirectives(Assembler.Preprocess(src)));
        Assert.Empty(Verification.CpuDirectives(Assembler.Preprocess(Fixture("01-single.asm"))));
    }

    [Fact]
    public void Hanging_assembler_is_killed_after_the_timeout()
    {
        // A stub that ignores its arguments and blocks for ~30 s stands in for a hung assembler.
        string stub = Path.Combine(Directory.CreateTempSubdirectory("poc002-hang-").FullName, "hang.cmd");
        File.WriteAllText(stub, "@ping -n 30 127.0.0.1 >nul\r\n");
        var slow = new Nasm(stub, TimeSpan.FromSeconds(1));
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = slow.Build(Fixture("01-single.asm"));
        Assert.True(result.TimedOut);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }
}
