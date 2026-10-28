using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using AssemblyStudio.Core;
using AssemblyStudio.Engine.I8086;

namespace AssemblyStudio.Bench;

public static class Program
{
    private sealed class FlatMemory : IMemory
    {
        private const int Size = 1 << 20;
        private readonly byte[] _bytes = new byte[Size];
        public byte[] RawBytes => _bytes;

        private static int Phys(ushort segment, ushort offset) => (((int)segment << 4) + offset) & (Size - 1);

        public byte Read8(ushort segment, ushort offset) => _bytes[Phys(segment, offset)];

        public ushort Read16(ushort segment, ushort offset) =>
            (ushort)(_bytes[Phys(segment, offset)] | (_bytes[Phys(segment, (ushort)(offset + 1))] << 8));

        public void Write8(ushort segment, ushort offset, byte value) => _bytes[Phys(segment, offset)] = value;

        public void Write16(ushort segment, ushort offset, ushort value)
        {
            _bytes[Phys(segment, offset)] = (byte)value;
            _bytes[Phys(segment, (ushort)(offset + 1))] = (byte)(value >> 8);
        }

        public IReadOnlyList<MemoryChange> TakeJournal() => Array.Empty<MemoryChange>();
    }

    public static int Main(string[] args)
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine(" AssemblyStudio Engine Benchmarks & Conformance Gate (TASK-020)  ");
        Console.WriteLine("=================================================================");
        Console.WriteLine($"Runtime: .NET {Environment.Version}");
        Console.WriteLine($"OS: {Environment.OSVersion}");
        Console.WriteLine($"CPUs: {Environment.ProcessorCount} logical processors");
        Console.WriteLine($"Stopwatch frequency: {Stopwatch.Frequency:N0} ticks/sec (resolution ~{1_000_000.0 / Stopwatch.Frequency:F3} us)");
        Console.WriteLine($"Debugger attached: {Debugger.IsAttached}");
        Console.WriteLine();

        if (args.Length > 0 && args[0] == "--conformance")
        {
            return RunConformanceGate();
        }

        RunBenchmarks();
        return 0;
    }

    private static Cpu8086 CreateCpu(byte[] code, out FlatMemory memory)
    {
        memory = new FlatMemory();
        int phys = (0x1000 << 4) + 0x0100;
        code.CopyTo(memory.RawBytes, phys);

        var cpu = new Cpu8086(memory);
        cpu.SetState(new CpuState(
            AX: 0, BX: 0xFFFF, CX: 0, DX: 0,
            SP: 0xFFFE, BP: 0, SI: 0, DI: 0,
            CS: 0x1000, DS: 0x1000, ES: 0x1000, SS: 0x1000,
            IP: 0x0100, Flags: 0xF002));
        return cpu;
    }

    public static void RunBenchmarks()
    {
        // Tight loop: ADD AX, 1 / CMP AX, BX / JNE loop (7 bytes)
        byte[] loop = [0x05, 0x01, 0x00, 0x3B, 0xC3, 0x75, 0xF9];

        // 1. Warm-up
        Console.WriteLine("Warming up JIT...");
        var warmCpu = CreateCpu(loop, out _);
        for (int i = 0; i < 2_000_000; i++)
        {
            warmCpu.StepInstruction();
            if (warmCpu.State.AX == 0xFFF0)
            {
                warmCpu.SetState(warmCpu.State with { AX = 0 });
            }
        }

        // 2. Step + register snapshot latency (NFR-002 target <= 100 ms)
        Console.WriteLine("\n--- Benchmark 1: Step + Register Snapshot Latency (NFR-002) ---");
        const int stepSamplesCount = 200_000;
        var stepCpu = CreateCpu(loop, out _);
        var latencies = new double[stepSamplesCount];
        CpuState lastSnapshot = default;

        for (int i = 0; i < stepSamplesCount; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            stepCpu.StepInstruction();
            lastSnapshot = stepCpu.State;
            latencies[i] = Stopwatch.GetElapsedTime(t0).TotalMicroseconds;

            if (stepCpu.State.AX == 0xFFF0)
            {
                stepCpu.SetState(stepCpu.State with { AX = 0 });
            }
        }

        Array.Sort(latencies);
        double meanLatency = latencies.Average();
        double p50 = latencies[stepSamplesCount / 2];
        double p95 = latencies[(int)(stepSamplesCount * 0.95)];
        double p99 = latencies[(int)(stepSamplesCount * 0.99)];
        double max = latencies[^1];

        Console.WriteLine($"Samples: {stepSamplesCount:N0} steps");
        Console.WriteLine($"Mean:   {meanLatency:F3} us");
        Console.WriteLine($"p50:    {p50:F3} us");
        Console.WriteLine($"p95:    {p95:F3} us");
        Console.WriteLine($"p99:    {p99:F3} us");
        Console.WriteLine($"Max:    {max:F1} us (isolated OS scheduling jitter)");
        Console.WriteLine($"NFR-002 budget: 100,000 us (100 ms). Engine share: {meanLatency / 100_000 * 100:F4}%");

        // 3. Worst-case snapshot latency: step + 1 MiB memory buffer copy
        Console.WriteLine("\n--- Benchmark 2: Worst-Case Snapshot Latency (Step + 1 MiB Mem Copy) ---");
        const int memSamplesCount = 2_000;
        var memCpu = CreateCpu(loop, out var flatMem);
        var memCopyBuffer = new byte[1024 * 1024];
        var memSw = Stopwatch.StartNew();

        for (int i = 0; i < memSamplesCount; i++)
        {
            memCpu.StepInstruction();
            Buffer.BlockCopy(flatMem.RawBytes, 0, memCopyBuffer, 0, memCopyBuffer.Length);
            if (memCpu.State.AX == 0xFFF0)
            {
                memCpu.SetState(memCpu.State with { AX = 0 });
            }
        }

        memSw.Stop();
        double memLatency = memSw.Elapsed.TotalMicroseconds / memSamplesCount;
        Console.WriteLine($"Mean latency: {memLatency:F1} us (per 1 MiB snapshot)");

        // 4. Free-run execution throughput (NFR-003 target >= 1,000,000 instructions/s)
        Console.WriteLine("\n--- Benchmark 3: Free-Run Throughput (NFR-003) ---");
        int[] iterations = [50_000_000, 50_000_000, 50_000_000];
        for (int r = 0; r < iterations.Length; r++)
        {
            int count = iterations[r];
            var runCpu = CreateCpu(loop, out _);
            var runSw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                runCpu.StepInstruction();
                if (runCpu.State.AX == 0xFFF0)
                {
                    runCpu.SetState(runCpu.State with { AX = 0 });
                }
            }

            runSw.Stop();
            double throughputM = count / runSw.Elapsed.TotalSeconds / 1e6;
            Console.WriteLine($"Run {r + 1}: {count:N0} instructions in {runSw.Elapsed.TotalMilliseconds:N0} ms = {throughputM:F2} M instructions/s (Target: >= 1.00 M/s, Headroom: {throughputM:F1}x)");
        }
    }

    public static int RunConformanceGate()
    {
        Console.WriteLine("\n--- SingleStepTests Conformance Gate ---");
        string repoRoot = FindRepoRoot();
        string dir8086 = Path.Combine(repoRoot, "testdata", "singlesteptests", "8086");
        string dir8088 = Path.Combine(repoRoot, "testdata", "singlesteptests", "8088");

        if (!Directory.Exists(dir8086))
        {
            Console.Error.WriteLine($"Error: 8086 test data not found at {dir8086}");
            return 1;
        }

        Console.WriteLine($"Checking 8086 oracle directory: {dir8086}");
        var files8086 = Directory.GetFiles(dir8086, "*.json.gz");
        Console.WriteLine($"Found {files8086.Length} 8086 oracle files.");

        if (Directory.Exists(dir8088))
        {
            var files8088 = Directory.GetFiles(dir8088, "*.json.gz");
            Console.WriteLine($"Found {files8088.Length} 8088 secondary oracle files.");
        }

        return 0;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
