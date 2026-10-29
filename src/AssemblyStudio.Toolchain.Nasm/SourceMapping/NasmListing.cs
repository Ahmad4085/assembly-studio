using System.Globalization;
using System.Text.RegularExpressions;

namespace AssemblyStudio.Toolchain.Nasm.SourceMapping;

/// <summary>
/// One row of a NASM 3.02 listing (POC-002; the layout is undocumented, so the NASM version is pinned,
/// ADR-0005 §6). Columns 0–5 hold the line number, 7–14 the section-relative offset, 16 onward the bytes,
/// then an optional nesting marker <c>&lt;n&gt;</c> and the source text. The marker is right-aligned
/// around column 35–36 (measured: rows without bytes put <c>&lt;9&gt;</c> and up one column left), so it is
/// found by pattern, not by a fixed column.
/// </summary>
/// <param name="Index">Row index in the listing.</param>
/// <param name="LineNumber">The line number NASM printed (in the file or macro definition the row came from).</param>
/// <param name="Level">Nesting level: 0 for the main file, n for the n-th include/macro/%rep level.</param>
/// <param name="Offset">Section-relative offset, or <see langword="null"/> when the row generated nothing.</param>
/// <param name="ByteCount">Number of bytes the row generated (including reserved space).</param>
/// <param name="Bytes">The bytes; <see langword="null"/> entries are relocated values not final in the listing.</param>
/// <param name="IsReserved">Whether the row reserved space (<c>resb</c>) instead of emitting file bytes.</param>
/// <param name="Source">The source text of the row.</param>
/// <param name="ErrorText">The text of an error row, else <see langword="null"/>.</param>
internal sealed record ListingRow(
    int Index, int LineNumber, int Level, int? Offset, int ByteCount, IReadOnlyList<byte?> Bytes,
    bool IsReserved, string Source, string? ErrorText);

internal static partial class NasmListing
{
    private const int BytesColumn = 16;
    private const int SourceColumn = 40;

    public static IReadOnlyList<ListingRow> Parse(string text)
    {
        var rows = new List<ListingRow>();

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length < 6 || !int.TryParse(line.AsSpan(0, 6), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lineNumber))
            {
                continue;
            }

            if (line.Length > 34 && line.AsSpan(BytesColumn, 18).Trim('*').IsEmpty)
            {
                rows.Add(new ListingRow(rows.Count, lineNumber, 0, null, 0, [], false, string.Empty, line[34..].Trim()));
                continue;
            }

            int? offset = line.Length >= 15 && int.TryParse(line.AsSpan(7, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)
                ? value
                : null;

            int level = 0;
            string field;
            string source;
            Match marker = LevelMarker().Match(line);
            if (marker.Success)
            {
                level = int.Parse(marker.Groups[1].Value, CultureInfo.InvariantCulture);
                field = line[BytesColumn..marker.Index];
                source = line[(marker.Index + marker.Length)..];
            }
            else
            {
                field = line.Length > BytesColumn ? line[BytesColumn..Math.Min(line.Length, SourceColumn)] : string.Empty;
                source = line.Length > SourceColumn ? line[SourceColumn..] : string.Empty;
            }

            // Long data continues on further rows with the same line number and their own offsets
            // ("-" ends a continued byte field), so each row is a range of its own.
            (List<byte?> bytes, int count, bool reserved) = ParseBytes(field.Trim());
            rows.Add(new ListingRow(rows.Count, lineNumber, level, offset, count, bytes, reserved, source, null));
        }

        return rows;
    }

    /// <summary>Known bytes (null = relocated, final only in the image), total length, and whether the row reserved space.</summary>
    internal static (List<byte?> Bytes, int Count, bool Reserved) ParseBytes(string field)
    {
        var bytes = new List<byte?>();
        bool reserved = false;
        int count = 0;
        foreach (Match token in ByteToken().Matches(field))
        {
            if (token.Groups[1].Success)
            {
                bytes.Add(byte.Parse(token.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
            else if (token.Groups[2].Success || token.Groups[3].Success)
            {
                string relocated = token.Groups[2].Success ? token.Groups[2].Value : token.Groups[3].Value;
                for (int i = 0; i < relocated.Length / 2; i++)
                {
                    bytes.Add(null);
                }
            }
            else if (token.Groups[4].Success)
            {
                int repeat = int.Parse(token.Groups[4].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                List<byte?> unit = [.. bytes];
                for (int i = 1; i < repeat; i++)
                {
                    bytes.AddRange(unit);
                }
            }
            else if (token.Groups[5].Success)
            {
                reserved = true;
                count += int.Parse(token.Groups[5].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
        }

        return (bytes, count + bytes.Count, reserved);
    }

    [GeneratedRegex(@"([0-9A-F]{2})|\[([0-9A-F]+)\]|\(([0-9A-F]+)\)|<rep ([0-9A-F]+)h>|<res ([0-9A-F]+)h>", RegexOptions.CultureInvariant)]
    private static partial Regex ByteToken();

    /// <summary>The nesting marker: <c>&lt;n&gt;</c> starting in columns 30–38, followed by a space or the line end.</summary>
    [GeneratedRegex(@"(?<=^.{30,38})<(\d+)>(?= |$)", RegexOptions.CultureInvariant)]
    private static partial Regex LevelMarker();
}

/// <summary>The parts of a NASM bin-format map (<c>[map all]</c>) the source map needs: origin, section bases, symbols.</summary>
internal sealed record NasmMapFile(int Origin, IReadOnlyDictionary<string, int> SectionStarts, IReadOnlyDictionary<string, int> Symbols)
{
    public static NasmMapFile Parse(string text)
    {
        List<string> lines = [.. text.Split('\n').Select(l => l.TrimEnd('\r'))];
        int origin = 0;
        var sections = new Dictionary<string, int>(StringComparer.Ordinal);
        var symbols = new Dictionary<string, int>(StringComparer.Ordinal);
        string mode = string.Empty;
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (line.StartsWith("-- Program origin", StringComparison.Ordinal))
            {
                while (++i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
                {
                }

                if (i == lines.Count)
                {
                    throw new InvalidDataException("The map has no program origin value.");
                }

                origin = Hex(lines[i].Trim());
                continue;
            }

            if (line.StartsWith("-- Sections (summary)", StringComparison.Ordinal))
            {
                mode = "summary";
                continue;
            }

            if (line.StartsWith("-- Symbols", StringComparison.Ordinal))
            {
                mode = "symbols";
                continue;
            }

            if (line.StartsWith("--", StringComparison.Ordinal))
            {
                mode = mode == "symbols" ? "symbols" : string.Empty;
                continue;
            }

            string[] columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (mode == "summary" && columns.Length == 6 && columns[0] != "Vstart")
            {
                sections[columns[5]] = Hex(columns[0]);
            }
            else if (mode == "symbols" && columns.Length == 3 && columns[0] != "Real")
            {
                symbols[columns[2]] = Hex(columns[0]);
            }
        }

        if (sections.Count == 0)
        {
            throw new InvalidDataException("The map lists no sections.");
        }

        return new NasmMapFile(origin, sections, symbols);
    }

    private static int Hex(string text) =>
        int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new InvalidDataException($"'{text}' in the map is not a hexadecimal number.");
}
