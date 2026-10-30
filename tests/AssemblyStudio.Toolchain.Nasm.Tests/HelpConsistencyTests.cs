using System.Globalization;
using System.Reflection;
using AssemblyStudio.Content;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm.Diagnostics;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// TASK-093, AC-051 (FR-032): the help page "Building with NASM" and the MASM page agree with the build: the exact
/// NASM command line, every build message code, the limits, and the MASM constructs that produce BLD007.
/// </summary>
public sealed class HelpConsistencyTests
{
    private static readonly EmbeddedContentCatalog Catalog = EmbeddedContentCatalog.BuiltIn();

    private static string Help(string id) => Catalog.ReadText(Catalog.Items.Single(i => i.Id == "help-" + id));

    [Fact]
    public void The_documented_command_line_is_the_one_the_build_uses()
    {
        string[] arguments = NasmAssembler.Arguments(@"<temp>\program.lst", @"<temp>\program.com", "<source>");
        string commandLine = "nasm " + string.Join(" ", arguments.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a));

        Assert.Contains(commandLine, Help("nasm"), StringComparison.Ordinal);
    }

    [Fact]
    public void Every_build_message_code_is_listed()
    {
        string help = Help("nasm");
        string[] codes = [.. typeof(BuildMessages).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)];

        Assert.Equal(13, codes.Length);
        Assert.All(codes.Where(c => c.StartsWith("BLD", StringComparison.Ordinal)), code => Assert.Contains(code, help, StringComparison.Ordinal));
        Assert.Contains("ASM001 to ASM003", help, StringComparison.Ordinal);
        Assert.Equal(["ASM001", "ASM002", "ASM003"], codes.Where(c => c.StartsWith("ASM", StringComparison.Ordinal)).Order());
    }

    [Fact]
    public void The_documented_limits_and_version_are_the_builds()
    {
        string nasm = Help("nasm");

        Assert.Contains($"larger than {BuildMessages.MaxComImageSize.ToString("N0", CultureInfo.InvariantCulture)} bytes", nasm, StringComparison.Ordinal);
        Assert.Contains($"up to {BuildMessages.MaxComImageSize.ToString("N0", CultureInfo.InvariantCulture)} bytes", Help("scope"), StringComparison.Ordinal);
        Assert.Contains($"more than {NasmOptions.DefaultTimeout.TotalSeconds:0} seconds", nasm, StringComparison.Ordinal);
        Assert.Equal(1 << 20, NasmOptions.DefaultOutputCap);
        Assert.Contains("At most 1 MB of messages", nasm, StringComparison.Ordinal);
        Assert.Contains($"version {NasmAssembler.MinimumVersion.Major}.{NasmAssembler.MinimumVersion.Minor:00} or later", nasm, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(".model small", ".model")]
    [InlineData(".code", ".code")]
    [InlineData(".data", ".data")]
    [InlineData(".stack 100h", ".stack")]
    [InlineData(".startup", ".startup")]
    [InlineData(".exit", ".exit")]
    [InlineData(".386", ".386")]
    [InlineData("assume cs:code", "ASSUME")]
    [InlineData("main proc", "PROC")]
    [InlineData("main endp", "ENDP")]
    [InlineData("code segment", "SEGMENT")]
    [InlineData("code ends", "ENDS")]
    [InlineData("mov ax, @data", "@data")]
    public async Task A_failed_build_with_a_MASM_construct_named_in_the_help_gets_BLD007(string line, string keyword)
    {
        Assert.Contains(keyword, Help("nasm-vs-masm"), StringComparison.Ordinal);
        var assembler = new NasmAssembler(new NasmOptions { BundledPath = NasmLocator.GetNasmPath() });
        string source = $"org 100h\n{line}\nmov ax, offset message\nret\nmessage db 'x$'\n"; // OFFSET (MASM) fails in NASM

        BuildResult result = await assembler.BuildAsync(new BuildRequest(null, source), TestContext.Current.CancellationToken);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Diagnostic hint = Assert.Single(result.Diagnostics, d => d.Code == BuildMessages.MasmHint);
        Assert.Equal(2, hint.Line); // the first MASM-style line
    }
}
