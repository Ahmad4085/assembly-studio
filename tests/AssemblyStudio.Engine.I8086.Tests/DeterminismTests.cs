using System.Collections.Generic;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests;

/// <summary>
/// Determinism tests for the 8086 CPU execution engine (TASK-020, NFR-016, AC-047).
/// Verifies that running identical initial machine states produces bit-for-bit identical
/// step outcomes, register states, flag values, and memory traces over long execution paths.
/// </summary>
public class DeterminismTests
{
    private readonly record struct TraceEntry(
        int StepIndex,
        StepOutcome Outcome,
        ushort InstructionIp,
        CpuState State);

    [Fact]
    public void Same_program_and_state_produces_identical_trace_over_long_run()
    {
        // Assemble a multi-stage 8086 program exercising arithmetic, logic, shifts, string ops,
        // subroutine call/return, stack ops, and flag manipulations:
        //
        // 1000:0100  B8 00 00          MOV AX, 0
        // 1000:0103  BB 00 10          MOV BX, 1000h
        // 1000:0106  B9 14 00          MOV CX, 20      ; 20 iterations
        //
        // ; Loop 1: arithmetic, logic, and conditional branch
        // loop1:
        // 1000:0109  01 D8             ADD AX, BX
        // 1000:010B  D1 E8             SHR AX, 1
        // 1000:010D  81 F0 AA 55       XOR AX, 55AAh
        // 1000:0111  50                PUSH AX
        // 1000:0112  58                POP AX
        // 1000:0113  E2 F4             LOOP loop1      ; dec cx, jnz loop1 (offset -12 = F4h)
        //
        // ; Subroutine call:
        // 1000:0115  E8 0C 00          CALL sub1       ; call sub1 (offset +12 = 000Ch) -> 0124h
        //
        // ; String operation:
        // 1000:0118  BE 00 02          MOV SI, 0200h   ; DS:SI
        // 1000:011B  BF 00 03          MOV DI, 0300h   ; ES:DI
        // 1000:011E  B9 10 00          MOV CX, 16      ; 16 bytes
        // 1000:0121  FC                CLD
        // 1000:0122  F3 A4             REP MOVSB
        // 1000:0124  F4                HLT
        //
        // ; Subroutine sub1 (at 0125h):
        // sub1:
        // 1000:0125  9C                PUSHF
        // 1000:0126  F9                STC
        // 1000:0127  15 01 00          ADC AX, 1
        // 1000:012A  9D                POPF
        // 1000:012B  C3                RET

        byte[] program =
        [
            0xB8, 0x00, 0x00,             // MOV AX, 0
            0xBB, 0x00, 0x10,             // MOV BX, 1000h
            0xB9, 0x14, 0x00,             // MOV CX, 20
            // loop1 (offset 0x09)
            0x01, 0xD8,                   // ADD AX, BX
            0xD1, 0xE8,                   // SHR AX, 1
            0x81, 0xF0, 0xAA, 0x55,       // XOR AX, 55AAh
            0x50,                         // PUSH AX
            0x58,                         // POP AX
            0xE2, 0xF4,                   // LOOP loop1 (-12)
            // post loop1 (offset 0x15)
            0xE8, 0x0D, 0x00,             // CALL sub1 (+13 -> 0x25)
            // post call (offset 0x18)
            0xBE, 0x00, 0x02,             // MOV SI, 0200h
            0xBF, 0x00, 0x03,             // MOV DI, 0300h
            0xB9, 0x10, 0x00,             // MOV CX, 16
            0xFC,                         // CLD
            0xF3, 0xA4,                   // REP MOVSB
            0xF4,                         // HLT
            // sub1 (offset 0x25)
            0x9C,                         // PUSHF
            0xF9,                         // STC
            0x15, 0x01, 0x00,             // ADC AX, 1
            0x9D,                         // POPF
            0xC3                          // RET
        ];

        // Seed source buffer at DS:0200h with deterministic pseudo-random bytes
        var seedBytes = new byte[16];
        for (int i = 0; i < seedBytes.Length; i++)
        {
            seedBytes[i] = (byte)(0x41 + (i * 7 % 26));
        }

        var (trace1, finalMem1) = ExecuteTrace(program, seedBytes, maxSteps: 500);
        var (trace2, finalMem2) = ExecuteTrace(program, seedBytes, maxSteps: 500);

        Assert.True(trace1.Count > 100, $"Trace length {trace1.Count} should be > 100 steps");
        Assert.Equal(trace1.Count, trace2.Count);

        for (int i = 0; i < trace1.Count; i++)
        {
            var t1 = trace1[i];
            var t2 = trace2[i];

            Assert.Equal(t1.StepIndex, t2.StepIndex);
            Assert.Equal(t1.Outcome, t2.Outcome);
            Assert.Equal(t1.InstructionIp, t2.InstructionIp);
            Assert.Equal(t1.State, t2.State);
        }

        // Verify final memory contents match
        Assert.Equal(finalMem1.Count, finalMem2.Count);
        foreach (var (addr, b) in finalMem1)
        {
            Assert.Equal(b, finalMem2[addr]);
        }

        // Verify string copy occurred to ES:0300h (physical 10000h + 0300h = 10300h)
        for (int i = 0; i < seedBytes.Length; i++)
        {
            int destPhysical = (0x1000 << 4) + 0x0300 + i;
            Assert.Equal(seedBytes[i], finalMem1[destPhysical]);
        }
    }

    [Fact]
    public void Different_initial_input_diverges_deterministically()
    {
        byte[] program =
        [
            0xB8, 0x00, 0x00,       // MOV AX, 0
            0x8A, 0x06, 0x00, 0x02, // MOV AL, [0200h]
            0x3C, 0x42,             // CMP AL, 42h
            0x74, 0x03,             // JZ +3
            0xB0, 0x01,             // MOV AL, 1
            0xF4                    // HLT
        ];

        byte[] input1 = [0x42];
        byte[] input2 = [0x99];

        var (trace1, _) = ExecuteTrace(program, input1, maxSteps: 20);
        var (trace2, _) = ExecuteTrace(program, input2, maxSteps: 20);

        // The traces must diverge at the conditional jump step
        bool diverged = false;
        for (int i = 0; i < Math.Min(trace1.Count, trace2.Count); i++)
        {
            if (trace1[i].State != trace2[i].State)
            {
                diverged = true;
                break;
            }
        }

        Assert.True(diverged, "Different inputs must produce diverging CPU states");
    }

    private static (List<TraceEntry> Trace, Dictionary<int, byte> ModifiedMemory) ExecuteTrace(
        byte[] program,
        byte[] dataAt0200,
        int maxSteps)
    {
        var memory = new FakeMemory();

        // Load program at 1000:0100 (physical 10100h)
        int codeBase = (0x1000 << 4) + 0x0100;
        for (int i = 0; i < program.Length; i++)
        {
            memory[codeBase + i] = program[i];
        }

        // Load data at DS:0200 (physical 10200h)
        int dataBase = (0x1000 << 4) + 0x0200;
        for (int i = 0; i < dataAt0200.Length; i++)
        {
            memory[dataBase + i] = dataAt0200[i];
        }

        var cpu = new Cpu8086(memory);
        cpu.SetState(new CpuState(
            AX: 0, BX: 0, CX: 0, DX: 0,
            SP: 0xFFFE, BP: 0, SI: 0, DI: 0,
            CS: 0x1000, DS: 0x1000, ES: 0x1000, SS: 0x1000,
            IP: 0x0100, Flags: 0xF002));

        var trace = new List<TraceEntry>();
        for (int step = 0; step < maxSteps; step++)
        {
            var res = cpu.StepInstruction();
            trace.Add(new TraceEntry(step, res.Outcome, res.InstructionIp, cpu.State));
            if (res.Outcome == StepOutcome.Halted)
            {
                break;
            }
        }

        // Snapshot all non-zero memory
        var finalMem = new Dictionary<int, byte>();
        for (int i = 0; i < FakeMemory.Size; i++)
        {
            byte b = memory[i];
            if (b != 0)
            {
                finalMem[i] = b;
            }
        }

        return (trace, finalMem);
    }
}
