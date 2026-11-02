using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Interrupts;

/// <summary>
/// Structured information describing an execution halt caused by a runtime error
/// (CPU exception) or an unsupported interrupt/service (FR-038, FR-040, FR-066).
/// Conforms to the What/Where/Next structure required by NFR-024 and AC-055.
/// </summary>
/// <param name="Code">Stable message code (e.g. <c>"RTE001"</c>, <c>"RTE021"</c>).</param>
/// <param name="What">Clear statement of what happened.</param>
/// <param name="Where">Address or location where the halt occurred (e.g. <c>"at 0800:0100"</c>).</param>
/// <param name="NextStep">Suggested actionable next step for the user.</param>
/// <param name="HaltReason">The <see cref="Core.HaltReason"/> associated with this halt.</param>
/// <param name="Vector">Interrupt vector number (0–255).</param>
/// <param name="Function">Optional service function number (e.g. AH value), or <see langword="null"/>.</param>
public sealed record RuntimeHaltInfo(
    string Code,
    string What,
    string Where,
    string NextStep,
    HaltReason HaltReason,
    byte Vector,
    byte? Function = null)
{
    /// <summary>
    /// Formatted message combining What, Where, and NextStep.
    /// </summary>
    public string FormattedMessage => $"{What} {Where}. {NextStep}";

    /// <summary>The message as it travels with the halt's <see cref="StepResult"/> (TASK-083).</summary>
    public RuntimeMessage ToMessage() => new(Code, What, Where, NextStep);

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {What} {Where}. Suggested next step: {NextStep}";
}
