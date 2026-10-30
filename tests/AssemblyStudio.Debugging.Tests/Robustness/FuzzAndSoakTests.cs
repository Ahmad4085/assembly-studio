using System.Diagnostics;
using System.Globalization;
using AssemblyStudio.Core;
using Xunit;
using static AssemblyStudio.Debugging.Tests.Robustness.RobustnessKit;

namespace AssemblyStudio.Debugging.Tests.Robustness;

/// <summary>
/// TASK-054, NFR-010: random images and random commands never crash or hang the host, and never raise an engine
/// exception. Seeds are fixed, so a failure names a reproducible case. The soak's length comes from the
/// <c>ASSEMBLY_STUDIO_SOAK_MINUTES</c> environment variable (default 0.25 in the normal suite; the 30-minute run
/// sets it to 30). Both run on their own within the assembly, because they keep the CPU busy.
/// </summary>
[Collection(nameof(RobustnessSequential))]
public sealed class FuzzAndSoakTests
{
    private const string SoakMinutesVariable = "ASSEMBLY_STUDIO_SOAK_MINUTES";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void Fuzz_600_random_images_with_random_input_never_fault_or_hang()
    {
        var random = new Random(20261029);
        var outcomes = new Dictionary<string, int>();
        for (int n = 0; n < 600; n++)
        {
            string outcome = RunRandomImage(random, budget: 20_000, $"fuzz image {n}");
            outcomes[outcome] = outcomes.GetValueOrDefault(outcome) + 1;
        }

        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(", ", outcomes.OrderBy(o => o.Key).Select(o => $"{o.Key}: {o.Value}")));
    }

    [Fact]
    public void Soak_random_programs_and_commands_for_the_configured_time()
    {
        double minutes = double.TryParse(Environment.GetEnvironmentVariable(SoakMinutesVariable), NumberStyles.Float, CultureInfo.InvariantCulture, out double m) ? m : 0.25;
        var random = new Random(4085);
        var deadline = Stopwatch.StartNew();
        using var process = Process.GetCurrentProcess();
        long startMemory = SettledManagedMemory();
        int startThreads = process.Threads.Count;
        int images = 0;
        int commands = 0;
        var outcomes = new Dictionary<string, int>();

        while (deadline.Elapsed < TimeSpan.FromMinutes(minutes))
        {
            string outcome = images % 10 == 9
                ? DriveWithRandomCommands(random, ref commands, $"soak image {images}")
                : RunRandomImage(random, budget: 50_000, $"soak image {images}");
            outcomes[outcome] = outcomes.GetValueOrDefault(outcome) + 1;
            images++;
        }

        long endMemory = SettledManagedMemory();
        process.Refresh();
        int endThreads = process.Threads.Count;
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Soak {deadline.Elapsed.TotalMinutes:F1} min: {images:N0} programs, {commands:N0} random commands, 0 faults; managed memory {startMemory / 1048576.0:F1} → {endMemory / 1048576.0:F1} MiB; threads {startThreads} → {endThreads}; outcomes {string.Join(", ", outcomes.OrderBy(o => o.Key).Select(o => $"{o.Key}: {o.Value}"))}");
        Assert.True(endMemory - startMemory < 64L * 1024 * 1024, $"Managed memory grew by {(endMemory - startMemory) / 1048576.0:F1} MiB.");
        Assert.True(endThreads - startThreads < 20, $"Threads grew from {startThreads} to {endThreads}.");
    }

    /// <summary>Runs one random image to a settled state, answering waits with random keys (Ctrl-C included).</summary>
    private static string RunRandomImage(Random random, ulong budget, string name)
    {
        byte[] image = RandomImage(random);
        IMachine? machine = null;
        using var session = new DebugSession(() => machine = Load(image), Disassembler) { InstructionBudget = budget };
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
        MachineSnapshot settled;
        try
        {
            settled = Settle(session, session.Run, Timeout);
        }
        catch (Xunit.Sdk.XunitException)
        {
            throw new Xunit.Sdk.XunitException(Hung(name, image, machine!));
        }
        for (int round = 0; ; round++)
        {
            Assert.True(settled.State != SessionState.Error, $"{name}: {session.LastFault}");
            if (settled.State != SessionState.WaitingForInput || round == 5)
            {
                return settled.State == SessionState.Paused ? $"Paused/{session.PauseReason}" : settled.State.ToString();
            }

            byte[] keys = RandomKeys(random);
            settled = Settle(session, () => session.SendInput(keys), Timeout);
        }
    }

    /// <summary>Describes a run that didn't settle: where the CPU is and what it executes; saves the image for a repro.</summary>
    private static string Hung(string name, byte[] image, IMachine machine)
    {
        CpuState cpu = machine.Cpu.State; // read from the test thread while the worker is stuck: a diagnostic, not exact
        string bytes = string.Join(" ", Enumerable.Range(0, 8).Select(i => machine.Memory.Read8(cpu.CS, (ushort)(cpu.IP + i)).ToString("X2", CultureInfo.InvariantCulture)));
        string file = Path.Combine(Path.GetTempPath(), $"asmstudio-hang-{Guid.NewGuid():N}.com");
        File.WriteAllBytes(file, image);
        return $"{name}: the run did not settle within {Timeout}. CPU at {cpu.CS:X4}:{cpu.IP:X4} ({bytes}), CX={cpu.CX:X4}, SI={cpu.SI:X4}, DI={cpu.DI:X4}; image saved to {file}";
    }

    /// <summary>Loads a random image and sends a random mix of every command, checking the session never faults.</summary>
    private static string DriveWithRandomCommands(Random random, ref int commands, string name)
    {
        byte[] image = RandomImage(random);
        using var session = new DebugSession(() => Load(image), Disassembler) { InstructionBudget = 30_000 };
        WaitSettled(session, Timeout, s => s.PauseReason == PauseReason.Loaded);
        int errors = 0;
        session.SnapshotPublished += (_, snapshot) =>
        {
            if (snapshot.State == SessionState.Error)
            {
                Interlocked.Increment(ref errors);
            }
        };
        var watches = new List<int>();
        for (int i = 0; i < 40; i++, commands++)
        {
            switch (random.Next(11))
            {
                case 0: session.Run(); break;
                case 1: session.Pause(); break;
                case 2: session.StepInto(); break;
                case 3: session.StepOver(); break;
                case 4: session.SendInput(RandomKeys(random)); break;
                case 5: session.SendCtrlC(); break;
                case 6 when watches.Count < DebugSession.MaxMemoryWatches:
                    watches.Add(session.WatchMemory((ushort)random.Next(0x10000), (ushort)random.Next(0x10000), random.Next(1, 4097)));
                    break;
                case 7 when watches.Count > 0:
                    session.UnwatchMemory(watches[^1]);
                    watches.RemoveAt(watches.Count - 1);
                    break;
                case 8: session.Reset(); break;
                case 9: session.Replay(session.RecordedInput); break;
                default: session.StopSession(); session.Reset(); break;
            }
        }

        // Commands are processed in order, so the snapshot of a final Reset means all of them were handled.
        MachineSnapshot before = session.LatestSnapshot!.Value;
        string state = before.State == SessionState.Paused ? $"Paused/{session.PauseReason}" : before.State.ToString();
        Settle(session, session.Reset, Timeout, s => s.PauseReason == PauseReason.Loaded && s.InstructionsExecuted == 0);
        Assert.True(errors == 0, $"{name}: {errors} error snapshots, last fault {session.LastFault}");
        return "Commands/" + state;
    }

    /// <summary>Random bytes, sometimes seeded with real DOS calls so input, output and exits are exercised too.</summary>
    private static byte[] RandomImage(Random random)
    {
        var image = new byte[random.Next(1, 2049)];
        random.NextBytes(image);
        for (int i = 0; i + 4 < image.Length; i += random.Next(8, 64))
        {
            byte function = new byte[] { 0x01, 0x02, 0x07, 0x08, 0x09, 0x0A, 0x4C, 0x3D }[random.Next(8)];
            image[i] = 0xB4;           // mov ah, function
            image[i + 1] = function;
            image[i + 2] = 0xCD;       // int 21h
            image[i + 3] = 0x21;
        }

        return image;
    }

    private static byte[] RandomKeys(Random random)
    {
        var keys = new byte[random.Next(1, 4)];
        random.NextBytes(keys);
        if (random.Next(8) == 0)
        {
            keys[0] = 0x03; // Ctrl-C
        }

        return keys;
    }

    private static long SettledManagedMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(forceFullCollection: true);
    }
}

/// <summary>The fuzz and soak tests run on their own (they load every core).</summary>
[CollectionDefinition(nameof(RobustnessSequential), DisableParallelization = true)]
public sealed class RobustnessSequential;
