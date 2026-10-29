using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm.Diagnostics;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// TASK-042: CPU-directive guard tests (FR-027, ADR-0005 §3).
/// Scans preprocessed NASM output for non-8086 CPU directives.
/// </summary>
public sealed class CpuDirectiveGuardTests
{
    private const string AssembledFile = @"C:\project\src\main.asm";
    private const string DocumentFile = @"C:\project\main.asm";
    private const string WorkDir = @"C:\project";

    [Theory]
    [InlineData("8086")]
    [InlineData("8086 ")]
    public void Cpu_8086_directive_is_allowed(string level)
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            $"[cpu {level}]\n" +
            "mov ax, 1\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("186")]
    [InlineData("286")]
    [InlineData("386")]
    [InlineData("486")]
    [InlineData("586")]
    [InlineData("686")]
    [InlineData("PENTIUM")]
    [InlineData("x64")]
    public void Non_8086_directives_are_rejected_with_BLD009(string level)
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            $"[cpu {level}]\n" +
            "mov ax, 1\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BLD009", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(DocumentFile, diagnostic.File);
        Assert.Equal(2, diagnostic.Line);
        Assert.Contains(level, diagnostic.What, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.NextStep));
    }

    [Fact]
    public void Macro_expansion_reports_macro_definition_line()
    {
        // When NASM expands a macro containing cpu 386 on line 2 of main.asm:
        string preprocessed =
            $"%line 4+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            "mov ax, 1\n" +
            $"%line 2+1 {AssembledFile}\n" +
            "[cpu 386]\n" +
            $"%line 7+1 {AssembledFile}\n" +
            "ret\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BLD009", diagnostic.Code);
        Assert.Equal(DocumentFile, diagnostic.File);
        Assert.Equal(2, diagnostic.Line);
        Assert.Contains("386", diagnostic.What, StringComparison.Ordinal);
    }

    [Fact]
    public void Include_file_directive_reports_include_file_path_and_line()
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            "%line 1+1 inc\\helpers.inc\n" +
            "nop\n" +
            "[cpu 286]\n" +
            $"%line 3+1 {AssembledFile}\n" +
            "ret\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BLD009", diagnostic.Code);
        Assert.Equal(@"C:\project\inc\helpers.inc", diagnostic.File);
        Assert.Equal(2, diagnostic.Line);
        Assert.Contains("286", diagnostic.What, StringComparison.Ordinal);
    }

    [Fact]
    public void Untitled_document_reports_null_file_path()
    {
        const string tempAssembled = @"C:\temp\build-1\src\untitled.asm";
        string preprocessed =
            $"%line 1+1 {tempAssembled}\n" +
            "[org 100h]\n" +
            "[cpu 386]\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, tempAssembled, null, @"C:\temp\build-1");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BLD009", diagnostic.Code);
        Assert.Null(diagnostic.File);
        Assert.Equal(2, diagnostic.Line);
    }

    [Fact]
    public void Multiple_cpu_directives_report_all_occurrences()
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            "[cpu 286]\n" +
            "mov ax, 1\n" +
            "[cpu 386]\n" +
            "ret\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal((2, "286"), (diagnostics[0].Line!.Value, "286"));
        Assert.Contains("286", diagnostics[0].What, StringComparison.Ordinal);
        Assert.Equal((4, "386"), (diagnostics[1].Line!.Value, "386"));
        Assert.Contains("386", diagnostics[1].What, StringComparison.Ordinal);
    }

    [Fact]
    public void String_literals_containing_cpu_are_not_flagged()
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            "db \"[cpu 386]\"\n" +
            "mov ax, 1\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Blank_lines_and_increments_are_counted_accurately()
    {
        string preprocessed =
            $"%line 1+1 {AssembledFile}\n" +
            "[org 100h]\n" +
            "\n" +
            "\n" +
            "[cpu 386]\n" +
            "\n" +
            "mov ax, 1\n";

        IReadOnlyList<Diagnostic> diagnostics = CpuDirectiveGuard.Scan(preprocessed, AssembledFile, DocumentFile, WorkDir);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(4, diagnostic.Line);
    }
}
