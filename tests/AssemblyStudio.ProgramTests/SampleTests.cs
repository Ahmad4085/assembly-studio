using AssemblyStudio.Content;
using AssemblyStudio.Core;
using Xunit;
using static AssemblyStudio.ProgramTests.ProgramHarness;

namespace AssemblyStudio.ProgramTests;

/// <summary>
/// TASK-091, AC-035 (FR-068): every built-in sample, taken from the content catalog exactly as the Learn sidebar opens
/// it, is built by the bundled NASM and run headless on the real engine, runtime and DOS services with its documented
/// input. Its console output must equal the documented expected output, and it must end through INT 21h function 4Ch
/// with exit code 0. The expected outputs were written from the x86 and DOS semantics each sample explains, not copied
/// from a run.
/// </summary>
public sealed class SampleTests
{
    private static readonly EmbeddedContentCatalog Catalog = EmbeddedContentCatalog.BuiltIn();

    private static IEnumerable<ContentItem> Samples => Catalog.Items.Where(i => i.Kind == ContentKind.Sample);

    public static TheoryData<string> SampleIds() => [.. Samples.Select(s => s.Id)];

    [Fact]
    public void FR_068_there_are_at_least_8_samples_covering_the_required_topics()
    {
        string[] required =
        [
            "hello", "arithmetic-flags", "loops", "conditional-jumps", "stack-procedures", "strings", "keyboard-input", "addressing-modes",
        ];

        Assert.True(Samples.Count() >= 8, $"{Samples.Count()} samples");
        Assert.Subset(Samples.Select(s => s.Id).ToHashSet(), required.ToHashSet());
    }

    [Theory]
    [MemberData(nameof(SampleIds))]
    public async Task AC_035_the_sample_prints_its_documented_output_and_exits_normally(string id)
    {
        ContentItem sample = Samples.Single(s => s.Id == id);
        string source = Catalog.ReadText(sample);

        BuildResult build = await BuildTextAsync(sample.FileName, source);
        ProgramRun run = Run(build, InputScript.Typed([.. sample.Input ?? []]));

        Assert.DoesNotContain(build.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Equal(RunEnd.Terminated, run.End);
        Assert.Equal(sample.ExpectedOutput, run.Output);
        Assert.Equal(new ProgramTermination(TerminationPath.Int21hFunction4Ch, 0x00), run.Termination);
    }

    [Theory]
    [MemberData(nameof(SampleIds))]
    public void Every_sample_starts_with_a_comment_that_names_it(string id)
    {
        ContentItem sample = Samples.Single(s => s.Id == id);

        string firstLine = Catalog.ReadText(sample).Split('\n')[0];

        Assert.Equal($"; {sample.Title}", firstLine.TrimEnd('\r'));
    }

    [Fact]
    public async Task The_keyboard_sample_waits_for_input_when_none_is_typed()
    {
        ContentItem sample = Samples.Single(s => s.Id == "keyboard-input");

        ProgramRun run = Run(await BuildTextAsync(sample.FileName, Catalog.ReadText(sample)));

        Assert.Equal(RunEnd.WaitingForInput, run.End);
        Assert.Equal("What is your name? ", run.Output);
    }
}
