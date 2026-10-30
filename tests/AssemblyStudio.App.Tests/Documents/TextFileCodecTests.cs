using System.Text;
using AssemblyStudio.App.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>TASK-062, FR-019/FR-020: encoding and line-ending detection, and saving what was opened.</summary>
public class TextFileCodecTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    [Theory]
    [InlineData("mov ax, 1\r\nint 20h\r\n", false, TextEncodingKind.Utf8, LineEnding.CrLf)]
    [InlineData("mov ax, 1\nint 20h\n", true, TextEncodingKind.Utf8Bom, LineEnding.Lf)]
    [InlineData("; café ✓\r\n", false, TextEncodingKind.Utf8, LineEnding.CrLf)]
    [InlineData("no line break", false, TextEncodingKind.Utf8, LineEnding.CrLf)]
    [InlineData("", false, TextEncodingKind.Utf8, LineEnding.CrLf)]
    [InlineData("a\r\nb\nc\r\n", false, TextEncodingKind.Utf8, LineEnding.Mixed)]
    [InlineData("a\rb\r", false, TextEncodingKind.Utf8, LineEnding.Mixed)]
    public void Detects_utf8_bom_and_line_endings_and_saves_the_same_bytes(string text, bool withBom, TextEncodingKind kind, LineEnding lineEnding)
    {
        byte[] bytes = withBom ? [.. Bom, .. Encoding.UTF8.GetBytes(text)] : Encoding.UTF8.GetBytes(text);

        (string decoded, DocumentEncoding encoding, LineEnding detected) = TextFileCodec.Decode(bytes);

        Assert.Equal(text, decoded);
        Assert.Equal(kind, encoding.Kind);
        Assert.Equal(lineEnding, detected);
        Assert.Equal(bytes, TextFileCodec.Encode(decoded, encoding, detected)); // unchanged on save
    }

    [Fact]
    public void Bytes_that_are_not_utf8_open_with_the_ansi_code_page_and_save_back_identically()
    {
        byte[] bytes = [.. Encoding.ASCII.GetBytes("; caf"), 0xE9, .. Encoding.ASCII.GetBytes("\r\n")]; // é in Windows-1252

        (string text, DocumentEncoding encoding, LineEnding lineEnding) = TextFileCodec.Decode(bytes);

        Assert.Equal(TextEncodingKind.Ansi, encoding.Kind);
        Assert.NotEqual(0, encoding.CodePage);
        Assert.StartsWith("; caf", text, StringComparison.Ordinal);
        Assert.Equal(bytes, TextFileCodec.Encode(text, encoding, lineEnding));
        Assert.StartsWith("Windows-", encoding.DisplayName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LineEnding.Lf, "a\nb\nc\n")]
    [InlineData(LineEnding.CrLf, "a\r\nb\r\nc\r\n")]
    public void Lines_added_by_the_editor_get_the_files_line_ending(LineEnding fileEnding, string saved)
    {
        string edited = "a\r\nb\nc\r"; // an editor inserts CR LF, the file had the other kind

        byte[] bytes = TextFileCodec.Encode(edited, DocumentEncoding.Utf8, fileEnding);

        Assert.Equal(saved, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void A_mixed_file_is_saved_exactly_as_it_is()
    {
        const string mixed = "a\r\nb\nc";

        Assert.Equal(mixed, Encoding.UTF8.GetString(TextFileCodec.Encode(mixed, DocumentEncoding.Utf8, LineEnding.Mixed)));
    }

    [Fact]
    public void A_bom_file_keeps_its_bom_after_edits()
    {
        byte[] bytes = TextFileCodec.Encode("edited\n", new DocumentEncoding(TextEncodingKind.Utf8Bom, 0), LineEnding.Lf);

        Assert.Equal([.. Bom, .. "edited\n"u8.ToArray()], bytes);
    }
}
