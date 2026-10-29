namespace AssemblyStudio.Os.Dos;

/// <summary>
/// A DOS service call the emulator refuses to complete, in the What/Where/Next shape of the runtime
/// messages (FR-066, NFR-024). The service returns <see cref="Core.HaltReason.RuntimeError"/> and
/// records one of these in <see cref="DosServices.LastError"/>.
/// </summary>
/// <param name="Code">Stable message code (e.g. <c>"DOS001"</c>).</param>
/// <param name="What">What went wrong.</param>
/// <param name="Where">Address of the INT instruction (e.g. <c>"at 0900:0100"</c>).</param>
/// <param name="NextStep">Suggested next step for the user.</param>
public sealed record DosServiceError(string Code, string What, string Where, string NextStep)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {What} {Where}. Suggested next step: {NextStep}";
}
