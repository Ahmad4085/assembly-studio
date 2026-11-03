using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Loading;
using Xunit;
using static AssemblyStudio.ProgramTests.ProgramHarness;

namespace AssemblyStudio.ProgramTests;

/// <summary>
/// TASK-115 (FR-031 v2.2, AC-020, D-023, DEF-003): the build and the loader share one .COM size limit. The sizes come
/// from <see cref="ComLoader.MaxComImageSize"/>, so a build limit that drifts from the loader's fails here.
/// </summary>
public class ComSizeLimitTests
{
    [Fact]
    public async Task An_image_of_the_loaders_largest_size_builds_loads_and_runs()
    {
        BuildResult build = await BuildTextAsync("largest", $"org 100h\nret\ntimes {ComLoader.MaxComImageSize - 1} db 90h\n");

        Assert.Equal(BuildOutcome.Succeeded, build.Outcome);
        Assert.Equal(ComLoader.MaxComImageSize, build.Image.Length);

        ProgramRun run = Run(build);

        Assert.Equal(RunEnd.Terminated, run.End);
        Assert.Equal(TerminationPath.ReturnToPsp, run.Termination?.Path); // the return word at FFFEh is intact
    }

    [Fact]
    public async Task One_byte_more_fails_to_build_with_the_size_limit_message()
    {
        BuildResult build = await TryBuildTextAsync($"org 100h\ntimes {ComLoader.MaxComImageSize + 1} db 90h\n");

        Assert.Equal(BuildOutcome.Failed, build.Outcome);
        Diagnostic limit = Assert.Single(build.Diagnostics, d => d.Code == "BLD006");
        Assert.Contains($"at most {ComLoader.MaxComImageSize:N0} bytes", limit.What, StringComparison.Ordinal);
    }
}
