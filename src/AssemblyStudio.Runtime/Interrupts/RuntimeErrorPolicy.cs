using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Interrupts;

/// <summary>
/// Policy engine that generates structured runtime error and unsupported service diagnostic
/// messages adhering to the What/Where/Next contract (FR-038, FR-040, FR-066, NFR-024, AC-029, AC-031).
/// </summary>
public static class RuntimeErrorPolicy
{
    /// <summary>
    /// Creates a structured <see cref="RuntimeHaltInfo"/> for an unhandled CPU exception
    /// (INT 0..4) with the default vector (FR-038).
    /// </summary>
    /// <param name="vector">CPU exception interrupt vector number (0–4).</param>
    /// <param name="state">CPU state at the faulting instruction.</param>
    /// <returns>A populated <see cref="RuntimeHaltInfo"/> record.</returns>
    public static RuntimeHaltInfo CreateCpuExceptionHalt(byte vector, CpuState state)
    {
        string where = $"at {state.CS:X4}:{state.IP:X4}";

        return vector switch
        {
            0 => new RuntimeHaltInfo(
                Code: "RTE001",
                What: "Divide error: division by zero or quotient overflow",
                Where: where,
                NextStep: "Ensure the divisor is non-zero and that the quotient fits within the destination register.",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            1 => new RuntimeHaltInfo(
                Code: "RTE002",
                What: "Single-step trap (INT 1)",
                Where: where,
                NextStep: "Clear the Trap Flag (TF) in FLAGS or install a custom INT 01h handler.",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            2 => new RuntimeHaltInfo(
                Code: "RTE003",
                What: "Non-maskable interrupt (INT 2)",
                Where: where,
                NextStep: "Install a custom INT 02h handler or check hardware interrupt conditions.",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            3 => new RuntimeHaltInfo(
                Code: "RTE004",
                What: "Breakpoint interrupt (INT 3)",
                Where: where,
                NextStep: "Install a custom INT 03h handler or remove the breakpoint instruction (0xCC).",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            4 => new RuntimeHaltInfo(
                Code: "RTE005",
                What: "Overflow trap (INTO with OF=1)",
                Where: where,
                NextStep: "Install a custom INT 04h handler or check preceding arithmetic operations for signed overflow.",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            _ => new RuntimeHaltInfo(
                Code: "RTE000",
                What: $"CPU exception (INT {vector:X2}h)",
                Where: where,
                NextStep: $"Install a custom INT {vector:X2}h vector handler.",
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),
        };
    }

    /// <summary>
    /// Creates a structured <see cref="RuntimeHaltInfo"/> for an unsupported software interrupt
    /// or unhandled service function with the default vector (FR-040, NFR-013, AC-029).
    /// </summary>
    /// <param name="vector">Interrupt vector number.</param>
    /// <param name="state">CPU state at the INT instruction.</param>
    /// <returns>A populated <see cref="RuntimeHaltInfo"/> record.</returns>
    public static RuntimeHaltInfo CreateUnsupportedServiceHalt(byte vector, CpuState state)
    {
        byte ah = (byte)(state.AX >> 8);
        string where = $"at {state.CS:X4}:{state.IP:X4}";

        if (vector == 0x21)
        {
            return new RuntimeHaltInfo(
                Code: "RTE021",
                What: $"INT 21h function {ah:X2}h (AH={ah:X2}h) is unsupported",
                Where: where,
                NextStep: "Host file, device, process and network services are not available in the offline emulator (NFR-013). Use supported DOS functions (01h, 02h, 07h, 08h, 09h, 0Ah, 4Ch).",
                HaltReason: HaltReason.UnsupportedService,
                Vector: vector,
                Function: ah);
        }

        return new RuntimeHaltInfo(
            Code: "RTE020",
            What: $"INT {vector:X2}h function {ah:X2}h (AH={ah:X2}h) is unsupported",
            Where: where,
            NextStep: $"Install a program vector for INT {vector:X2}h or use supported DOS services (INT 20h, INT 21h).",
            HaltReason: HaltReason.UnsupportedService,
            Vector: vector,
            Function: ah);
    }

    /// <summary>
    /// The message for an instruction outside the supported 8086 set (FR-037, AC-028). The CPU halts before it; the
    /// debugger adds this message from the halt and the instruction's bytes.
    /// </summary>
    /// <param name="cs">CS of the instruction.</param>
    /// <param name="ip">IP of the instruction.</param>
    /// <param name="bytes">The bytes at CS:IP (the opcode and what follows; its length is unknown).</param>
    public static RuntimeMessage UnsupportedInstruction(ushort cs, ushort ip, IReadOnlyList<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        string hex = string.Join(" ", bytes.Take(6).Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
        return new RuntimeMessage(
            Code: "RTE010",
            What: $"Unsupported instruction (bytes at CS:IP: {hex})",
            Where: $"at {cs:X4}:{ip:X4}",
            NextStep: "This instruction is outside the supported 8086 set. See Help for the supported instructions, change the code, then Reset.");
    }

    /// <summary>The message for a HLT instruction: the program stopped the CPU (it has no hardware interrupts to wait for).</summary>
    public static RuntimeMessage Hlt(ushort cs, ushort ip) => new(
        Code: "RTE011",
        What: "The program executed HLT and the CPU stopped",
        Where: $"at {cs:X4}:{ip:X4}",
        NextStep: "End the program with INT 21h function 4Ch instead of HLT, then Reset.");

    /// <summary>The message for an internal emulator fault (NFR-010): the session stopped; the IDE stays usable.</summary>
    public static RuntimeMessage EngineFault(string fault) => new(
        Code: "RTE090",
        What: $"Internal emulator error ({fault})",
        Where: "in the emulator, not in your program",
        NextStep: "Reset to try again. The log has the details (Help ▸ About ▸ Diagnostics).");
}
