using System.Diagnostics;
using Poc.Cpu;

// POC-001 measurements (exit criteria 2 and 3). Run in Release: dotnet run -c Release --project Poc.Cpu.Bench
// Loop under test: ADD AX,1 / CMP AX,BX / JNE back (all in the POC subset).
byte[] loop = [0x05, 0x01, 0x00, 0x3B, 0xC3, 0x75, 0xF9];

// Evidence mode: per-file SingleStepTests counts (dotnet run -c Release --project Poc.Cpu.Bench -- --sst-report <testdata>)
if (args.Length == 2 && args[0] == "--sst-report")
{
    int files = 0, total = 0, passed = 0;
    foreach (string path in Directory.GetFiles(args[1], "*.json.gz").Order())
    {
        var r = SingleStepTests.Run(args[1], Path.GetFileName(path));
        files++; total += r.Total; passed += r.Passed;
        string regs = r.RegFieldCounts.Count == 0 ? "" : "  modrm.reg: " + string.Join(", ", r.RegFieldCounts.OrderBy(kv => kv.Key).Select(kv => $"/{kv.Key}={kv.Value}"));
        Console.WriteLine($"{r.File,-14} {r.Passed,5}/{r.Total,-5}{regs}");
        foreach (string f in r.Failures) Console.WriteLine($"    {f}");
    }
    Console.WriteLine($"TOTAL files={files} cases={total} passed={passed} failed={total - passed}");
    return;
}

static Cpu8086 NewCpu(byte[] code)
{
    var cpu = new Cpu8086();
    cpu.Segs[Cpu8086.CS] = cpu.Segs[Cpu8086.DS] = cpu.Segs[Cpu8086.ES] = cpu.Segs[Cpu8086.SS] = 0x1000;
    cpu.IP = 0x0100;
    cpu.Regs[Cpu8086.SP] = 0xFFFE;
    cpu.Regs[Cpu8086.BX] = 0xFFFF;   // the loop never exits within the measured counts
    cpu.Flags = 0xF002;
    code.CopyTo(cpu.Memory, Cpu8086.Phys(0x1000, 0x0100));
    return cpu;
}

Console.WriteLine($".NET {Environment.Version}, {Environment.ProcessorCount} logical CPUs, {(Debugger.IsAttached ? "debugger" : "no debugger")}");

// Warm-up (JIT tiering).
var warm = NewCpu(loop);
for (int i = 0; i < 2_000_000; i++) { warm.Step(); if (warm.Regs[Cpu8086.AX] == 0xFFF0) warm.Regs[Cpu8086.AX] = 0; }

// 1) Step + register snapshot latency, per step, as the debugger's Step command would do.
{
    const int n = 200_000;
    var cpu = NewCpu(loop);
    var samples = new double[n];
    CpuRegisters snap = default;
    for (int i = 0; i < n; i++)
    {
        long t0 = Stopwatch.GetTimestamp();
        cpu.Step();
        snap = cpu.Snapshot();
        samples[i] = Stopwatch.GetElapsedTime(t0).TotalMicroseconds;
        if (cpu.Regs[Cpu8086.AX] == 0xFFF0) cpu.Regs[Cpu8086.AX] = 0;
    }
    Array.Sort(samples);
    Console.WriteLine($"step+register snapshot: mean {samples.Average():F3} us, p50 {samples[n / 2]:F3} us, p99 {samples[(int)(n * 0.99)]:F3} us, max {samples[^1]:F1} us (last IP {snap.IP:X4})");
}

// 2) Worst-case snapshot: full 1 MiB memory copy per step (upper bound for history/snapshots).
{
    const int n = 2_000;
    var cpu = NewCpu(loop);
    var copy = new byte[Cpu8086.MemorySize];
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < n; i++) { cpu.Step(); Buffer.BlockCopy(cpu.Memory, 0, copy, 0, copy.Length); }
    Console.WriteLine($"step+1 MiB memory copy: mean {sw.Elapsed.TotalMicroseconds / n:F1} us");
}

// 3) Free-run throughput.
foreach (int count in new[] { 50_000_000, 50_000_000, 50_000_000 })
{
    var cpu = NewCpu(loop);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < count; i++)
    {
        cpu.Step();
        if (cpu.Regs[Cpu8086.AX] == 0xFFF0) cpu.Regs[Cpu8086.AX] = 0;
    }
    sw.Stop();
    Console.WriteLine($"free run: {count:N0} instructions in {sw.Elapsed.TotalMilliseconds:N0} ms = {count / sw.Elapsed.TotalSeconds / 1e6:F2} M instructions/s");
}
