using System.Globalization;
using System.Text;
using AssemblyStudio.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Editor.Search;

/// <summary>
/// The find and replace bar and Go to Line (UI-004; FR-015, FR-016, FR-017; AC-011), working on the active editor through
/// <see cref="ISearchTarget"/>.
/// <list type="bullet">
///   <item><description>Find next searches from the end of the selection, find previous from its start; both wrap around.
///   Typing in the find box searches from the start of the selection, so the current match stays selected.</description></item>
///   <item><description>Replace replaces the selection when it is a match, then selects the next match; otherwise it only
///   finds the next one.</description></item>
///   <item><description>Replace All replaces every match as one undo step.</description></item>
/// </list>
/// </summary>
public sealed partial class FindReplaceViewModel : ObservableObject
{
    private readonly Func<ISearchTarget?> _target;
    private bool _searching;

    /// <summary>Creates the bar.</summary>
    /// <param name="target">The active editor, or <see langword="null"/> when no document is open.</param>
    public FindReplaceViewModel(Func<ISearchTarget?> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
    }

    /// <summary>The bar wants keyboard focus in its find box (Ctrl+F) or its replace box (Ctrl+H).</summary>
    public event EventHandler<bool>? FocusRequested;

    /// <summary>Asks for a line number: (line count, current line) → the line, or <see langword="null"/> when cancelled. Set by
    /// the view (the Go to Line dialog).</summary>
    public Func<int, int, int?>? AskLine { get; set; }

    /// <summary>The bar is shown.</summary>
    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>The replace row is shown (Ctrl+H).</summary>
    [ObservableProperty]
    public partial bool IsReplaceShown { get; set; }

    /// <summary>The text to find.</summary>
    [ObservableProperty]
    public partial string Query { get; set; } = string.Empty;

    /// <summary>The replacement.</summary>
    [ObservableProperty]
    public partial string Replacement { get; set; } = string.Empty;

    /// <summary>Match case (FR-015).</summary>
    [ObservableProperty]
    public partial bool MatchCase { get; set; }

    /// <summary>Whole word (FR-015).</summary>
    [ObservableProperty]
    public partial bool WholeWord { get; set; }

    /// <summary>"Match 2 of 5", "No matches", "Replaced 3" (announced politely).</summary>
    [ObservableProperty]
    public partial string Status { get; private set; } = string.Empty;

    private SearchOptions Options => new(MatchCase, WholeWord);

    /// <summary>Ctrl+F: opens the bar with the selected text (one line) as the query and focuses the find box.</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void Find() => Open(replace: false);

    /// <summary>Ctrl+H: as Find, with the replace row.</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void OpenReplace() => Open(replace: true);

    /// <summary>F3: the next match after the selection.</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void FindNext()
    {
        if (_target() is { } target)
        {
            Show(target, SearchRules.FindNext(target.Text, Query, target.SelectionStart + target.SelectionLength, Options));
        }
    }

    /// <summary>Shift+F3: the previous match before the selection.</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void FindPrevious()
    {
        if (_target() is { } target)
        {
            Show(target, SearchRules.FindPrevious(target.Text, Query, target.SelectionStart, Options));
        }
    }

    /// <summary>Replaces the selected match, then selects the next one (FR-016).</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void Replace()
    {
        if (_target() is not { } target || Query.Length == 0)
        {
            return;
        }

        int start = target.SelectionStart;
        if (target.SelectionLength == Query.Length && SearchRules.FindAll(target.Text, Query, Options).Contains(start))
        {
            target.Replace(start, Query.Length, Replacement);
            Show(target, SearchRules.FindNext(target.Text, Query, start + Replacement.Length, Options));
            return;
        }

        FindNext();
    }

    /// <summary>Replaces every match, as one undo step (FR-016).</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void ReplaceAll()
    {
        if (_target() is not { } target || Query.Length == 0)
        {
            return;
        }

        IReadOnlyList<int> all = SearchRules.FindAll(target.Text, Query, Options);
        if (all.Count == 0)
        {
            Status = NoMatches();
            return;
        }

        target.ReplaceAll(all, Query.Length, Replacement);
        Status = Format(Strings.FindReplaced, all.Count);
    }

    /// <summary>Esc in the bar: closes it and returns to the editor.</summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        Status = string.Empty;
        _target()?.Focus();
    }

    /// <summary>Ctrl+G: asks for a line and moves the caret to its start (FR-017).</summary>
    [RelayCommand(CanExecute = nameof(HasTarget))]
    private void GoToLine()
    {
        if (_target() is { } target && AskLine?.Invoke(target.LineCount, target.CaretLine) is int line)
        {
            target.GoToLine(Math.Clamp(line, 1, target.LineCount));
        }
    }

    /// <summary>Reads a Go to Line entry: a whole number from 1 to <paramref name="lineCount"/>.</summary>
    public static bool TryParseLine(string? text, int lineCount, out int line) =>
        int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out line) && line >= 1 && line <= lineCount;

    /// <summary>The commands' availability changed (a document was opened or closed).</summary>
    public void Refresh()
    {
        FindCommand.NotifyCanExecuteChanged();
        OpenReplaceCommand.NotifyCanExecuteChanged();
        FindNextCommand.NotifyCanExecuteChanged();
        FindPreviousCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
        GoToLineCommand.NotifyCanExecuteChanged();
    }

    partial void OnQueryChanged(string value) => SearchAsYouType();

    partial void OnMatchCaseChanged(bool value) => SearchAsYouType();

    partial void OnWholeWordChanged(bool value) => SearchAsYouType();

    private bool HasTarget() => _target() is not null;

    private void Open(bool replace)
    {
        if (_target() is not { } target)
        {
            return;
        }

        string selected = target.Text.Substring(target.SelectionStart, target.SelectionLength);
        if (selected.Length > 0 && selected.IndexOfAny(['\r', '\n']) < 0)
        {
            _searching = true; // the selection is the match: don't move it
            Query = selected;
            _searching = false;
        }

        IsReplaceShown = replace;
        IsOpen = true;
        Status = Query.Length == 0 ? string.Empty : Count(target, target.SelectionStart);
        FocusRequested?.Invoke(this, replace && Query.Length > 0);
    }

    private void SearchAsYouType()
    {
        if (_searching || !IsOpen || _target() is not { } target)
        {
            return;
        }

        if (Query.Length == 0)
        {
            Status = string.Empty;
            return;
        }

        Show(target, SearchRules.FindNext(target.Text, Query, target.SelectionStart, Options));
    }

    private void Show(ISearchTarget target, SearchHit? hit)
    {
        if (Query.Length == 0)
        {
            Status = string.Empty;
            return;
        }

        if (hit is not { } found)
        {
            Status = NoMatches();
            return;
        }

        target.SelectRange(found.Offset, Query.Length);
        string status = Count(target, found.Offset);
        Status = found.Wrapped ? $"{status} {Strings.FindWrapped}" : status;
    }

    // "Match 2 of 5" when a match starts at offset, else "5 matches".
    private string Count(ISearchTarget target, int offset)
    {
        IReadOnlyList<int> all = SearchRules.FindAll(target.Text, Query, Options);
        if (all.Count == 0)
        {
            return NoMatches();
        }

        int index = IndexOf(all, offset);
        return index >= 0 ? Format(Strings.FindMatchOf, index + 1, all.Count) : Format(Strings.FindMatches, all.Count);
    }

    private static int IndexOf(IReadOnlyList<int> all, int offset)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == offset)
            {
                return i;
            }
        }

        return -1;
    }

    private string NoMatches() => Format(Strings.FindNoMatches, Query);

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(format), args);
}
