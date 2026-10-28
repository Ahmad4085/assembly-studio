using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Console;

/// <summary>
/// A no-op console device used as the default fallback when no console is injected into <see cref="IMachine"/>.
/// Discards all output bytes, reports no delta, and indicates an empty input queue.
/// </summary>
public sealed class NullConsoleDevice : IConsoleDevice
{
    /// <inheritdoc />
    public void WriteOutput(byte value)
    {
        // Discard output in null device.
    }

    /// <inheritdoc />
    public string TakeOutput() => string.Empty;

    /// <inheritdoc />
    public bool TryReadInput(out byte value)
    {
        value = 0;
        return false;
    }
}
