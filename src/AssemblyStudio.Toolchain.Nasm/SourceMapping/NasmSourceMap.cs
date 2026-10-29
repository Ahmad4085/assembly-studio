using System.Text.RegularExpressions;
using AssemblyStudio.Core;
using LineMapping = AssemblyStudio.Core.SourceMapping;

namespace AssemblyStudio.Toolchain.Nasm.SourceMapping;

/// <summary>A contiguous range of the image produced by one listing row.</summary>
internal sealed record MappedRange(
    int Address, int Length, LineMapping Mapping, string Section, IReadOnlyList<byte?> Bytes, bool IsReserved, int ListingIndex);

/// <summary>
/// The address ↔ source mapping of a NASM build, built from the listing and the map (POC-002, ADR-0005 §5).
/// <list type="bullet">
///   <item><description>Macro-expanded bytes map to the line that invoked the outermost macro (flagged as an expansion).</description></item>
///   <item><description><c>%rep</c> bodies map to their own lines (inside a macro: to the macro's invoking line).</description></item>
///   <item><description>Included lines map to the included file, resolved like NASM: from the working directory
///   (the main file's folder), else from the including file's folder.</description></item>
///   <item><description>Section-relative offsets become addresses through the map's section starts; a section switch
///   inside a macro expansion counts, a section line inside a macro <i>definition</i> does not.</description></item>
/// </list>
/// </summary>
internal sealed partial class NasmSourceMap : ISourceMap
{
    private readonly MappedRange[] _byAddress;

    private NasmSourceMap(IReadOnlyList<MappedRange> ranges, NasmMapFile map, IReadOnlyList<ListingRow> rows)
    {
        Ranges = ranges;
        Map = map;
        Rows = rows;
        _byAddress = [.. ranges.Where(r => !r.IsReserved).OrderBy(r => r.Address)];
    }

    /// <summary>All mapped ranges in listing order.</summary>
    public IReadOnlyList<MappedRange> Ranges { get; }

    /// <summary>The parsed map.</summary>
    public NasmMapFile Map { get; }

    /// <summary>The parsed listing.</summary>
    public IReadOnlyList<ListingRow> Rows { get; }

    /// <summary>Builds the source map.</summary>
    /// <param name="listingText">The listing NASM wrote (<c>-l</c>).</param>
    /// <param name="mapText">The map NASM wrote (<c>[map all]</c>).</param>
    /// <param name="assembledFile">The file NASM was given (possibly a temporary copy of the document).</param>
    /// <param name="documentFile">The document's saved path, or <see langword="null"/> when untitled; lines of
    /// <paramref name="assembledFile"/> are reported against it.</param>
    /// <param name="workingDirectory">NASM's working directory (include paths resolve from it first).</param>
    /// <exception cref="InvalidDataException">The listing or map doesn't have the expected form.</exception>
    public static NasmSourceMap Build(string listingText, string mapText, string assembledFile, string? documentFile, string workingDirectory)
    {
        NasmMapFile map = NasmMapFile.Parse(mapText);
        IReadOnlyList<ListingRow> rows = NasmListing.Parse(listingText);
        var ranges = new List<MappedRange>();
        var stack = new Stack<Frame>();
        string assembled = Path.GetFullPath(assembledFile);
        stack.Push(new Frame(0, FrameKind.File, assembled, default));
        string section = ".text";
        int definitionDepth = 0; // inside %macro … %endmacro at the current level: listed, not assembled
        ListingRow? previous = null;

        foreach (ListingRow row in rows)
        {
            if (row.ErrorText is not null)
            {
                continue;
            }

            while (stack.Peek().Level > row.Level)
            {
                stack.Pop();
            }

            if (row.Level > stack.Peek().Level)
            {
                stack.Push(Enter(stack.Peek(), row.Level, previous, workingDirectory));
                definitionDepth = 0;
            }

            string code = StripComment(row.Source);
            if (MacroStart().IsMatch(code))
            {
                definitionDepth++;
            }
            else if (MacroEnd().IsMatch(code))
            {
                definitionDepth = Math.Max(0, definitionDepth - 1);
            }
            else if (definitionDepth == 0 && SectionLine().Match(code) is { Success: true } switched)
            {
                section = switched.Groups["name"].Value;
            }

            if (row.Offset is int offset && row.ByteCount > 0)
            {
                if (!map.SectionStarts.TryGetValue(section, out int start))
                {
                    throw new InvalidDataException($"Section '{section}' from the listing is missing in the map.");
                }

                Frame frame = stack.Peek();
                ranges.Add(new MappedRange(start + offset, row.ByteCount, MappingOf(frame, row.LineNumber, assembled, documentFile),
                    section, row.Bytes, row.IsReserved, row.Index));
            }

            previous = row;
        }

        return new NasmSourceMap(ranges, map, rows);
    }

    /// <inheritdoc />
    public LineMapping? Find(ushort offset)
    {
        int low = 0;
        int high = _byAddress.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            MappedRange range = _byAddress[mid];
            if (offset < range.Address)
            {
                high = mid - 1;
            }
            else if (offset >= range.Address + range.Length)
            {
                low = mid + 1;
            }
            else
            {
                return range.Mapping;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public ushort? FirstOffsetOf(string? file, int line)
    {
        int? first = null;
        foreach (MappedRange range in _byAddress)
        {
            SourceLocation location = range.Mapping.Location;
            if (location.Line == line && SameFile(location.File, file) && (first is null || range.Address < first))
            {
                first = range.Address;
            }
        }

        return first is int address ? (ushort)address : null;
    }

    private static bool SameFile(string? a, string? b) =>
        a is null || b is null ? a == b : string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static Frame Enter(Frame parent, int level, ListingRow? previous, string workingDirectory)
    {
        string trigger = previous is null ? string.Empty : StripComment(previous.Source);
        if (IncludeLine().Match(trigger) is { Success: true } include)
        {
            string relative = include.Groups["path"].Value;
            string fromWorkingDirectory = Path.GetFullPath(Path.Combine(workingDirectory, relative));
            string fromIncluder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parent.CurrentFile)!, relative));
            return new Frame(level, FrameKind.File, File.Exists(fromWorkingDirectory) ? fromWorkingDirectory : fromIncluder, default);
        }

        if (RepEnd().IsMatch(trigger))
        {
            // %rep expansions keep their own line numbers; inside a macro they still belong to the invoking line.
            return parent.Kind == FrameKind.Macro
                ? parent with { Level = level }
                : new Frame(level, FrameKind.Rep, parent.CurrentFile, default);
        }

        SourceLocation invoker = parent.Kind == FrameKind.Macro
            ? parent.Invoker
            : new SourceLocation(parent.CurrentFile, previous?.LineNumber ?? 0);
        return new Frame(level, FrameKind.Macro, parent.CurrentFile, invoker);
    }

    private static LineMapping MappingOf(Frame frame, int lineNumber, string assembled, string? document)
    {
        SourceLocation location = frame.Kind == FrameKind.Macro ? frame.Invoker : new SourceLocation(frame.CurrentFile, lineNumber);
        string? file = string.Equals(location.File, assembled, StringComparison.OrdinalIgnoreCase) ? document : location.File;
        return new LineMapping(location with { File = file }, frame.Kind == FrameKind.Macro);
    }

    private static string StripComment(string source)
    {
        int comment = source.IndexOf(';', StringComparison.Ordinal);
        return comment >= 0 ? source[..comment] : source;
    }

    private enum FrameKind
    {
        File,
        Rep,
        Macro,
    }

    /// <summary>One nesting level. <see cref="CurrentFile"/> is the file whose lines the level lists (for a macro,
    /// the file it was invoked from); <see cref="Invoker"/> is the macro's invoking line.</summary>
    private sealed record Frame(int Level, FrameKind Kind, string CurrentFile, SourceLocation Invoker);

    [GeneratedRegex(@"^\s*%include\s+[""<'](?<path>[^""'>]+)[""'>]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IncludeLine();

    [GeneratedRegex(@"^\s*\[?\s*(?:section|segment)\s+(?<name>[^\s\],]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SectionLine();

    [GeneratedRegex(@"^\s*%(?:i|r|ir)?macro\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MacroStart();

    [GeneratedRegex(@"^\s*%endmacro\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MacroEnd();

    [GeneratedRegex(@"^\s*%endrep\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RepEnd();
}
