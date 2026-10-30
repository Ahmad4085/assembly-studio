using System.Text;
using System.Text.Json;
using AssemblyStudio.Content;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;
using Xunit;
using static AssemblyStudio.ProgramTests.ProgramHarness;

namespace AssemblyStudio.ProgramTests;

/// <summary>
/// TASK-092: the instruction reference (FR-071) and its agreement with the engine (FR-072, AC-037).
/// <para>
/// The reference pages <c>Content/Reference/instructions-*.txt</c> are generated from the hand-written instruction data
/// (<c>src/AssemblyStudio.Content/ReferenceData/instructions.json</c>) and the engine's opcode table. The Content
/// project can't reference the engine (ADR-0002), so the pages are generated here and committed; the first test fails
/// when they are out of date. Set <c>ASSEMBLY_STUDIO_WRITE_REFERENCE=1</c> and run it to regenerate them.
/// </para>
/// <para>
/// Each instruction's flag statement is checked by running its probe instruction on the real engine from hundreds of
/// random states: every flag it says is changed must change in some run, the flags it says are undefined must be
/// exactly the ones the engine reports undefined, and no other flag may ever change.
/// </para>
/// </summary>
public sealed class ReferenceTests
{
    private const FlagBit Watched = FlagBit.OF | FlagBit.DF | FlagBit.IF | FlagBit.TF | FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF | FlagBit.CF;
    private const int Trials = 600;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Lazy<ReferenceData> Data = new(() =>
        JsonSerializer.Deserialize<ReferenceData>(File.ReadAllText(DataPath), Json) ?? throw new InvalidOperationException("No reference data."));

    private static string DataPath => Path.Combine(Repository, "src", "AssemblyStudio.Content", "ReferenceData", "instructions.json");

    private static string PagePath(string category) =>
        Path.Combine(Repository, "src", "AssemblyStudio.Content", "Content", "Reference", $"instructions-{category}.txt");

    public static TheoryData<string> Categories() => [.. Data.Value.Categories.Select(c => c.Id)];

    public static TheoryData<string> Probed() => [.. Data.Value.Instructions.Where(i => i.Probe is not null).Select(i => i.Mnemonic)];

    // ---- Generation ----

    [Theory]
    [MemberData(nameof(Categories))]
    public void The_reference_page_is_generated_from_the_instruction_data_and_the_opcode_table(string category)
    {
        string expected = ReferenceGenerator.Render(Data.Value, category);
        string path = PagePath(category);
        if (Environment.GetEnvironmentVariable("ASSEMBLY_STUDIO_WRITE_REFERENCE") == "1")
        {
            File.WriteAllText(path, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        Assert.True(File.Exists(path), $"Missing {path}; run with ASSEMBLY_STUDIO_WRITE_REFERENCE=1.");
        Assert.True(expected == File.ReadAllText(path).ReplaceLineEndings("\n"),
            $"{Path.GetFileName(path)} is out of date; run this test with ASSEMBLY_STUDIO_WRITE_REFERENCE=1 and commit the result.");

        ContentItem item = Assert.Single(EmbeddedContentCatalog.BuiltIn().Items, i => i.File == $"Reference/instructions-{category}.txt");
        Assert.Equal(ContentKind.Reference, item.Kind);
        Assert.Equal(Data.Value.Categories.Single(c => c.Id == category).Title, item.Title);
    }

    [Fact]
    public void Every_supported_opcode_and_prefix_is_documented_and_every_documented_opcode_is_supported()
    {
        var documented = new HashSet<byte>();
        foreach (InstructionData instruction in Data.Value.Instructions)
        {
            foreach (string opcode in instruction.Opcodes)
            {
                byte value = ReferenceGenerator.OpcodeByte(opcode);
                Assert.False(OpcodeTable.IsUnsupported(value), $"{instruction.Mnemonic} lists {opcode}, which the engine doesn't support.");
                documented.Add(value);
            }
        }

        byte[] missing = [.. OpcodeTable.Entries.Where(e => e.Status != OpcodeStatus.Unsupported && !documented.Contains(e.Opcode)).Select(e => e.Opcode)];
        Assert.True(missing.Length == 0, "Not documented: " + string.Join(", ", missing.Select(b => $"{b:X2}h")));
    }

    [Fact]
    public void The_instruction_data_is_complete()
    {
        string[] flagNames = [.. Enum.GetNames<FlagBit>().Where(n => n != nameof(FlagBit.None))];
        HashSet<string> categories = [.. Data.Value.Categories.Select(c => c.Id)];

        Assert.Equal(Data.Value.Instructions.Length, Data.Value.Instructions.Select(i => i.Mnemonic).Distinct().Count());
        Assert.All(Data.Value.Instructions, i =>
        {
            Assert.Contains(i.Category, categories);
            Assert.False(string.IsNullOrWhiteSpace(i.Name) || string.IsNullOrWhiteSpace(i.Operation) || string.IsNullOrWhiteSpace(i.Example), i.Mnemonic);
            Assert.NotEmpty(i.Opcodes);
            Assert.All(i.Modified.Concat(i.Undefined), f => Assert.Contains(f, flagNames));
            Assert.Empty(i.Modified.Intersect(i.Undefined));
            Assert.True(i.Probe is not null || !string.IsNullOrWhiteSpace(i.ProbeSkipped), $"{i.Mnemonic}: a probe, or the reason there is none");
        });
    }

    // ---- AC-037: flag statements against the engine ----

    [Fact]
    public async Task AC_037_the_ADD_entry_states_the_flags_the_engine_changes()
    {
        InstructionData add = Data.Value.Instructions.Single(i => i.Mnemonic == "ADD");

        Observed observed = await ObserveAsync(add);

        Assert.Equal(FlagBit.OF | FlagBit.SF | FlagBit.ZF | FlagBit.AF | FlagBit.PF | FlagBit.CF, Mask(add.Modified));
        Assert.Equal(Mask(add.Modified), observed.Changed);
        Assert.Equal(FlagBit.None, observed.Undefined);
    }

    [Theory]
    [MemberData(nameof(Probed))]
    public async Task AC_037_the_flag_statement_matches_the_engine(string mnemonic)
    {
        InstructionData instruction = Data.Value.Instructions.Single(i => i.Mnemonic == mnemonic);
        FlagBit modified = Mask(instruction.Modified);
        FlagBit undefined = Mask(instruction.Undefined);

        Observed observed = await ObserveAsync(instruction);

        Assert.True(observed.Runs >= Trials / 4, $"{mnemonic}: only {observed.Runs} runs completed normally");
        Assert.True(undefined == observed.Undefined, $"{mnemonic}: documented undefined {undefined}, the engine reports {observed.Undefined}");
        Assert.True((observed.Changed & ~(modified | undefined)) == FlagBit.None,
            $"{mnemonic}: {observed.Changed & ~(modified | undefined)} changed but the reference says it doesn't");
        Assert.True((modified & ~observed.Changed) == FlagBit.None, $"{mnemonic}: {modified & ~observed.Changed} documented as changed but never changed");
    }

    private sealed record Observed(FlagBit Changed, FlagBit Undefined, int Runs);

    /// <summary>Runs the probe from <see cref="Trials"/> random states (random registers with edge values, random flags,
    /// random memory around the pointers) and collects the flags that changed and those the engine reported undefined.
    /// Runs that left the instruction through an exception (a divide error) are skipped, except for control transfers.</summary>
    private static async Task<Observed> ObserveAsync(InstructionData instruction)
    {
        BuildResult build = await BuildTextAsync(instruction.Mnemonic, $"org 100h\n{instruction.Probe}\n");
        ProgramRun loaded = Run(build, budget: 0);
        var machine = loaded.Machine;
        CpuState start = loaded.InitialState;
        byte[] image = build.Image.ToArray();
        ushort next = (ushort)(0x100 + image.Length);
        bool transfersControl = instruction.Category == "control-transfer";
        var random = new Random(instruction.Mnemonic.Aggregate(17, (h, c) => (h * 31) + c));
        ushort[] edges = [0x0000, 0x0001, 0x0009, 0x0099, 0x007F, 0x0080, 0x00FF, 0x7FFF, 0x8000, 0xFFFF];

        ushort Value() => random.Next(4) == 0 ? edges[random.Next(edges.Length)] : (ushort)random.Next(0x10000);
        ushort Pointer() => (ushort)(0x4000 + random.Next(0x100));

        FlagBit changed = FlagBit.None, undefined = FlagBit.None;
        int runs = 0;
        for (int trial = 0; trial < Trials; trial++)
        {
            for (int i = 0; i < image.Length; i++)
            {
                machine.Memory.Write8(start.CS, (ushort)(0x100 + i), image[i]); // a REP STOSB may have overwritten it
            }

            for (ushort offset = 0x4000; offset < 0x4204; offset++)
            {
                machine.Memory.Write8(start.DS, offset, (byte)random.Next(0x100));
            }

            ushort flags = (ushort)((random.Next(0x10000) & (ushort)(Watched & ~FlagBit.TF)) | 0xF002);
            var before = start with
            {
                AX = Value(), BX = Pointer(), CX = Value(), DX = Value(), BP = Value(), SI = Pointer(), DI = Pointer(), SP = Pointer(), IP = 0x100, Flags = flags,
            };
            machine.Cpu.SetState(before);
            StepResult result = machine.Step();
            CpuState after = machine.Cpu.State;
            if (result.Outcome != StepOutcome.Executed || (!transfersControl && (after.CS != before.CS || after.IP != next)))
            {
                continue;
            }

            runs++;
            changed |= (FlagBit)((before.Flags ^ after.Flags) & (ushort)Watched);
            undefined |= result.UndefinedFlags;
        }

        return new Observed(changed & ~undefined, undefined, runs);
    }

    private static FlagBit Mask(IEnumerable<string> flags) => flags.Aggregate(FlagBit.None, (mask, f) => mask | Enum.Parse<FlagBit>(f));
}

/// <summary>The instruction reference data file.</summary>
public sealed record ReferenceData(CategoryData[] Categories, InstructionData[] Instructions);

/// <summary>A reference page.</summary>
public sealed record CategoryData(string Id, string Title);

/// <summary>One instruction's hand-written reference data.</summary>
public sealed record InstructionData(
    string Mnemonic,
    string Name,
    string Category,
    string Operation,
    string[] Modified,
    string[] Undefined,
    string Notes,
    string Example,
    string[] Opcodes,
    string? Probe,
    string? ProbeSkipped);

/// <summary>Renders the reference pages (plain text, shown as-is in the Learn sidebar).</summary>
public static class ReferenceGenerator
{
    private static readonly string[] FlagOrder = ["OF", "DF", "IF", "TF", "SF", "ZF", "AF", "PF", "CF"];

    /// <summary>The opcode byte of an opcode reference such as <c>80 /0</c>.</summary>
    public static byte OpcodeByte(string opcode) => Convert.ToByte(opcode.Split(' ')[0], 16);

    /// <summary>The page for <paramref name="category"/>.</summary>
    public static string Render(ReferenceData data, string category)
    {
        ArgumentNullException.ThrowIfNull(data);
        var text = new StringBuilder();
        text.Append(data.Categories.Single(c => c.Id == category).Title).Append('\n')
            .Append("Generated from the instruction reference data and the engine's opcode table. Each flag statement is checked against the engine by the automated tests.\n")
            .Append("Flags: OF overflow, DF direction, IF interrupt enable, TF trap, SF sign, ZF zero, AF auxiliary carry, PF parity, CF carry.\n");
        foreach (InstructionData i in data.Instructions.Where(i => i.Category == category))
        {
            text.Append('\n')
                .Append(i.Mnemonic).Append(" - ").Append(i.Name).Append('\n')
                .Append("  ").Append(i.Operation).Append('\n')
                .Append("  Example: ").Append(i.Example).Append('\n')
                .Append("  Flags changed: ").Append(Flags(i.Modified, "none")).Append('\n');
            if (i.Undefined.Length > 0)
            {
                text.Append("  Undefined afterwards: ").Append(Flags(i.Undefined, "none")).Append('\n');
            }

            text.Append(i.Modified.Length + i.Undefined.Length < FlagOrder.Length ? "  All other flags keep their values.\n" : "");
            if (!string.IsNullOrWhiteSpace(i.Notes))
            {
                text.Append("  ").Append(i.Notes).Append('\n');
            }

            text.Append("  Opcodes: ").Append(string.Join("; ", i.Opcodes.Select(Opcode))).Append('\n');
        }

        return text.ToString();
    }

    private static string Flags(IEnumerable<string> flags, string none)
    {
        string[] ordered = [.. FlagOrder.Where(flags.Contains)];
        return ordered.Length == 0 ? none : string.Join(" ", ordered);
    }

    private static string Opcode(string opcode)
    {
        string[] parts = opcode.Split(' ');
        OpcodeEntry entry = OpcodeTable.Get(OpcodeByte(opcode));
        return parts.Length > 1 ? $"{entry.Opcode:X2}h {parts[1]} ({entry.Mnemonic})" : $"{entry.Opcode:X2}h {entry.Mnemonic}";
    }
}
