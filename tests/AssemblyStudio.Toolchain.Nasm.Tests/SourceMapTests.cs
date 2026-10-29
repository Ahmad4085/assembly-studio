using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm.SourceMapping;
using LineMapping = AssemblyStudio.Core.SourceMapping;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// TASK-043: the source map (ADR-0005 §5). The POC-002 fixtures (01–07) and the cases ADR-0005 left
/// unverified (08 ≥10-level nesting, 09 nested includes in subfolders, 10 <c>%rep</c>, 11 a section switch
/// inside a macro) plus 12 (wrapped data, reserved space) are built with the bundled NASM 3.02, and every
/// map is checked three independent ways: listing bytes against the image, label addresses against the
/// map's symbols, and code ranges against ndisasm's instruction boundaries.
/// </summary>
public sealed partial class SourceMapTests
{
    private static readonly string FixtureDir = FindFixtures();

    public static TheoryData<string> MappedFixtures => new()
    {
        "01-single.asm", "02-sections.asm", "03-times-align.asm", "04-macro.asm", "05-include.asm",
        "08-deep-nesting.asm", "09-nested-include.asm", "10-rep.asm", "11-macro-section.asm", "12-long-data.asm",
    };

    private static string FindFixtures()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."),
            "tests", "AssemblyStudio.Toolchain.Nasm.Tests", "fixtures");
    }

    private static string Fixture(string name) => Path.GetFullPath(Path.Combine(FixtureDir, name));

    private static NasmAssembler Assembler() => new(new NasmOptions
    {
        BundledPath = NasmLocator.GetNasmPath(),
        TempRoot = Path.Combine(Path.GetTempPath(), "asmstudio-task043"),
    });

    private static async Task<(NasmSourceMap Map, byte[] Image)> BuildAndMap(string fixture)
    {
        BuildResult result = await Assembler().BuildAsync(new BuildRequest(Fixture(fixture), null), TestContext.Current.CancellationToken);
        Assert.True(result.Outcome == BuildOutcome.Succeeded, $"{fixture}: {result.Outcome}\n{result.Output}");
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == "BLD010");
        return (Assert.IsType<NasmSourceMap>(result.SourceMap), result.Image.ToArray());
    }

    private static LineMapping At(string fixture, int line, bool macro = false) => new(new SourceLocation(Fixture(fixture), line), macro);

    // ---- The three verifiers on every fixture ----

    [Theory]
    [MemberData(nameof(MappedFixtures))]
    public async Task Every_mapped_byte_matches_the_image(string fixture)
    {
        (NasmSourceMap map, byte[] image) = await BuildAndMap(fixture);

        Assert.NotEmpty(map.Ranges);
        Assert.Empty(CheckBytes(map, image));
    }

    [Theory]
    [MemberData(nameof(MappedFixtures))]
    public async Task Label_addresses_agree_with_the_map_symbols(string fixture)
    {
        (NasmSourceMap map, _) = await BuildAndMap(fixture);

        Assert.Empty(CheckSymbols(map));
    }

    [Theory]
    [MemberData(nameof(MappedFixtures))]
    public async Task Code_ranges_start_on_instruction_boundaries(string fixture)
    {
        (NasmSourceMap map, byte[] image) = await BuildAndMap(fixture);

        Assert.Empty(CheckInstructionBoundaries(map, image));
    }

    // ---- POC-002 fixtures (regression) ----

    [Fact]
    public async Task Single_section_lines_map_to_their_addresses()
    {
        (NasmSourceMap map, _) = await BuildAndMap("01-single.asm");
        string file = Fixture("01-single.asm");

        Assert.Equal((ushort)0x100, map.FirstOffsetOf(file, 4)); // mov ax, 3
        Assert.Equal((ushort)0x103, map.FirstOffsetOf(file, 5)); // add ax, 4
        Assert.Null(map.FirstOffsetOf(file, 3));                 // label only
        Assert.Equal(At("01-single.asm", 8), map.Find(0x10C));   // mov ah, 4Ch
    }

    [Fact]
    public async Task A_second_section_and_re_entry_use_their_own_base()
    {
        (NasmSourceMap map, _) = await BuildAndMap("02-sections.asm");
        string file = Fixture("02-sections.asm");

        Assert.Equal((ushort)map.Map.Symbols["msg"], map.FirstOffsetOf(file, 11));
        Assert.Equal((ushort)0x10F, map.FirstOffsetOf(file, 15)); // nop after re-entering .text
    }

    [Fact]
    public async Task Times_and_align_cover_their_full_length()
    {
        (NasmSourceMap map, _) = await BuildAndMap("03-times-align.asm");
        string file = Fixture("03-times-align.asm");

        MappedRange times = Assert.Single(map.Ranges, r => r.Mapping.Location.Line == 4);
        Assert.Equal((0x102, 5), (times.Address, times.Length));
        Assert.Equal((ushort)map.Map.Symbols["main"], map.FirstOffsetOf(file, 7));
        Assert.Equal(At("03-times-align.asm", 8), map.Find(0x114)); // middle of "times 3 inc ax"
    }

    [Fact]
    public async Task A_macro_expansion_maps_to_the_invoking_line()
    {
        (NasmSourceMap map, _) = await BuildAndMap("04-macro.asm");

        Assert.Equal(At("04-macro.asm", 8, macro: true), map.Find(0x100));
        Assert.Equal(At("04-macro.asm", 8, macro: true), map.Find(0x105));
        Assert.Equal(At("04-macro.asm", 9, macro: true), map.Find(0x107));
        Assert.Equal(At("04-macro.asm", 10), map.Find(0x10E));
        Assert.Null(map.FirstOffsetOf(Fixture("04-macro.asm"), 4)); // the macro body itself has no address
    }

    [Fact]
    public async Task Included_lines_map_to_the_included_file()
    {
        (NasmSourceMap map, _) = await BuildAndMap("05-include.asm");
        string include = Fixture("inc/helpers.inc");

        Assert.Equal(new LineMapping(new SourceLocation(include, 3), false), map.Find((ushort)map.Map.Symbols["newline"]));
        Assert.Equal(new LineMapping(new SourceLocation(include, 6), false), map.Find(0x10E)); // ret
        Assert.Equal(At("05-include.asm", 7), map.Find(0x10F));                                // nop back in the main file
    }

    [Theory]
    [InlineData("06-errors.asm")] // assembler errors
    [InlineData("07-cpu386.asm")] // rejected by the CPU guard (TASK-042)
    public async Task A_failed_build_has_no_source_map(string fixture)
    {
        BuildResult result = await Assembler().BuildAsync(new BuildRequest(Fixture(fixture), null), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.Null(result.SourceMap);
    }

    // ---- Cases ADR-0005 left unverified ----

    [Fact]
    public async Task Twelve_nested_macro_levels_map_to_the_outermost_invocation()
    {
        (NasmSourceMap map, _) = await BuildAndMap("08-deep-nesting.asm");

        Assert.Equal(12, map.Rows.Max(r => r.Level)); // levels 9-12 are found although their marker column shifts
        for (ushort offset = 0x100; offset <= 0x103; offset++)
        {
            Assert.Equal(At("08-deep-nesting.asm", 42, macro: true), map.Find(offset)); // inc ax/bx/cx/dx of the first L12
        }

        Assert.Equal(At("08-deep-nesting.asm", 43), map.Find(0x104)); // nop
        Assert.Equal(At("08-deep-nesting.asm", 44, macro: true), map.Find(0x105));
        Assert.Equal(At("08-deep-nesting.asm", 44, macro: true), map.Find(0x108));
        Assert.Equal(At("08-deep-nesting.asm", 45), map.Find(0x109));
    }

    [Fact]
    public async Task Nested_includes_in_subfolders_map_to_each_file()
    {
        (NasmSourceMap map, _) = await BuildAndMap("09-nested-include.asm");
        string outer = Fixture("inc/outer.inc");
        string inner = Fixture("inc/sub/inner.inc");

        Assert.Equal(new LineMapping(new SourceLocation(outer, 3), false), map.Find(0x10B)); // inc ax
        Assert.Equal(new LineMapping(new SourceLocation(inner, 3), false), map.Find((ushort)map.Map.Symbols["inner"]));
        Assert.Equal(new LineMapping(new SourceLocation(inner, 4), false), map.Find(0x10D)); // inner ret
        Assert.Equal(new LineMapping(new SourceLocation(outer, 5), false), map.Find(0x10E)); // outer ret
        Assert.Equal(At("09-nested-include.asm", 8), map.Find(0x10F));                        // nop in the main file
    }

    [Fact]
    public async Task Rep_bodies_map_to_their_own_lines()
    {
        (NasmSourceMap map, _) = await BuildAndMap("10-rep.asm");

        Assert.Equal(At("10-rep.asm", 5), map.Find(0x100)); // add ax, 0
        Assert.Equal(At("10-rep.asm", 5), map.Find(0x106)); // add ax, 2
        Assert.Equal(At("10-rep.asm", 10), map.Find(0x109)); // inner %rep: inc cx
        Assert.Equal(At("10-rep.asm", 12), map.Find(0x10B)); // outer %rep: inc dx
        Assert.Equal(At("10-rep.asm", 10), map.Find(0x10D));
        Assert.Equal(At("10-rep.asm", 14), map.Find(0x10F)); // mov ax, 4C00h
        Assert.Equal((ushort)0x109, map.FirstOffsetOf(Fixture("10-rep.asm"), 10));
    }

    [Fact]
    public async Task A_section_switch_inside_a_macro_maps_code_and_data_to_the_invoking_line()
    {
        (NasmSourceMap map, _) = await BuildAndMap("11-macro-section.asm");

        Assert.Equal(At("11-macro-section.asm", 10, macro: true), map.Find(0x100));                          // mov dx, msg1
        Assert.Equal(At("11-macro-section.asm", 10, macro: true), map.Find((ushort)map.Map.Symbols["msg1"])); // its data
        Assert.Equal(At("11-macro-section.asm", 11), map.Find(0x103));                                       // mov ah, 09h
        Assert.Equal(At("11-macro-section.asm", 13, macro: true), map.Find(0x107));
        Assert.Equal(At("11-macro-section.asm", 13, macro: true), map.Find((ushort)map.Map.Symbols["msg2"]));
        Assert.Equal(At("11-macro-section.asm", 14), map.Find(0x10A));
    }

    [Fact]
    public async Task Wrapped_data_maps_every_byte_and_reserved_space_maps_nothing()
    {
        (NasmSourceMap map, _) = await BuildAndMap("12-long-data.asm");

        for (int offset = 0x10C; offset < 0x140; offset++)
        {
            Assert.Equal(At("12-long-data.asm", 8), map.Find((ushort)offset));
        }

        Assert.Equal(At("12-long-data.asm", 9), map.Find(0x145));  // last byte of times 3 dw
        Assert.Equal(At("12-long-data.asm", 11), map.Find(0x146)); // nop
        Assert.Null(map.Find((ushort)map.Map.Symbols["buffer"]));  // resb in .bss
        Assert.Null(map.FirstOffsetOf(Fixture("12-long-data.asm"), 13));
    }

    // ---- Documents that are not saved as built ----

    [Fact]
    public async Task An_unsaved_build_maps_lines_to_the_document_and_an_untitled_one_to_null()
    {
        string source = Fixture("01-single.asm");
        string text = await File.ReadAllTextAsync(source, TestContext.Current.CancellationToken);

        BuildResult unsaved = await Assembler().BuildAsync(new BuildRequest(source, text), TestContext.Current.CancellationToken);
        BuildResult untitled = await Assembler().BuildAsync(new BuildRequest(null, text), TestContext.Current.CancellationToken);

        Assert.Equal(At("01-single.asm", 4), unsaved.SourceMap!.Find(0x100));
        Assert.Equal(new LineMapping(new SourceLocation(null, 4), false), untitled.SourceMap!.Find(0x100));
        Assert.Equal((ushort)0x103, untitled.SourceMap.FirstOffsetOf(null, 5));
    }

    [Fact]
    public async Task Addresses_outside_the_program_have_no_line()
    {
        (NasmSourceMap map, _) = await BuildAndMap("01-single.asm");

        Assert.Null(map.Find(0x0000));
        Assert.Null(map.Find(0x00FF));
        Assert.Null(map.Find(0xFFFF));
    }

    [Fact]
    public void A_map_without_sections_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => NasmSourceMap.Build("     1 00000000 90   nop\n", "not a map", @"C:\x\a.asm", null, @"C:\x"));
    }

    // ---- Verifiers (ported from POC-002) ----

    /// <summary>Every known listing byte equals the image byte at its address.</summary>
    private static List<string> CheckBytes(NasmSourceMap map, byte[] image)
    {
        var problems = new List<string>();
        foreach (MappedRange range in map.Ranges.Where(r => !r.IsReserved))
        {
            int fileStart = range.Address - map.Map.Origin; // bin format without vstart/follows: Start == Vstart
            for (int i = 0; i < range.Bytes.Count; i++)
            {
                if (range.Bytes[i] is not byte expected)
                {
                    continue; // relocated: final only in the image
                }

                int position = fileStart + i;
                if (position < 0 || position >= image.Length)
                {
                    problems.Add($"{range.Address + i:X4} outside the image ({range.Mapping})");
                    break;
                }

                if (image[position] != expected)
                {
                    problems.Add($"{range.Address + i:X4}: image {image[position]:X2} != listing {expected:X2} ({range.Mapping})");
                }
            }
        }

        return problems;
    }

    /// <summary>Each label's address in the map equals the first range at or after the label's listing row.</summary>
    private static List<string> CheckSymbols(NasmSourceMap map)
    {
        var problems = new List<string>();
        foreach (ListingRow row in map.Rows.Where(r => r.ErrorText is null))
        {
            Match label = LabelDefinition().Match(row.Source);
            if (!label.Success || !map.Map.Symbols.TryGetValue(label.Groups["name"].Value, out int address))
            {
                continue;
            }

            MappedRange? next = map.Ranges.Where(r => r.ListingIndex >= row.Index).OrderBy(r => r.ListingIndex).FirstOrDefault();
            if (next is not null && !next.IsReserved && next.Address != address)
            {
                problems.Add($"symbol {label.Groups["name"].Value}: map {address:X4} != mapped {next.Address:X4}");
            }
        }

        return problems;
    }

    /// <summary>Every instruction range in .text starts on an instruction boundary according to ndisasm. Data rows
    /// (db/dw/dd/dq/dt, also via times) are skipped: ndisasm decodes data inside .text as instructions.</summary>
    private static List<string> CheckInstructionBoundaries(NasmSourceMap map, byte[] image)
    {
        string file = Path.Combine(Path.GetTempPath(), $"asmstudio-task043-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(file, image);
        try
        {
            var start = new ProcessStartInfo(NasmLocator.GetNdisasmPath()) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            foreach (string argument in new[] { "-b", "16", "-o", map.Map.Origin.ToString("X", CultureInfo.InvariantCulture) + "h", file })
            {
                start.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(start)!;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            HashSet<int> starts = [.. output.Split('\n')
                .Where(l => l.Length >= 8)
                .Select(l => int.TryParse(l.AsSpan(0, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int a) ? a : -1)
                .Where(a => a >= 0)];

            int codeStart = map.Map.SectionStarts[".text"];
            int codeEnd = map.Map.SectionStarts.Values.Where(s => s > codeStart).DefaultIfEmpty(int.MaxValue).Min();
            var dataRows = new HashSet<int>();
            ListingRow? previous = null;
            foreach (ListingRow row in map.Rows)
            {
                // A wrapped data row continues on rows with the same line number and no source text.
                bool continuation = previous is not null && dataRows.Contains(previous.Index)
                    && row.LineNumber == previous.LineNumber && string.IsNullOrWhiteSpace(row.Source);
                if (continuation || DataDirective().IsMatch(row.Source))
                {
                    dataRows.Add(row.Index);
                }

                previous = row;
            }

            return [.. map.Ranges
                .Where(r => r.Section == ".text" && r.Address < codeEnd && !dataRows.Contains(r.ListingIndex) && !starts.Contains(r.Address))
                .Select(r => $"{r.Address:X4} ({r.Mapping}) is not an instruction start per ndisasm")];
        }
        finally
        {
            File.Delete(file);
        }
    }

    [GeneratedRegex(@"^\s*(?<name>[A-Za-z_.?@$][\w.?@$#~]*)\s*(?::|\s+(?:db|dw|dd|times|resb|resw)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex LabelDefinition();

    [GeneratedRegex(@"\b(?:db|dw|dd|dq|dt)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DataDirective();
}
