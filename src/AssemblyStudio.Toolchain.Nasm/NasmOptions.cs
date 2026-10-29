namespace AssemblyStudio.Toolchain.Nasm;

/// <summary>Configuration of <see cref="NasmAssembler"/> (ADR-0005 §1–2, NFR-014).</summary>
public sealed record NasmOptions
{
    /// <summary>The default build timeout (FR-025: 10 s).</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The default cap on captured assembler output, per stream (NFR-014: 1 MiB of characters).</summary>
    public const int DefaultOutputCap = 1 << 20;

    /// <summary>Path of the bundled NASM (<c>tools\nasm\nasm.exe</c> next to the application), set by the
    /// composition root. <see langword="null"/> when the application ships without one.</summary>
    public string? BundledPath { get; init; }

    /// <summary>The user-configured NASM path from Settings (FR-023, FR-075). When set it overrides the
    /// bundled one, and it must be a full path to an existing <c>.exe</c> file.</summary>
    public string? ConfiguredPath { get; init; }

    /// <summary>Build timeout; the process tree is killed when it is exceeded.</summary>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    /// <summary>Maximum characters captured from each output stream.</summary>
    public int OutputCap { get; init; } = DefaultOutputCap;

    /// <summary>Folder in which each build gets its own temporary directory, deleted after the build.</summary>
    public string TempRoot { get; init; } = Path.Combine(Path.GetTempPath(), "AssemblyStudio", "builds");
}
