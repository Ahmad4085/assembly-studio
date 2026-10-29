using System.Collections;
using System.Globalization;
using System.Resources;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm.Diagnostics;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

/// <summary>
/// TASK-041: NASM diagnostics (FR-029), the build message catalog (FR-066, NFR-024), the MASM hint
/// (FR-021) and the .COM size limit (FR-031). Builds use the bundled NASM 3.02.
/// </summary>
public sealed class BuildDiagnosticsTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("asmstudio-task041-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private NasmAssembler Assembler() => new(new NasmOptions
    {
        BundledPath = NasmLocator.GetNasmPath(),
        TempRoot = Path.Combine(_root, "builds"),
    });

    private string Save(string fileName, string text)
    {
        string path = Path.Combine(_root, fileName);
        File.WriteAllText(path, text);
        return path;
    }

    private static Task<BuildResult> Build(NasmAssembler assembler, string? path, string? text) =>
        assembler.BuildAsync(new BuildRequest(path, text), TestContext.Current.CancellationToken);

    // ---- Parser (FR-029) ----

    [Fact]
    public void Parser_reads_every_gnu_severity_and_keeps_the_message_as_written()
    {
        const string output =
            "C:\\p\\a.asm:3: warning: label `x' alone on a line without a colon might be in error [-w+label-orphan]\n" +
            "C:\\p\\a.asm:7: error: invalid combination of opcode and operands\r\n" +
            "C:\\p\\a.asm:9: fatal: unable to open include file `x.inc'\n" +
            "C:\\p\\a.asm:9: panic: something internal\n" +
            "C:\\p\\a.asm:10: info: macro `m' defined here\n" +
            "nasm: error: more than one input file specified\n" +
            "some other line\n";

        IReadOnlyList<Diagnostic> parsed = NasmDiagnosticParser.Parse(output, @"C:\p\a.asm", @"C:\p\a.asm");

        Assert.Equal(
            [
                ("ASM002", DiagnosticSeverity.Warning, 3, "label `x' alone on a line without a colon might be in error [-w+label-orphan]"),
                ("ASM001", DiagnosticSeverity.Error, 7, "invalid combination of opcode and operands"),
                ("ASM001", DiagnosticSeverity.Error, 9, "unable to open include file `x.inc'"),
                ("ASM001", DiagnosticSeverity.Error, 9, "something internal"),
                ("ASM003", DiagnosticSeverity.Info, 10, "macro `m' defined here"),
            ],
            parsed.Select(d => (d.Code, d.Severity, d.Line!.Value, d.What)));
        Assert.All(parsed, d => Assert.False(string.IsNullOrWhiteSpace(d.NextStep)));
    }

    [Fact]
    public void Parser_reports_a_temporary_copy_as_the_document_and_keeps_other_files()
    {
        const string output =
            "C:\\tmp\\build-1\\src\\main.asm:2: error: bad\n" +
            "C:\\proj\\lib.inc:5: error: worse\n";

        IReadOnlyList<Diagnostic> saved = NasmDiagnosticParser.Parse(output, @"C:\tmp\build-1\src\main.asm", @"C:\proj\main.asm");
        IReadOnlyList<Diagnostic> untitled = NasmDiagnosticParser.Parse(output, @"C:\tmp\build-1\src\main.asm", null);

        Assert.Equal([@"C:\proj\main.asm", @"C:\proj\lib.inc"], saved.Select(d => d.File));
        Assert.Equal([null, @"C:\proj\lib.inc"], untitled.Select(d => d.File));
    }

    // ---- AC-013 (toolchain part): a warning on line 3 and an error on line 7 ----

    private const string WarningAndError =
        "org 100h\n" +
        "start:\n" +
        "orphan\n" +              // line 3: label without a colon → warning
        "mov ax, 1\n" +
        "mov bx, 2\n" +
        "mov cx, 3\n" +
        "mov ax,\n" +             // line 7: error
        "int 20h\n";

    [Fact]
    public async Task AC_013_a_build_with_an_error_and_a_warning_fails_and_reports_both_with_line_and_severity()
    {
        string path = Save("ac013.asm", WarningAndError);

        BuildResult result = await Build(Assembler(), path, null);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.True(result.Image.IsEmpty);
        Assert.Equal(
            [("ASM002", DiagnosticSeverity.Warning, 3), ("ASM001", DiagnosticSeverity.Error, 7)],
            result.Diagnostics.Select(d => (d.Code, d.Severity, d.Line!.Value)));
        Assert.All(result.Diagnostics, d => Assert.Equal(path, d.File));
    }

    [Fact]
    public async Task Diagnostics_of_an_unsaved_build_point_at_the_document_not_the_temporary_copy()
    {
        string path = Save("unsaved.asm", "org 100h\nint 20h\n");

        BuildResult saved = await Build(Assembler(), path, WarningAndError);
        BuildResult untitled = await Build(Assembler(), null, WarningAndError);

        Assert.All(saved.Diagnostics, d => Assert.Equal(path, d.File));
        Assert.All(untitled.Diagnostics, d => Assert.Null(d.File));
        Assert.Equal(2, untitled.Diagnostics.Count);
    }

    [Fact]
    public async Task A_warning_alone_still_builds()
    {
        BuildResult result = await Build(Assembler(), null, "org 100h\norphan\nint 20h\n");

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(("ASM002", 2), (warning.Code, warning.Line!.Value));
    }

    [Fact]
    public async Task A_clean_build_has_no_diagnostics()
    {
        BuildResult result = await Build(Assembler(), null, "org 100h\nint 20h\n");

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Empty(result.Diagnostics);
    }

    // ---- AC-014: MASM-style hint ----

    [Fact]
    public async Task AC_014_masm_style_source_fails_with_the_nasm_errors_and_a_hint()
    {
        string path = Save("masm.asm", ".model small\n.code\nstart:\n    mov ax, @data\n    mov ds, ax\nend start\n");

        BuildResult result = await Build(Assembler(), path, null);

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.Contains(result.Diagnostics, d => d.Code == "ASM001");
        Diagnostic hint = Assert.Single(result.Diagnostics, d => d.Code == "BLD007");
        Assert.Equal((DiagnosticSeverity.Info, 1, path), (hint.Severity, hint.Line!.Value, hint.File));
        Assert.Contains(".model small", hint.What, StringComparison.Ordinal);
        Assert.Contains("NASM syntax", hint.What, StringComparison.Ordinal);
        Assert.Contains("Help", hint.NextStep, StringComparison.Ordinal);
        Assert.Same(hint, result.Diagnostics[^1]); // after the assembler's own errors
    }

    [Theory]
    [InlineData(".model small", ".model small", 1)]
    [InlineData("; .model small\n.CODE", ".CODE", 2)]
    [InlineData("start:\n  assume cs:code", "assume cs:code", 2)]
    [InlineData("main proc near", "main proc near", 1)]
    [InlineData("main endp", "main endp", 1)]
    [InlineData("code segment", "code segment", 1)]
    [InlineData("mov ax, @data", "@data", 1)]
    public void MasmHint_finds_masm_only_constructs(string source, string construct, int line)
    {
        Assert.Equal((construct, line), MasmHint.Find(source));
    }

    [Theory]
    [InlineData("org 100h\nsegment .text\nsection .data\nmsg db 'proc', '$'\n")]
    [InlineData("mov ax, 1 ; .model small is MASM\n")]
    [InlineData("%macro proc 1\n%endmacro\n")]
    public void MasmHint_ignores_nasm_code_and_comments(string source)
    {
        Assert.Null(MasmHint.Find(source));
    }

    [Fact]
    public async Task No_masm_hint_when_the_build_succeeds()
    {
        BuildResult result = await Build(Assembler(), null, "org 100h\nint 20h\n; .model small\n");

        Assert.Empty(result.Diagnostics);
    }

    // ---- AC-020: .COM size limit ----

    [Fact]
    public async Task AC_020_an_image_over_65280_bytes_fails_with_the_size_limit_message()
    {
        BuildResult result = await Build(Assembler(), null, "org 100h\ntimes 65281 db 90h\n");

        Assert.Equal(BuildOutcome.Failed, result.Outcome);
        Assert.True(result.Image.IsEmpty);
        Diagnostic limit = Assert.Single(result.Diagnostics);
        Assert.Equal(("BLD006", DiagnosticSeverity.Error), (limit.Code, limit.Severity));
        Assert.Contains("65,281", limit.What, StringComparison.Ordinal);
        Assert.Contains("65,280", limit.What, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_image_of_exactly_65280_bytes_builds()
    {
        BuildResult result = await Build(Assembler(), null, "org 100h\ntimes 65280 db 90h\n");

        Assert.Equal(BuildOutcome.Succeeded, result.Outcome);
        Assert.Equal(65_280, result.Image.Length);
    }

    // ---- AC-055 (catalog part): every message has a code, what, where and next step ----

    [Fact]
    public void Every_catalog_code_has_its_texts_and_no_resource_belongs_to_an_unknown_code()
    {
        ResourceSet resources = BuildMessages.Resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var keys = resources.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);

        foreach (string code in BuildMessages.Catalog.Keys)
        {
            Assert.Matches("^(ASM|BLD)[0-9]{3}$", code);
            Assert.False(string.IsNullOrWhiteSpace(keys.GetValueOrDefault(code + "_Next")), $"{code}_Next is missing.");
            if (!BuildMessages.AssemblerTextCodes.Contains(code))
            {
                Assert.False(string.IsNullOrWhiteSpace(keys.GetValueOrDefault(code + "_What")), $"{code}_What is missing.");
            }
        }

        Assert.All(keys.Keys, key => Assert.Contains(key.Split('_')[0], BuildMessages.Catalog.Keys));
    }

    [Fact]
    public void Every_catalog_message_can_be_created_with_its_arguments()
    {
        object?[] args = ["text", 2];
        foreach (string code in BuildMessages.Catalog.Keys.Except(BuildMessages.AssemblerTextCodes))
        {
            Diagnostic message = BuildMessages.Create(code, null, null, args);

            Assert.Equal(code, message.Code);
            Assert.False(string.IsNullOrWhiteSpace(message.What));
            Assert.False(string.IsNullOrWhiteSpace(message.NextStep));
        }
    }
}
