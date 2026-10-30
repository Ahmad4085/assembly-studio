using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Documents;

/// <summary>How a document's bytes are encoded (FR-019).</summary>
public enum TextEncodingKind
{
    /// <summary>UTF-8 without a byte order mark (also plain ASCII). New files use it.</summary>
    Utf8,

    /// <summary>UTF-8 with the EF BB BF byte order mark.</summary>
    Utf8Bom,

    /// <summary>Not valid UTF-8: decoded with the Windows ANSI code page, with a visible notice.</summary>
    Ansi,
}

/// <summary>A document's encoding: the kind and, for <see cref="TextEncodingKind.Ansi"/>, the code page.</summary>
/// <param name="Kind">The encoding kind.</param>
/// <param name="CodePage">The ANSI code page (for example 1252), or 0 for UTF-8.</param>
public readonly record struct DocumentEncoding(TextEncodingKind Kind, int CodePage)
{
    /// <summary>UTF-8 without BOM: the encoding of new files.</summary>
    public static DocumentEncoding Utf8 { get; } = new(TextEncodingKind.Utf8, 0);

    /// <summary>The name shown in the status bar and the encoding notice, such as "UTF-8" or "Windows-1252".</summary>
    public string DisplayName => Kind switch
    {
        TextEncodingKind.Utf8 => Strings.EncodingUtf8,
        TextEncodingKind.Utf8Bom => Strings.EncodingUtf8Bom,
        _ => TextFileCodec.AnsiEncoding(CodePage).WebName switch
        {
            string name when name.StartsWith("windows-", StringComparison.OrdinalIgnoreCase) => "Windows-" + name["windows-".Length..],
            string name => name,
        },
    };
}

/// <summary>A document's line endings (FR-020).</summary>
public enum LineEnding
{
    /// <summary>CR LF (also files without any line break; new files use it).</summary>
    CrLf,

    /// <summary>LF.</summary>
    Lf,

    /// <summary>Both kinds (or lone CR): kept exactly as they are until the user normalizes them.</summary>
    Mixed,
}

/// <summary>
/// Decodes and encodes source files so that saving keeps what was opened (FR-019, FR-020): UTF-8 with or without
/// BOM is detected and kept; bytes that aren't valid UTF-8 are read with the Windows ANSI code page and written back
/// with it. Line endings are detected; on save every line break becomes the file's line ending (an editor may insert
/// CR LF into an LF file), except in a mixed file, which is written exactly as it is until the user normalizes it.
/// </summary>
public static partial class TextFileCodec
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    static TextFileCodec() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>The decoded text with its encoding and line ending.</summary>
    public static (string Text, DocumentEncoding Encoding, LineEnding LineEnding) Decode(ReadOnlySpan<byte> bytes)
    {
        string text;
        DocumentEncoding encoding;
        if (bytes.StartsWith(Utf8Bom))
        {
            encoding = new DocumentEncoding(TextEncodingKind.Utf8Bom, 0);
            text = DecodeUtf8OrNull(bytes[Utf8Bom.Length..]) ?? DecodeAnsi(bytes, out encoding);
        }
        else
        {
            encoding = DocumentEncoding.Utf8;
            text = DecodeUtf8OrNull(bytes) ?? DecodeAnsi(bytes, out encoding);
        }

        return (text, encoding, DetectLineEnding(text));
    }

    /// <summary>The bytes to save: <paramref name="text"/> with its line breaks set to <paramref name="lineEnding"/>
    /// (unless mixed) and encoded as <paramref name="encoding"/>.</summary>
    public static byte[] Encode(string text, DocumentEncoding encoding, LineEnding lineEnding)
    {
        ArgumentNullException.ThrowIfNull(text);
        string normalized = lineEnding == LineEnding.Mixed ? text : WithLineEnding(text, lineEnding);

        return encoding.Kind switch
        {
            TextEncodingKind.Utf8Bom => [.. Utf8Bom, .. Encoding.UTF8.GetBytes(normalized)],
            TextEncodingKind.Ansi => AnsiEncoding(encoding.CodePage).GetBytes(normalized),
            _ => Encoding.UTF8.GetBytes(normalized),
        };
    }

    /// <summary><paramref name="text"/> with every line break (CR LF, LF or a lone CR) replaced by <paramref name="lineEnding"/>.</summary>
    public static string WithLineEnding(string text, LineEnding lineEnding)
    {
        ArgumentNullException.ThrowIfNull(text);
        return LineBreak().Replace(text, lineEnding == LineEnding.Lf ? "\n" : "\r\n");
    }

    /// <summary>CR LF when every line break is CR LF (or there are none), LF when every one is LF, else mixed.</summary>
    public static LineEnding DetectLineEnding(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int crlf = 0;
        int lf = 0;
        int cr = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    crlf++;
                    i++;
                }
                else
                {
                    cr++;
                }
            }
            else if (text[i] == '\n')
            {
                lf++;
            }
        }

        return (crlf, lf, cr) switch
        {
            (_, 0, 0) => LineEnding.CrLf,
            (0, _, 0) => LineEnding.Lf,
            _ => LineEnding.Mixed,
        };
    }

    /// <summary>The Windows ANSI code page of <paramref name="codePage"/>, falling back to Windows-1252.</summary>
    internal static Encoding AnsiEncoding(int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.GetEncoding(1252);
        }
    }

    private static string? DecodeUtf8OrNull(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static string DecodeAnsi(ReadOnlySpan<byte> bytes, out DocumentEncoding encoding)
    {
        int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
        if (codePage is 65001 or 0)
        {
            codePage = 1252; // a UTF-8 system locale has no ANSI code page to fall back to
        }

        encoding = new DocumentEncoding(TextEncodingKind.Ansi, codePage);
        return AnsiEncoding(codePage).GetString(bytes);
    }

    [GeneratedRegex("\r\n|\r|\n", RegexOptions.CultureInvariant)]
    private static partial Regex LineBreak();
}
