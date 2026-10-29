namespace AssemblyStudio.Core;

/// <summary>
/// Assembler integration (C03, ADR-0005): finds and checks the assembler and builds a source into a
/// flat .COM image (FR-023–FR-026, FR-030). Implementations run the assembler as an external process
/// without a shell, in an isolated per-build temporary directory, with a timeout and an output cap
/// (NFR-014). They never execute the program they build.
/// </summary>
public interface IAssembler
{
    /// <summary>Finds the assembler that a build would use and checks that it runs and which version
    /// it reports (FR-023, FR-024, AC-015). Does not build anything.</summary>
    AssemblerInfo Check();

    /// <summary>Builds <paramref name="request"/>. Never throws for assembler failures: a missing
    /// assembler, a timeout, a crash, cancellation and a failed build are all reported as a
    /// <see cref="BuildResult"/> (FR-030, FR-095).</summary>
    /// <param name="request">The document to build.</param>
    /// <param name="cancellationToken">Cancels the build; the assembler process tree is killed.</param>
    Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken = default);
}
