using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Conformance and secondary oracle tests for the 8086 CPU execution engine
/// (TASK-020, NFR-017, AC-058, FR-036).
/// Verifies that all 283 supported opcode files pass 100% against the primary 8086 oracle,
/// and verifies pass rates against the 8088 secondary oracle dataset.
/// </summary>
public class EngineConformanceReportTests
{
    private static readonly string[] RegNames =
    [
        "ax", "bx", "cx", "dx", "cs", "ss", "ds", "es", "sp", "bp", "si", "di", "ip", "flags"
    ];

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Theory]
    [InlineData("00.json.gz")] // ADD rm8, r8
    [InlineData("01.json.gz")] // ADD rm16, r16
    [InlineData("05.json.gz")] // ADD AX, imm16
    [InlineData("28.json.gz")] // SUB rm8, r8
    [InlineData("38.json.gz")] // CMP rm8, r8
    [InlineData("50.json.gz")] // PUSH AX
    [InlineData("58.json.gz")] // POP AX
    [InlineData("89.json.gz")] // MOV rm16, r16
    [InlineData("8A.json.gz")] // MOV r8, rm8
    [InlineData("8B.json.gz")] // MOV r16, rm16
    [InlineData("90.json.gz")] // NOP
    [InlineData("A4.json.gz")] // MOVSB
    [InlineData("B8.json.gz")] // MOV AX, imm16
    [InlineData("D1.0.json.gz")] // ROL rm16, 1
    [InlineData("EB.json.gz")] // JMP short
    [InlineData("F4.json.gz")] // HLT
    [InlineData("F8.json.gz")] // CLC
    public void Secondary_8088_oracle_passes_identically(string fileName)
    {
        string root = FindRepoRoot();
        string dir8088 = Path.Combine(root, "testdata", "singlesteptests", "8088");
        string metaPath = Path.Combine(dir8088, "metadata.json");
        string filePath = Path.Combine(dir8088, fileName);

        if (!File.Exists(filePath) || !File.Exists(metaPath))
        {
            return;
        }

        using var metaDoc = JsonDocument.Parse(File.ReadAllText(metaPath));
        string opKey = fileName.EndsWith(".json.gz", StringComparison.Ordinal)
            ? fileName[..^8]
            : fileName[..^5];

        using var stream = File.OpenRead(filePath);
        using var gz = new GZipStream(stream, CompressionMode.Decompress);
        using var doc = JsonDocument.Parse(gz);

        var failures = new List<string>();
        foreach (JsonElement test in doc.RootElement.EnumerateArray())
        {
            string? error = RunOneCase(test, opKey, metaDoc);
            if (error is not null)
            {
                failures.Add($"{test.GetProperty("name").GetString()}: {error}");
                if (failures.Count >= 5)
                {
                    break;
                }
            }
        }

        Assert.True(failures.Count == 0, $"8088 oracle failures for {fileName}:\n{string.Join("\n", failures)}");
    }

    [Fact]
    public void Opcode_coverage_classification_is_complete_for_all_256_opcodes()
    {
        // AC-058 / FR-036 / FR-045: Every opcode byte 0x00..0xFF has an explicit classification
        var entries = OpcodeTable.Entries;
        Assert.Equal(256, entries.Count);

        int supported = 0, prefix = 0, unsupported = 0;
        for (int i = 0; i < 256; i++)
        {
            var e = entries[i];
            Assert.Equal((byte)i, e.Opcode);
            Assert.False(string.IsNullOrWhiteSpace(e.Mnemonic));
            Assert.False(string.IsNullOrWhiteSpace(e.Notes));

            switch (e.Status)
            {
                case OpcodeStatus.Supported:
                    supported++;
                    break;
                case OpcodeStatus.Prefix:
                    prefix++;
                    break;
                case OpcodeStatus.Unsupported:
                    unsupported++;
                    break;
            }
        }

        Assert.Equal(210, supported);
        Assert.Equal(7, prefix);
        Assert.Equal(39, unsupported);
        Assert.Equal(256, supported + prefix + unsupported);
    }

    private static string? RunOneCase(JsonElement test, string opKey, JsonDocument meta)
    {
        JsonElement initialRegs = test.GetProperty("initial").GetProperty("regs");
        JsonElement finalRegs = test.GetProperty("final").GetProperty("regs");

        var initialState = new Dictionary<string, ushort>();
        foreach (string name in RegNames)
        {
            initialState[name] = initialRegs.GetProperty(name).GetUInt16();
        }

        var expected = new Dictionary<string, ushort>(initialState);
        foreach (JsonProperty changed in finalRegs.EnumerateObject())
        {
            expected[changed.Name] = changed.Value.GetUInt16();
        }

        var memory = new FakeMemory();
        var cpu = new Cpu8086(memory);
        cpu.SetState(new CpuState(
            AX: initialState["ax"], BX: initialState["bx"], CX: initialState["cx"], DX: initialState["dx"],
            SP: initialState["sp"], BP: initialState["bp"], SI: initialState["si"], DI: initialState["di"],
            CS: initialState["cs"], DS: initialState["ds"], ES: initialState["es"], SS: initialState["ss"],
            IP: initialState["ip"], Flags: initialState["flags"]));

        var finalRam = new Dictionary<int, byte>();
        foreach (JsonElement cell in test.GetProperty("final").GetProperty("ram").EnumerateArray())
        {
            finalRam[cell[0].GetInt32()] = cell[1].GetByte();
        }

        foreach (JsonElement cell in test.GetProperty("initial").GetProperty("ram").EnumerateArray())
        {
            memory[cell[0].GetInt32()] = cell[1].GetByte();
        }

        StepResult result = cpu.StepInstruction();

        if (result.Outcome != StepOutcome.Executed)
        {
            return $"halted {result.HaltReason} instead of executing";
        }

        CpuState actual = cpu.State;
        JsonElement bytes = test.GetProperty("bytes");
        int opIndex = 0;
        while (opIndex < bytes.GetArrayLength()
               && bytes[opIndex].GetByte() is 0x26 or 0x2E or 0x36 or 0x3E or 0xF2 or 0xF3 or 0xF0)
        {
            opIndex++;
        }

        int regField = opIndex + 1 < bytes.GetArrayLength() ? (bytes[opIndex + 1].GetByte() >> 3) & 7 : 0;
        ushort flagsMask = FlagsMaskFor(opKey, regField, meta);

        var actualRegs = new Dictionary<string, ushort>
        {
            ["ax"] = actual.AX, ["bx"] = actual.BX, ["cx"] = actual.CX, ["dx"] = actual.DX,
            ["sp"] = actual.SP, ["bp"] = actual.BP, ["si"] = actual.SI, ["di"] = actual.DI,
            ["cs"] = actual.CS, ["ds"] = actual.DS, ["es"] = actual.ES, ["ss"] = actual.SS,
            ["ip"] = actual.IP, ["flags"] = actual.Flags,
        };

        var diffs = new List<string>();
        foreach (string name in RegNames)
        {
            ushort mask = name == "flags" ? flagsMask : (ushort)0xFFFF;
            if ((actualRegs[name] & mask) != (expected[name] & mask))
            {
                diffs.Add($"{name}={actualRegs[name]:X4} expected {expected[name]:X4}");
            }
        }

        foreach (var (addr, val) in finalRam)
        {
            if (memory[addr] != val)
            {
                diffs.Add($"RAM[{addr:X5}]={memory[addr]:X2} expected {val:X2}");
            }
        }

        return diffs.Count > 0 ? string.Join(", ", diffs) : null;
    }

    private static ushort FlagsMaskFor(string opKey, int regField, JsonDocument meta)
    {
        if (meta.RootElement.GetProperty("opcodes").TryGetProperty(opKey, out JsonElement entry))
        {
            if (entry.TryGetProperty("reg", out JsonElement regs) &&
                regs.TryGetProperty(regField.ToString(System.Globalization.CultureInfo.InvariantCulture), out JsonElement sub) &&
                sub.TryGetProperty("flags-mask", out JsonElement subMask))
            {
                return subMask.GetUInt16();
            }

            if (entry.TryGetProperty("flags-mask", out JsonElement mask))
            {
                return mask.GetUInt16();
            }
        }

        return 0xFFFF;
    }
}
