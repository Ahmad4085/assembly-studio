using System.IO.Compression;
using System.Text.Json;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// SingleStepTests 8086 v1 oracle (https://github.com/SingleStepTests/8086, MIT; fetched and
/// sha256-verified into testdata/, see build/). One JSON file per opcode: for every case, load the
/// initial registers and RAM, execute exactly one instruction on <see cref="Cpu8086"/>, then
/// compare all 14 registers (undefined flag bits masked via metadata.json) and every RAM byte the
/// case lists in its final state, and fail on any write the case does not expect (TASK-012 test
/// requirements: 100% for every listed opcode file).
/// </summary>
public class SingleStepOracleTests
{
    /// <summary>The opcode files in TASK-012 scope: MOV, XCHG, LEA/LDS/LES, XLAT, LAHF/SAHF,
    /// PUSH/POP (reg, seg, rm), PUSH/POP rm via FF/6, PUSHF/POPF; TASK-013 scope: ADD/ADC/
    /// SUB/SBB/CMP (rm/r, acc-imm, groups 80-83 all /reg), INC/DEC (r16 and rm), NEG F6/F7 /3;
    /// TASK-014 scope: OR/AND/XOR families (rm/r + acc-imm), TEST (84/85, A8/A9, F6/F7 /0),
    /// NOT (F6/F7 /2), shifts/rotates D0-D3 all /reg incl. the undocumented /6 SETMO; TASK-015 scope:
    /// MUL/IMUL/DIV/IDIV (F6/F7 /4-/7), AAA/AAS/DAA/DAS, AAM/AAD, CBW/CWD.</summary>
    public static TheoryData<string> OracleFiles { get; } = new(
    [
        // TASK-012: data transfer
        "06.json.gz", "07.json.gz", "0E.json.gz", "16.json.gz", "17.json.gz", "1E.json.gz", "1F.json.gz",
        "50.json.gz", "51.json.gz", "52.json.gz", "53.json.gz", "54.json.gz", "55.json.gz", "56.json.gz",
        "57.json.gz", "58.json.gz", "59.json.gz", "5A.json.gz", "5B.json.gz", "5C.json.gz", "5D.json.gz",
        "5E.json.gz", "5F.json.gz",
        "86.json.gz", "87.json.gz", "88.json.gz", "89.json.gz", "8A.json.gz", "8B.json.gz", "8C.json.gz",
        "8D.json.gz", "8E.json.gz", "8F.json.gz",
        "90.json.gz", "91.json.gz", "92.json.gz", "93.json.gz", "94.json.gz", "95.json.gz", "96.json.gz", "97.json.gz",
        "9C.json.gz", "9D.json.gz", "9E.json.gz", "9F.json.gz",
        "A0.json.gz", "A1.json.gz", "A2.json.gz", "A3.json.gz",
        "B0.json.gz", "B1.json.gz", "B2.json.gz", "B3.json.gz", "B4.json.gz", "B5.json.gz", "B6.json.gz",
        "B7.json.gz", "B8.json.gz", "B9.json.gz", "BA.json.gz", "BB.json.gz", "BC.json.gz", "BD.json.gz",
        "BE.json.gz", "BF.json.gz",
        "C4.json.gz", "C5.json.gz", "C6.json.gz", "C7.json.gz", "D7.json.gz",
        "FF.6.json.gz",
        // TASK-013: arithmetic (ADD/ADC/SUB/SBB/CMP families, INC/DEC, NEG)
        "00.json.gz", "01.json.gz", "02.json.gz", "03.json.gz", "04.json.gz", "05.json.gz",
        "10.json.gz", "11.json.gz", "12.json.gz", "13.json.gz", "14.json.gz", "15.json.gz",
        "18.json.gz", "19.json.gz", "1A.json.gz", "1B.json.gz", "1C.json.gz", "1D.json.gz",
        "28.json.gz", "29.json.gz", "2A.json.gz", "2B.json.gz", "2C.json.gz", "2D.json.gz",
        "38.json.gz", "39.json.gz", "3A.json.gz", "3B.json.gz", "3C.json.gz", "3D.json.gz",
        "40.json.gz", "41.json.gz", "42.json.gz", "43.json.gz", "44.json.gz", "45.json.gz", "46.json.gz", "47.json.gz",
        "48.json.gz", "49.json.gz", "4A.json.gz", "4B.json.gz", "4C.json.gz", "4D.json.gz", "4E.json.gz", "4F.json.gz",
        "80.0.json.gz", "80.1.json.gz", "80.2.json.gz", "80.3.json.gz", "80.4.json.gz", "80.5.json.gz", "80.6.json.gz", "80.7.json.gz",
        "81.0.json.gz", "81.1.json.gz", "81.2.json.gz", "81.3.json.gz", "81.4.json.gz", "81.5.json.gz", "81.6.json.gz", "81.7.json.gz",
        "82.0.json.gz", "82.1.json.gz", "82.2.json.gz", "82.3.json.gz", "82.4.json.gz", "82.5.json.gz", "82.6.json.gz", "82.7.json.gz",
        "83.0.json.gz", "83.1.json.gz", "83.2.json.gz", "83.3.json.gz", "83.4.json.gz", "83.5.json.gz", "83.6.json.gz", "83.7.json.gz",
        "F6.3.json.gz", "F7.3.json.gz", "FE.0.json.gz", "FE.1.json.gz", "FF.0.json.gz", "FF.1.json.gz",
        // TASK-014: logic (OR/AND/XOR families, TEST, NOT), shifts/rotates D0-D3 all /reg (incl. /6 SETMO)
        "08.json.gz", "09.json.gz", "0A.json.gz", "0B.json.gz", "0C.json.gz", "0D.json.gz",
        "20.json.gz", "21.json.gz", "22.json.gz", "23.json.gz", "24.json.gz", "25.json.gz",
        "30.json.gz", "31.json.gz", "32.json.gz", "33.json.gz", "34.json.gz", "35.json.gz",
        "84.json.gz", "85.json.gz", "A8.json.gz", "A9.json.gz",
        "F6.0.json.gz", "F6.2.json.gz", "F7.0.json.gz", "F7.2.json.gz",
        "D0.0.json.gz", "D0.1.json.gz", "D0.2.json.gz", "D0.3.json.gz", "D0.4.json.gz", "D0.5.json.gz", "D0.6.json.gz", "D0.7.json.gz",
        "D1.0.json.gz", "D1.1.json.gz", "D1.2.json.gz", "D1.3.json.gz", "D1.4.json.gz", "D1.5.json.gz", "D1.6.json.gz", "D1.7.json.gz",
        "D2.0.json.gz", "D2.1.json.gz", "D2.2.json.gz", "D2.3.json.gz", "D2.4.json.gz", "D2.5.json.gz", "D2.6.json.gz", "D2.7.json.gz",
        "D3.0.json.gz", "D3.1.json.gz", "D3.2.json.gz", "D3.3.json.gz", "D3.4.json.gz", "D3.5.json.gz", "D3.6.json.gz", "D3.7.json.gz",
        // TASK-015: MUL/IMUL/DIV/IDIV (F6/F7 /4-/7, incl. divide errors vectoring through INT 0), BCD adjust, CBW/CWD
        "F6.4.json.gz", "F6.5.json.gz", "F6.6.json.gz", "F6.7.json.gz", "F7.4.json.gz", "F7.5.json.gz", "F7.6.json.gz", "F7.7.json.gz",
        "27.json.gz", "2F.json.gz", "37.json.gz", "3F.json.gz", "D4.json.gz", "D5.json.gz", "98.json.gz", "99.json.gz",
        // TASK-016: control transfer and interrupts (jumps, calls, returns, loops, interrupts)
        "70.json.gz", "71.json.gz", "72.json.gz", "73.json.gz", "74.json.gz", "75.json.gz", "76.json.gz", "77.json.gz",
        "78.json.gz", "79.json.gz", "7A.json.gz", "7B.json.gz", "7C.json.gz", "7D.json.gz", "7E.json.gz", "7F.json.gz",
        "E0.json.gz", "E1.json.gz", "E2.json.gz", "E3.json.gz",
        "E8.json.gz", "E9.json.gz", "EA.json.gz", "EB.json.gz",
        "9A.json.gz",
        "C2.json.gz", "C3.json.gz", "CA.json.gz", "CB.json.gz",
        "FF.2.json.gz", "FF.3.json.gz", "FF.4.json.gz", "FF.5.json.gz",
        "CC.json.gz", "CD.json.gz", "CE.json.gz", "CF.json.gz",
    ]);

    private static readonly string[] RegNames = ["ax", "bx", "cx", "dx", "sp", "bp", "si", "di", "cs", "ds", "es", "ss", "ip", "flags"];

    /// <summary>Finds the repository's testdata directory from the test run location.</summary>
    private static string TestDataDir()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "testdata", "singlesteptests", "8086")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "testdata", "singlesteptests", "8086");
    }

    private static (JsonDocument Meta, string Dir) LoadMetaOnce()
    {
        string dir = TestDataDir();
        return (JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "metadata.json"))), dir);
    }

    private static readonly Lazy<(JsonDocument, string)> Meta = new(LoadMetaOnce);

    /// <summary>
    /// Flags mask for one case: per-reg mask when metadata defines one for this opcode's ModR/M reg
    /// field (e.g. FF.6), else the opcode-level mask, else all bits compared.
    /// </summary>
    private static ushort FlagsMaskFor(string opKey, int regField)
    {
        JsonElement entry = Meta.Value.Item1.RootElement.GetProperty("opcodes").GetProperty(opKey);
        if (entry.TryGetProperty("reg", out JsonElement regs) && regs.TryGetProperty(regField.ToString(System.Globalization.CultureInfo.InvariantCulture), out JsonElement sub)
            && sub.TryGetProperty("flags-mask", out JsonElement subMask))
        {
            return subMask.GetUInt16();
        }

        return entry.TryGetProperty("flags-mask", out JsonElement mask) ? mask.GetUInt16() : (ushort)0xFFFF;
    }

    [Theory]
    [MemberData(nameof(OracleFiles))]
    public void Oracle_file_passes_100_percent(string fileName)
    {
        string dir = Meta.Value.Item2;
        string opKey = fileName.Split('.')[0];

        using var gz = new GZipStream(File.OpenRead(Path.Combine(dir, fileName)), CompressionMode.Decompress);
        using JsonDocument doc = JsonDocument.Parse(gz);

        var failures = new List<string>();

        foreach (JsonElement test in doc.RootElement.EnumerateArray())
        {
            string? error = RunOne(test, opKey);
            if (error is not null)
            {
                failures.Add($"#{test.GetProperty("test_num").GetInt32()} '{test.GetProperty("name").GetString()}': {error}");
            }
        }

        Assert.True(failures.Count == 0, $"{fileName}: {failures.Count} failing cases. First: {string.Join(" | ", failures.Take(5))}");
    }

    private static string? RunOne(JsonElement test, string opKey)
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

        var diffs = new List<string>();
        if (result.Outcome != StepOutcome.Executed)
        {
            diffs.Add($"halted {result.HaltReason} instead of executing");
        }
        else
        {
            CpuState actual = cpu.State;
            JsonElement bytes = test.GetProperty("bytes");
            int opIndex = 0;
            while (opIndex < bytes.GetArrayLength()
                   && bytes[opIndex].GetByte() is 0x26 or 0x2E or 0x36 or 0x3E or 0xF2 or 0xF3 or 0xF0)
            {
                opIndex++;
            }

            int regField = opIndex + 1 < bytes.GetArrayLength() ? (bytes[opIndex + 1].GetByte() >> 3) & 7 : 0;
            ushort flagsMask = FlagsMaskFor(opKey, regField);
            ushort ax = actual.AX, cx = actual.CX, dx = actual.DX, bx = actual.BX;
            var actualRegs = new Dictionary<string, ushort>
            {
                ["ax"] = ax, ["bx"] = bx, ["cx"] = cx, ["dx"] = dx,
                ["sp"] = actual.SP, ["bp"] = actual.BP, ["si"] = actual.SI, ["di"] = actual.DI,
                ["cs"] = actual.CS, ["ds"] = actual.DS, ["es"] = actual.ES, ["ss"] = actual.SS,
                ["ip"] = actual.IP, ["flags"] = actual.Flags,
            };
            foreach (string name in RegNames)
            {
                ushort mask = name == "flags" ? flagsMask : (ushort)0xFFFF;
                if ((actualRegs[name] & mask) != (expected[name] & mask))
                {
                    diffs.Add($"{name}={actualRegs[name]:X4} expected {expected[name]:X4}");
                }
            }

            foreach (var (addr, value) in finalRam)
            {
                if (memory[addr] != value)
                {
                    diffs.Add($"[{addr:X5}]={memory[addr]:X2} expected {value:X2}");
                }
            }

            foreach (MemoryChange change in memory.TakeJournal())
            {
                if (!finalRam.ContainsKey(change.PhysicalAddress))
                {
                    diffs.Add($"unexpected write to {change.PhysicalAddress:X5}");
                }
            }
        }

        // Memory is a fresh FakeMemory per case, so cases run deterministically with no scrubbing.
        if (diffs.Count > 0)
        {
            return string.Join("; ", diffs);
        }

        return null;
    }
}
