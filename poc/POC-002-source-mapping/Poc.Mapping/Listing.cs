using System.Globalization;
using System.Text.RegularExpressions;

namespace Poc.Mapping;

/// <summary>
/// One row of a NASM 3.02 listing. Column layout measured in POC-002 (undocumented by NASM):
/// cols 0-5 line number, 7-14 section-relative offset (hex), 16-35 bytes, 36 nesting marker <c>&lt;n&gt;</c>, 40+ source.
/// </summary>
public sealed record ListingRow(
    int Index, int LineNumber, int Level, int? Offset, int ByteCount, IReadOnlyList<byte?> Bytes,
    bool IsReserved, string Source, string? ErrorText);

public static partial class Listing
{
    [GeneratedRegex(@"([0-9A-F]{2})|\[([0-9A-F]+)\]|\(([0-9A-F]+)\)|<rep ([0-9A-F]+)h>|<res ([0-9A-F]+)h>|-$")]
    private static partial Regex ByteToken();

    [GeneratedRegex(@"^<(\d+)>")]
    private static partial Regex LevelMarker();

    public static IReadOnlyList<ListingRow> Parse(string text)
    {
        var rows = new List<ListingRow>();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length < 6 || !int.TryParse(line.AsSpan(0, 6), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lineNo)) continue;

            if (line.Length > 34 && line.AsSpan(16, 18).Trim('*').IsEmpty)
            {
                rows.Add(new ListingRow(rows.Count, lineNo, 0, null, 0, [], false, "", line[34..].Trim()));
                continue;
            }

            int? offset = null;
            if (line.Length >= 15 && int.TryParse(line.AsSpan(7, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int off)) offset = off;
            string field = line.Length > 16 ? line.Substring(16, Math.Min(20, line.Length - 16)).Trim() : "";
            int level = 0;
            if (line.Length > 36) { var m = LevelMarker().Match(line[36..]); if (m.Success) level = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture); }
            string source = line.Length > 40 ? line[40..] : "";

            var (bytes, count, reserved) = ParseBytes(field);
            rows.Add(new ListingRow(rows.Count, lineNo, level, offset, count, bytes, reserved, source, null));
        }
        return rows;
    }

    /// <summary>Known bytes (null = relocated, value not final in the listing), total length, reserved (no file bytes).</summary>
    internal static (List<byte?> Bytes, int Count, bool Reserved) ParseBytes(string field)
    {
        var bytes = new List<byte?>();
        bool reserved = false;
        int count = 0;
        foreach (Match t in ByteToken().Matches(field))
        {
            if (t.Groups[1].Success) bytes.Add(byte.Parse(t.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            else if (t.Groups[2].Success || t.Groups[3].Success)
            {
                string v = t.Groups[2].Success ? t.Groups[2].Value : t.Groups[3].Value;
                for (int i = 0; i < v.Length / 2; i++) bytes.Add(null);
            }
            else if (t.Groups[4].Success)
            {
                int n = int.Parse(t.Groups[4].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var unit = bytes.ToList();
                for (int i = 1; i < n; i++) bytes.AddRange(unit);
            }
            else if (t.Groups[5].Success)
            {
                reserved = true;
                count += int.Parse(t.Groups[5].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
        }
        return (bytes, count + bytes.Count, reserved);
    }
}

/// <summary>Parsed NASM bin-format map file (<c>[map all]</c>).</summary>
public sealed record MapFile(int Origin, IReadOnlyDictionary<string, (int Vstart, int Start)> Sections, IReadOnlyDictionary<string, int> Symbols)
{
    public static MapFile Parse(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        int origin = 0;
        var sections = new Dictionary<string, (int, int)>();
        var symbols = new Dictionary<string, int>();
        string mode = "";
        for (int i = 0; i < lines.Count; i++)
        {
            string l = lines[i];
            if (l.StartsWith("-- Program origin", StringComparison.Ordinal))
            {
                while (string.IsNullOrWhiteSpace(lines[++i])) { }   // a blank line separates header and value
                origin = Hex(lines[i].Trim());
                continue;
            }
            if (l.StartsWith("-- Sections (summary)", StringComparison.Ordinal)) { mode = "summary"; continue; }
            if (l.StartsWith("-- Symbols", StringComparison.Ordinal)) { mode = "symbols"; continue; }
            if (l.StartsWith("--", StringComparison.Ordinal)) { mode = mode == "symbols" ? "symbols" : ""; continue; }
            string[] c = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (mode == "summary" && c.Length == 6 && c[0] != "Vstart") sections[c[5]] = (Hex(c[0]), Hex(c[1]));
            else if (mode == "symbols" && c.Length == 3 && c[0] != "Real") symbols[c[2]] = Hex(c[0]);
        }
        return new MapFile(origin, sections, symbols);
    }

    private static int Hex(string s) => int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
