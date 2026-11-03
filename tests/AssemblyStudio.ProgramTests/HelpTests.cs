using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AssemblyStudio.Content;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using AssemblyStudio.Runtime.Loading;
using Xunit;
using static AssemblyStudio.ProgramTests.ProgramHarness;

namespace AssemblyStudio.ProgramTests;

/// <summary>
/// TASK-093, AC-051 (FR-045, NFR-033): the help pages agree with the implementation. "Unsupported instructions" is
/// generated from the engine's opcode table (regenerate with <c>ASSEMBLY_STUDIO_WRITE_REFERENCE=1</c>), and each
/// unsupported form it lists halts on the engine; the DOS functions it documents are exactly the ones the runtime
/// handles; the documented start state and size limits are the loader's.
/// </summary>
public sealed partial class HelpTests
{
    private static readonly EmbeddedContentCatalog Catalog = EmbeddedContentCatalog.BuiltIn();

    /// <summary>Unsupported reg-field forms of otherwise supported opcodes (docs/06-implementation/opcode-support.md §3).</summary>
    private static readonly (string Name, byte[] Code)[] UnsupportedForms =
    [
        ("F6h /1 (TEST alias)", [0xF6, 0xC8, 0x01]),
        ("F7h /1 (TEST alias)", [0xF7, 0xC8, 0x01, 0x00]),
        ("FEh /2 to /7", [0xFE, 0xD0]),
        ("FFh /7", [0xFF, 0xF8]),
    ];

    private static string Help(string id) => Catalog.ReadText(Catalog.Items.Single(i => i.Id == "help-" + id));

    private static string PagePath => Path.Combine(Repository, "src", "AssemblyStudio.Content", "Content", "Help", "unsupported-instructions.txt");

    [Fact]
    public void The_unsupported_instructions_page_is_generated_from_the_opcode_table()
    {
        string expected = RenderUnsupported();
        if (Environment.GetEnvironmentVariable("ASSEMBLY_STUDIO_WRITE_REFERENCE") == "1")
        {
            File.WriteAllText(PagePath, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        Assert.True(expected == File.ReadAllText(PagePath).ReplaceLineEndings("\n"),
            "unsupported-instructions.txt is out of date; run this test with ASSEMBLY_STUDIO_WRITE_REFERENCE=1 and commit the result.");
        Assert.Equal(expected, Help("unsupported-instructions").ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Every_listed_unsupported_form_halts_on_the_engine()
    {
        foreach ((string name, byte[] code) in UnsupportedForms)
        {
            Assert.Equal(HaltReason.UnsupportedInstruction, StepCode(code).HaltReason);
            Assert.Contains(name, Help("unsupported-instructions"), StringComparison.Ordinal);
        }

        foreach (OpcodeEntry entry in OpcodeTable.Entries.Where(e => e.Status == OpcodeStatus.Unsupported))
        {
            Assert.Equal(HaltReason.UnsupportedInstruction, StepCode([entry.Opcode, 0x00, 0x00, 0x00]).HaltReason);
        }
    }

    [Fact]
    public async Task The_documented_DOS_functions_are_exactly_the_ones_the_runtime_handles()
    {
        HashSet<byte> documented = [.. DocumentedFunction().Matches(Help("dos-services")).Select(m => byte.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
        BuildResult build = await BuildTextAsync("dos-probe", "org 100h\nmov ah, 0\nint 21h\nhlt\n");
        var handled = new HashSet<byte>();

        for (int function = 0; function < 256; function++)
        {
            byte[] image = build.Image.ToArray();
            image[1] = (byte)function; // mov ah, <function>
            ProgramRun run = Run(build with { Image = image }, InputScript.Typed("a\r", "a\r", "a\r"), budget: 1_000);
            if (!(run.End == RunEnd.Halted && run.Halt?.Code == "RTE021"))
            {
                handled.Add((byte)function);
            }
        }

        Assert.Equal([.. documented.Order()], handled.Order().ToArray());
        Assert.Equal([0x00, 0x01, 0x02, 0x07, 0x08, 0x09, 0x0A, 0x4C], handled.Order().ToArray());
    }

    /// <summary>TASK-114 (DEF-028): RTE021's next step lists the documented functions (checked against execution above),
    /// names the help page by its title, and shows no requirement ID.</summary>
    [Fact]
    public void TASK_114_the_unsupported_DOS_function_message_lists_the_documented_functions()
    {
        string next = Runtime.Interrupts.RuntimeErrorPolicy.CreateUnsupportedServiceHalt(0x21, new CpuState(0x3D00, 0, 0, 0, 0xFFFE, 0, 0, 0, 0x0800, 0x0800, 0x0800, 0x0800, 0x0100, 0x0002)).NextStep;
        string list = next[(next.IndexOf('(', StringComparison.Ordinal) + 1)..next.IndexOf(')', StringComparison.Ordinal)];
        byte[] listed = [.. list.Split(", ").Select(f => byte.Parse(f.TrimEnd('h'), NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
        byte[] documented = [.. DocumentedFunction().Matches(Help("dos-services")).Select(m => byte.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).Distinct().Order()];

        Assert.Equal(documented, listed);
        Assert.Contains("see Help, DOS services.", next, StringComparison.Ordinal);
        Assert.Equal("DOS services", Catalog.Items.Single(i => i.Id == "help-dos-services").Title);
        Assert.DoesNotMatch(@"\b(?:FR|NFR|AC)-\d{3}\b", next);
    }

    [Fact]
    public async Task INT_20h_is_documented_and_other_interrupts_are_unsupported_as_documented()
    {
        ProgramRun int20 = Run(await BuildTextAsync("int20", "org 100h\nint 20h\n"));
        ProgramRun int10 = Run(await BuildTextAsync("int10", "org 100h\nmov ah, 0Eh\nint 10h\n"));

        Assert.Equal(TerminationPath.Int20h, int20.Termination?.Path);
        Assert.Contains("INT 20h         Terminate the program", Help("dos-services"), StringComparison.Ordinal);
        Assert.Equal("RTE020", int10.Halt?.Code);
        Assert.Contains("(RTE020)", Help("dos-services"), StringComparison.Ordinal);
        Assert.Contains("no BIOS services (INT 10h, INT 16h", Help("scope"), StringComparison.Ordinal);
    }

    [Fact]
    public void The_documented_start_state_and_size_limit_are_the_loaders()
    {
        string scope = Help("scope");

        Assert.Contains($"CS = DS = ES = SS = {ComLoader.DefaultProgramSegment:X4}h, IP = {ComLoader.DefaultEntryPoint:X4}h, SP = {ComLoader.DefaultInitialSp:X4}h", scope, StringComparison.Ordinal);
        Assert.Contains($"FLAGS {ComLoader.InitialFlags:X4}h", scope, StringComparison.Ordinal);
        Assert.Contains($"up to {ComLoader.MaxComImageSize.ToString("N0", CultureInfo.InvariantCulture)} bytes", scope, StringComparison.Ordinal);
        Assert.DoesNotContain("DEF-003", scope, StringComparison.Ordinal);
        Assert.Contains($"at most {ComLoader.MaxComImageSize.ToString("N0", CultureInfo.InvariantCulture)} bytes", Help("differences"), StringComparison.Ordinal);
    }

    [Fact]
    public void Every_help_page_is_in_the_catalog()
    {
        string[] pages = [.. Directory.GetFiles(Path.GetDirectoryName(PagePath)!, "*.txt").Select(f => "Help/" + Path.GetFileName(f)).Order()];

        Assert.Equal(pages, Catalog.Items.Where(i => i.Kind == ContentKind.Help).Select(i => i.File).Order());
    }

    private static StepResult StepCode(byte[] code)
    {
        var memory = new Runtime.Memory.RealModeMemory();
        for (int i = 0; i < code.Length; i++)
        {
            memory.Write8(0x0800, (ushort)(0x100 + i), code[i]);
        }

        var cpu = new Cpu8086(memory, null);
        cpu.SetState(new CpuState(0, 0, 0, 0, 0xFFFE, 0, 0, 0, 0x0800, 0x0800, 0x0800, 0x0800, 0x0100, 0x0002));
        return cpu.StepInstruction();
    }

    /// <summary>The page: unsupported opcodes by category from the table, then the unsupported forms.</summary>
    private static string RenderUnsupported()
    {
        var text = new StringBuilder()
            .Append("Unsupported instructions\n")
            .Append("Generated from the engine's opcode table. Running one of these stops the program with \"unsupported instruction\" before the\n")
            .Append("instruction changes anything. Every other 8086 instruction is supported; the Reference section explains each one.\n");
        (OpcodeCategory Category, string Title)[] groups =
        [
            (OpcodeCategory.PortIo, "Port input and output (no devices are emulated)"),
            (OpcodeCategory.CoprocessorEsc, "8087 coprocessor (ESC)"),
            (OpcodeCategory.UndocumentedOrAlias, "80186 and later instructions, and undocumented aliases"),
        ];
        OpcodeEntry[] unsupported = [.. OpcodeTable.Entries.Where(e => e.Status == OpcodeStatus.Unsupported)];
        Assert.All(unsupported, e => Assert.Contains(e.Category, groups.Select(g => g.Category))); // a new category needs a heading
        foreach ((OpcodeCategory category, string title) in groups)
        {
            text.Append('\n').Append(title).Append('\n');
            foreach (OpcodeEntry entry in unsupported.Where(e => e.Category == category))
            {
                text.Append("  ").Append(entry.Opcode.ToString("X2", CultureInfo.InvariantCulture)).Append("h  ").Append(entry.Mnemonic).Append('\n');
            }
        }

        text.Append("\nUnsupported forms of supported opcodes (the reg field of the ModR/M byte)\n");
        foreach ((string name, _) in UnsupportedForms)
        {
            text.Append("  ").Append(name).Append('\n');
        }

        return text.ToString();
    }

    [GeneratedRegex(@"^  INT 21h AH=([0-9A-F]{2})h", RegexOptions.Multiline)]
    private static partial Regex DocumentedFunction();
}
