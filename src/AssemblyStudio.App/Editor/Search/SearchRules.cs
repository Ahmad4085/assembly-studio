namespace AssemblyStudio.App.Editor.Search;

/// <summary>How a search matches (FR-015).</summary>
/// <param name="MatchCase">Upper and lower case differ.</param>
/// <param name="WholeWord">A match isn't part of a longer name: no identifier character right before or after it.</param>
public readonly record struct SearchOptions(bool MatchCase, bool WholeWord);

/// <summary>A match found from a position: its offset, and whether the search went past the document's end (or start).</summary>
/// <param name="Offset">The match's offset.</param>
/// <param name="Wrapped">The search wrapped around.</param>
public readonly record struct SearchHit(int Offset, bool Wrapped);

/// <summary>
/// The find and replace rules of the editor (FR-015, FR-016, AC-011), on plain text so they can be tested exactly:
/// matches are ordinal (match case) or ordinal ignoring case; a whole-word match has no NASM identifier character
/// (letters, digits, <c>_ . $ ? @ # ~</c>) right before or after it; matches don't overlap and are taken left to right;
/// find next and previous wrap around the document.
/// </summary>
public static class SearchRules
{
    /// <summary>Every match, left to right, not overlapping.</summary>
    public static IReadOnlyList<int> FindAll(string text, string query, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(query);
        var matches = new List<int>();
        if (query.Length == 0)
        {
            return matches;
        }

        StringComparison comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int at = 0;
        while (at <= text.Length - query.Length && (at = text.IndexOf(query, at, comparison)) >= 0)
        {
            if (!options.WholeWord || IsWholeWord(text, at, query.Length))
            {
                matches.Add(at);
                at += query.Length;
            }
            else
            {
                at++;
            }
        }

        return matches;
    }

    /// <summary>The first match at or after <paramref name="from"/>, else the first in the document (wrapped).</summary>
    public static SearchHit? FindNext(string text, string query, int from, SearchOptions options)
    {
        IReadOnlyList<int> all = FindAll(text, query, options);
        if (all.Count == 0)
        {
            return null;
        }

        foreach (int offset in all)
        {
            if (offset >= from)
            {
                return new SearchHit(offset, false);
            }
        }

        return new SearchHit(all[0], true);
    }

    /// <summary>The last match before <paramref name="before"/>, else the last in the document (wrapped).</summary>
    public static SearchHit? FindPrevious(string text, string query, int before, SearchOptions options)
    {
        IReadOnlyList<int> all = FindAll(text, query, options);
        if (all.Count == 0)
        {
            return null;
        }

        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] < before)
            {
                return new SearchHit(all[i], false);
            }
        }

        return new SearchHit(all[^1], true);
    }

    /// <summary>A NASM identifier character: a whole-word match may not touch one.</summary>
    public static bool IsIdentifierChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '$' or '?' or '@' or '#' or '~';

    private static bool IsWholeWord(string text, int offset, int length) =>
        (offset == 0 || !IsIdentifierChar(text[offset - 1])) && (offset + length == text.Length || !IsIdentifierChar(text[offset + length]));
}
