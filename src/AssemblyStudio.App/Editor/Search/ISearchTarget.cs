namespace AssemblyStudio.App.Editor.Search;

/// <summary>The editor a search works on (the active <see cref="CodeEditor"/>); keeps the find bar free of control types.</summary>
public interface ISearchTarget
{
    /// <summary>The document's text.</summary>
    string Text { get; }

    /// <summary>Start of the selection (the caret when nothing is selected).</summary>
    int SelectionStart { get; }

    /// <summary>Length of the selection.</summary>
    int SelectionLength { get; }

    /// <summary>The number of lines.</summary>
    int LineCount { get; }

    /// <summary>The caret's line (1-based).</summary>
    int CaretLine { get; }

    /// <summary>Selects a range and scrolls it into view.</summary>
    void SelectRange(int start, int length);

    /// <summary>Replaces a range (one undo step).</summary>
    void Replace(int start, int length, string text);

    /// <summary>Replaces every range with the same text, as one undo step. The ranges are in order and don't overlap.</summary>
    void ReplaceAll(IReadOnlyList<int> starts, int length, string text);

    /// <summary>Puts the caret at the start of <paramref name="line"/>, scrolls it into view and focuses the editor.</summary>
    void GoToLine(int line);

    /// <summary>Gives the editor keyboard focus.</summary>
    void Focus();
}
