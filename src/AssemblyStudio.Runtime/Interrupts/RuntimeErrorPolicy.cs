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
        string where = RuntimeMessages.Text("WhereAt", state.CS, state.IP);

        return vector switch
        {
            0 => new RuntimeHaltInfo(
                Code: "RTE001",
                What: RuntimeMessages.Text("RTE001_What"),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE001_Next"),
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            1 => new RuntimeHaltInfo(
                Code: "RTE002",
                What: RuntimeMessages.Text("RTE002_What"),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE002_Next"),
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            2 => new RuntimeHaltInfo(
                Code: "RTE003",
                What: RuntimeMessages.Text("RTE003_What"),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE003_Next"),
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            3 => new RuntimeHaltInfo(
                Code: "RTE004",
                What: RuntimeMessages.Text("RTE004_What"),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE004_Next"),
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            4 => new RuntimeHaltInfo(
                Code: "RTE005",
                What: RuntimeMessages.Text("RTE005_What"),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE005_Next"),
                HaltReason: HaltReason.RuntimeError,
                Vector: vector),

            _ => new RuntimeHaltInfo(
                Code: "RTE000",
                What: RuntimeMessages.Text("RTE000_What", vector),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE000_Next", vector),
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
        string where = RuntimeMessages.Text("WhereAt", state.CS, state.IP);

        if (vector == 0x21)
        {
            return new RuntimeHaltInfo(
                Code: "RTE021",
                What: RuntimeMessages.Text("RTE021_What", ah),
                Where: where,
                NextStep: RuntimeMessages.Text("RTE021_Next"),
                HaltReason: HaltReason.UnsupportedService,
                Vector: vector,
                Function: ah);
        }

        return new RuntimeHaltInfo(
            Code: "RTE020",
            What: RuntimeMessages.Text("RTE020_What", vector, ah),
            Where: where,
            NextStep: RuntimeMessages.Text("RTE020_Next", vector),
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
            What: RuntimeMessages.Text("RTE010_What", hex),
            Where: RuntimeMessages.Text("WhereAt", cs, ip),
            NextStep: RuntimeMessages.Text("RTE010_Next"));
    }

    /// <summary>The message for a HLT instruction: the program stopped the CPU (it has no hardware interrupts to wait for).</summary>
    public static RuntimeMessage Hlt(ushort cs, ushort ip) => new(
        Code: "RTE011",
        What: RuntimeMessages.Text("RTE011_What"),
        Where: RuntimeMessages.Text("WhereAt", cs, ip),
        NextStep: RuntimeMessages.Text("RTE011_Next"));

    /// <summary>The message for an internal emulator fault (NFR-010): the session stopped; the IDE stays usable.</summary>
    public static RuntimeMessage EngineFault(string fault) => new(
        Code: "RTE090",
        What: RuntimeMessages.Text("RTE090_What", fault),
        Where: RuntimeMessages.Text("RTE090_Where"),
        NextStep: RuntimeMessages.Text("RTE090_Next"));
}
