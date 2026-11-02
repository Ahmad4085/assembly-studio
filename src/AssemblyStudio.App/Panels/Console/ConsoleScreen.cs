using System.Text;

namespace AssemblyStudio.App.Panels.Console;

/// <summary>What one <see cref="ConsoleScreen.Write"/> changed, so a view can update only that part.</summary>
/// <param name="RemovedLines">Lines removed from the top to stay within the cap.</param>
/// <param name="FirstChangedLine">The first line (after the removal) whose text changed; it and every line after it are new.</param>
/// <param name="Bells">BEL characters in the written text.</param>
public readonly record struct ConsoleChange(int RemovedLines, int FirstChangedLine, int Bells);

/// <summary>
/// The program console's text as a DOS teletype shows it (UI-010, FR-063): printable characters overwrite at the cursor,
/// CR returns to the start of the line, LF moves down one line (keeping the column, as the BIOS does), BS moves left one
/// column without erasing, TAB moves to the next multiple of 8, and BEL is counted for a visual signal instead of being
/// shown. Other control codes are shown as their code page 437 symbols. A line wraps after column 80, as on the DOS text
/// screen (80 × 25; D-019). The cursor is always on the last line (the MVP DOS services have no cursor positioning). The text
/// is capped (NFR-015, AC-045): the oldest output is trimmed and counted.
/// </summary>
public sealed class ConsoleScreen
{
    /// <summary>The screen width: after column 80 the BIOS continues on the next line (D-019).</summary>
    public const int Width = 80;

    /// <summary>The default cap: 1,048,576 characters, the size of the console device's buffer (NFR-015).</summary>
    public const int DefaultCapacity = 1024 * 1024;

    // Code page 437 symbols for the control codes 00h–1Fh (00h shows as a space).
    private const string ControlGlyphs = " ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼";

    private readonly List<StringBuilder> _lines = [new()];
    private int _column;
    private long _length;

    /// <summary>Creates an empty screen.</summary>
    /// <param name="capacity">The most characters kept (line breaks count as one).</param>
    public ConsoleScreen(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
    }

    /// <summary>The most characters kept.</summary>
    public int Capacity { get; }

    /// <summary>The number of lines (at least one).</summary>
    public int LineCount => _lines.Count;

    /// <summary>The cursor column on the last line (0-based).</summary>
    public int Column => _column;

    /// <summary>Characters trimmed from the start since the last <see cref="Clear"/>.</summary>
    public long TrimmedCharacters { get; private set; }

    /// <summary>Whether older output was trimmed.</summary>
    public bool HasTrimmed => TrimmedCharacters > 0;

    /// <summary>BEL characters received since the last <see cref="Clear"/>.</summary>
    public int BellCount { get; private set; }

    /// <summary>Whether nothing was written since the last <see cref="Clear"/>.</summary>
    public bool IsEmpty => _length == 0 && _lines.Count == 1;

    /// <summary>The whole text, lines separated by <c>\n</c>.</summary>
    public string Text => string.Join("\n", _lines);

    /// <summary>The text of line <paramref name="index"/>.</summary>
    public string Line(int index) => _lines[index].ToString();

    /// <summary>The text from line <paramref name="first"/> to the end, lines separated by <c>\n</c>.</summary>
    public string TextFrom(int first) => string.Join("\n", _lines.Skip(first));

    /// <summary>Applies program output (code page 437 decoded) and reports what changed.</summary>
    public ConsoleChange Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int first = _lines.Count - 1;
        int bells = 0;
        foreach (char c in text)
        {
            switch (c)
            {
                case '\r':
                    _column = 0;
                    break;
                case '\n':
                    _lines.Add(new StringBuilder().Append(' ', _column));
                    _length += 1 + _column;
                    break;
                case '\b':
                    if (_column > 0)
                    {
                        _column--;
                    }

                    break;
                case '\t':
                    do
                    {
                        Put(' ');
                    }
                    while (_column % 8 != 0);
                    break;
                case '\a':
                    bells++;
                    break;
                default:
                    Put(c < 0x20 ? ControlGlyphs[c] : c == 0x7F ? '⌂' : c);
                    break;
            }
        }

        BellCount += bells;
        int removed = Trim();
        return new ConsoleChange(removed, Math.Max(0, first - removed), bells);
    }

    /// <summary>Counts output that was dropped before it reached the screen (a flood between two updates).</summary>
    public void NoteTrimmed(long characters) => TrimmedCharacters += characters;

    /// <summary>Empties the screen (Run, Reset; FR-063).</summary>
    public void Clear()
    {
        _lines.Clear();
        _lines.Add(new StringBuilder());
        _column = 0;
        _length = 0;
        TrimmedCharacters = 0;
        BellCount = 0;
    }

    private void Put(char c)
    {
        StringBuilder line = _lines[^1];
        if (_column < line.Length)
        {
            line[_column] = c; // overwrite after CR or BS
        }
        else
        {
            line.Append(c);
            _length++;
        }

        _column++;
        if (_column == Width)
        {
            _lines.Add(new StringBuilder()); // the BIOS wraps to the start of the next line (D-019)
            _length++;
            _column = 0;
        }
    }

    private int Trim()
    {
        int removed = 0;
        while (_length > Capacity && _lines.Count > 1)
        {
            int length = _lines[0].Length + 1;
            _lines.RemoveAt(0);
            _length -= length;
            TrimmedCharacters += length;
            removed++;
        }

        if (_length > Capacity)
        {
            // One line longer than the cap: keep its end.
            int cut = (int)(_length - Capacity);
            _lines[0].Remove(0, cut);
            _column = Math.Max(0, _column - cut);
            _length -= cut;
            TrimmedCharacters += cut;
        }

        return removed;
    }
}
