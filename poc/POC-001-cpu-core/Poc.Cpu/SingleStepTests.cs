using System.IO.Compression;
using System.Text.Json;

namespace Poc.Cpu;

/// <summary>Result of running one SingleStepTests file against <see cref="Cpu8086"/>.</summary>
public sealed record FileResult(string File, int Total, int Passed, IReadOnlyList<string> Failures, IReadOnlyDictionary<int, int> RegFieldCounts);

/// <summary>
/// Runner for SingleStepTests 8086 v1 JSON (https://github.com/SingleStepTests/8086, MIT).
/// Each test: load initial registers and RAM, execute one instruction, compare all registers
/// (undefined flags masked via metadata.json) and every RAM byte listed in the final state.
/// </summary>
public static class SingleStepTests
{
    private static readonly string[] RegNames = ["ax", "bx", "cx", "dx", "sp", "bp", "si", "di", "cs", "ds", "es", "ss", "ip", "flags"];

    public static ushort FlagsMask(string testDataDir, string fileName)
    {
        using var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(testDataDir, "metadata.json")));
        string[] parts = fileName.Split('.');           // "80.5.json.gz" or "05.json.gz"
        if (!meta.RootElement.GetProperty("opcodes").TryGetProperty(parts[0], out var entry)) return 0xFFFF;
        if (parts.Length == 4 && entry.TryGetProperty("reg", out var regs) && regs.TryGetProperty(parts[1], out var sub))
            entry = sub;
        return entry.TryGetProperty("flags-mask", out var mask) ? mask.GetUInt16() : (ushort)0xFFFF;
    }

    public static FileResult Run(string testDataDir, string fileName, int maxFailuresReported = 10)
    {
        ushort flagsMask = FlagsMask(testDataDir, fileName);
        using var gz = new GZipStream(File.OpenRead(Path.Combine(testDataDir, fileName)), CompressionMode.Decompress);
        using var doc = JsonDocument.Parse(gz);

        var failures = new List<string>();
        var regFields = new Dictionary<int, int>();
        int total = 0, passed = 0;
        var cpu = new Cpu8086 { WriteLog = [] };

        foreach (var test in doc.RootElement.EnumerateArray())
        {
            total++;
            string? error = RunOne(cpu, test, flagsMask, regFields);
            if (error is null) passed++;
            else if (failures.Count < maxFailuresReported)
                failures.Add($"#{test.GetProperty("test_num").GetInt32()} '{test.GetProperty("name").GetString()}': {error}");
        }
        return new FileResult(fileName, total, passed, failures, regFields);
    }

    private static string? RunOne(Cpu8086 cpu, JsonElement test, ushort flagsMask, Dictionary<int, int> regFields)
    {
        var initial = test.GetProperty("initial");
        var final = test.GetProperty("final");

        var expected = new Dictionary<string, ushort>();
        foreach (string name in RegNames) expected[name] = initial.GetProperty("regs").GetProperty(name).GetUInt16();
        SetRegs(cpu, expected);
        foreach (var cell in final.GetProperty("regs").EnumerateObject()) expected[cell.Name] = cell.Value.GetUInt16();

        // Clear only the bytes this test touches, keeping the run deterministic without a 1 MiB reset per test.
        var finalRam = new Dictionary<int, byte>();
        foreach (var cell in final.GetProperty("ram").EnumerateArray()) finalRam[cell[0].GetInt32()] = cell[1].GetByte();
        foreach (var cell in initial.GetProperty("ram").EnumerateArray()) cpu.Memory[cell[0].GetInt32()] = cell[1].GetByte();

        byte[] bytes = test.GetProperty("bytes").EnumerateArray().Select(b => b.GetByte()).ToArray();
        int opIndex = Array.FindIndex(bytes, b => b is not (0x26 or 0x2E or 0x36 or 0x3E or 0xF2 or 0xF3 or 0xF0));
        if (opIndex >= 0 && opIndex + 1 < bytes.Length && bytes[opIndex] is 0xC6 or 0xC7)
        {
            int reg = (bytes[opIndex + 1] >> 3) & 7;
            regFields[reg] = regFields.GetValueOrDefault(reg) + 1;
        }

        cpu.WriteLog!.Clear();
        string? error = null;
        try { cpu.Step(); }
        catch (UnsupportedInstructionException ex) { error = ex.Message; }

        if (error is null)
        {
            var actual = GetRegs(cpu);
            var diffs = new List<string>();
            foreach (string name in RegNames)
            {
                ushort mask = name == "flags" ? flagsMask : (ushort)0xFFFF;
                if ((actual[name] & mask) != (expected[name] & mask))
                    diffs.Add($"{name}={actual[name]:X4} expected {expected[name]:X4}");
            }
            foreach (var (addr, value) in finalRam)
                if (cpu.Memory[addr] != value) diffs.Add($"[{addr:X5}]={cpu.Memory[addr]:X2} expected {value:X2}");
            foreach (int addr in cpu.WriteLog!)
                if (!finalRam.ContainsKey(addr)) diffs.Add($"unexpected write to {addr:X5}");
            if (diffs.Count > 0) error = string.Join("; ", diffs);
        }

        // Scrub the touched RAM so the next test starts from zeroed memory there.
        foreach (int addr in finalRam.Keys) cpu.Memory[addr] = 0;
        foreach (var cell in initial.GetProperty("ram").EnumerateArray()) cpu.Memory[cell[0].GetInt32()] = 0;
        foreach (int addr in cpu.WriteLog!) cpu.Memory[addr] = 0;
        return error;
    }

    private static void SetRegs(Cpu8086 cpu, Dictionary<string, ushort> r)
    {
        cpu.Regs[Cpu8086.AX] = r["ax"]; cpu.Regs[Cpu8086.BX] = r["bx"]; cpu.Regs[Cpu8086.CX] = r["cx"]; cpu.Regs[Cpu8086.DX] = r["dx"];
        cpu.Regs[Cpu8086.SP] = r["sp"]; cpu.Regs[Cpu8086.BP] = r["bp"]; cpu.Regs[Cpu8086.SI] = r["si"]; cpu.Regs[Cpu8086.DI] = r["di"];
        cpu.Segs[Cpu8086.CS] = r["cs"]; cpu.Segs[Cpu8086.DS] = r["ds"]; cpu.Segs[Cpu8086.ES] = r["es"]; cpu.Segs[Cpu8086.SS] = r["ss"];
        cpu.IP = r["ip"]; cpu.Flags = r["flags"];
    }

    private static Dictionary<string, ushort> GetRegs(Cpu8086 cpu) => new()
    {
        ["ax"] = cpu.Regs[Cpu8086.AX], ["bx"] = cpu.Regs[Cpu8086.BX], ["cx"] = cpu.Regs[Cpu8086.CX], ["dx"] = cpu.Regs[Cpu8086.DX],
        ["sp"] = cpu.Regs[Cpu8086.SP], ["bp"] = cpu.Regs[Cpu8086.BP], ["si"] = cpu.Regs[Cpu8086.SI], ["di"] = cpu.Regs[Cpu8086.DI],
        ["cs"] = cpu.Segs[Cpu8086.CS], ["ds"] = cpu.Segs[Cpu8086.DS], ["es"] = cpu.Segs[Cpu8086.ES], ["ss"] = cpu.Segs[Cpu8086.SS],
        ["ip"] = cpu.IP, ["flags"] = cpu.Flags,
    };
}
