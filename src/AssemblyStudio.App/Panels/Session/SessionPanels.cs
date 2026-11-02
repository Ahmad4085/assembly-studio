using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Panels.Session;

/// <summary>
/// The current-instruction strip at the top of the editor (UI-007; FR-037, FR-053): <c>CS:IP</c>, the bytes and the
/// disassembly of the instruction about to execute, "No source mapping for this address" when no line produced it, and the
/// halt's message when the program stopped on it. Shown only while a program is loaded.
/// </summary>
public sealed partial class InstructionStripViewModel : ObservableObject
{
    /// <summary>"0800:0103  B8 34 12  mov ax, 1234h".</summary>
    [ObservableProperty]
    public partial string Text { get; private set; } = string.Empty;

    /// <summary>"No source mapping for this address" or "(macro expansion)", or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? Note { get; private set; }

    /// <summary>The halt's message ("RTE010 Unsupported instruction …"), or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? HaltText { get; private set; }

    /// <summary>A program is loaded: the strip is shown.</summary>
    [ObservableProperty]
    public partial bool IsVisible { get; private set; }

    /// <summary>Running: "Running — not current".</summary>
    [ObservableProperty]
    public partial bool IsNotCurrent { get; private set; }

    /// <summary>Shows the instruction at the pause.</summary>
    public void Show(CurrentInstruction? instruction, RuntimeMessage? halt)
    {
        IsVisible = true;
        IsNotCurrent = false;
        HaltText = halt is null ? null : $"{halt.Code} {halt.What} {halt.Where}. {halt.NextStep}";
        if (instruction is not { } i)
        {
            Text = string.Empty;
            Note = Strings.StripNoInstruction;
            return;
        }

        string bytes = string.Join(" ", i.Bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        Text = $"{i.Cs:X4}:{i.Ip:X4}   {bytes,-18}   {i.Disassembly ?? Strings.StripNoDisassembly}";
        Note = i.SourceLine is null ? Strings.StripNoSourceMapping : i.FromMacroExpansion ? Strings.StripMacroExpansion : null;
    }

    /// <summary>The program runs: the strip says the instruction isn't current.</summary>
    public void MarkRunning()
    {
        IsVisible = true;
        IsNotCurrent = true;
        HaltText = null;
    }

    /// <summary>No program loaded: hidden.</summary>
    public void Clear()
    {
        IsVisible = false;
        IsNotCurrent = false;
        Text = string.Empty;
        Note = null;
        HaltText = null;
    }
}

/// <summary>The kind of a runtime message (its glyph and color; the words carry the meaning too).</summary>
public enum SessionMessageKind
{
    /// <summary>ⓘ An event: loaded, paused, ended.</summary>
    Info,

    /// <summary>▲ Needs a decision: the instruction budget.</summary>
    Warning,

    /// <summary>⨯ The program halted with an error or on something unsupported.</summary>
    Error,
}

/// <summary>One runtime message (UI-012).</summary>
/// <param name="Kind">Its kind.</param>
/// <param name="Text">What happened and where.</param>
/// <param name="Code">The stable code for errors (FR-066), or <see langword="null"/>.</param>
/// <param name="NextStep">The suggested next step, or <see langword="null"/>.</param>
public sealed record SessionMessage(SessionMessageKind Kind, string Text, string? Code = null, string? NextStep = null)
{
    /// <summary>⨯ ▲ ⓘ.</summary>
    public string Glyph => Kind switch
    {
        SessionMessageKind.Error => "⨯",
        SessionMessageKind.Warning => "▲",
        _ => "ⓘ",
    };

    /// <summary>The whole message for screen readers.</summary>
    public string AccessibleName => string.Join(" ", new[] { Kind switch
    {
        SessionMessageKind.Error => Strings.SeverityError,
        SessionMessageKind.Warning => Strings.SeverityWarning,
        _ => Strings.SeverityInfo,
    }, Code, Text, NextStep }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>
/// The runtime messages panel (UI-012; FR-038, FR-040, FR-041, FR-042, FR-065, FR-066): the session's events, separate
/// from the build's problems and the program's console. Errors carry the runtime's code, what, where and next step. When a
/// run reaches its instruction budget the panel offers Continue and Stop.
/// </summary>
public sealed partial class RuntimeMessagesViewModel : ObservableObject
{
    /// <summary>The messages, oldest first.</summary>
    public ObservableCollection<SessionMessage> Items { get; } = [];

    /// <summary>No messages ("Session messages appear here").</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>The session is paused at the instruction budget: Continue and Stop are offered (FR-042).</summary>
    [ObservableProperty]
    public partial bool OffersBudgetChoice { get; set; }

    /// <summary>Adds a message.</summary>
    public void Add(SessionMessage message)
    {
        Items.Add(message);
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Empties the panel (a new session).</summary>
    public void Clear()
    {
        Items.Clear();
        OffersBudgetChoice = false;
        OnPropertyChanged(nameof(IsEmpty));
    }
}

/// <summary>One executed instruction (UI-013).</summary>
/// <param name="Address">"0800:0103".</param>
/// <param name="Bytes">"B8 34 12".</param>
/// <param name="Disassembly">"mov ax, 1234h".</param>
/// <param name="Line">The source line, or <see langword="null"/> when no line produced the address.</param>
public sealed record HistoryRow(string Address, string Bytes, string Disassembly, int? Line)
{
    /// <summary>"line 7" or "—".</summary>
    public string LineText => Line is int line ? string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.HistoryLine), line) : "—";

    /// <summary>"0800:0103 mov ax, 1234h, line 7".</summary>
    public string AccessibleName => $"{Address} {Disassembly}, {LineText}";
}

/// <summary>
/// The execution history panel (UI-013, FR-058): the last executed instructions (at most 1,000), newest last, with address,
/// bytes, disassembly and source line, read from the session at each pause. Activating an entry selects its line (read only).
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    /// <summary>The rows, oldest first.</summary>
    public ObservableCollection<HistoryRow> Rows { get; } = [];

    /// <summary>No instructions executed ("No instructions executed yet").</summary>
    public bool IsEmpty => Rows.Count == 0;

    /// <summary>Running: the rows are those of the last pause (FR-057).</summary>
    [ObservableProperty]
    public partial bool IsNotCurrent { get; private set; }

    /// <summary>Raised when an entry with a source line is activated: the view selects the line in the built document.</summary>
    public event EventHandler<int>? GoToLineRequested;

    /// <summary>Shows the session's history at a pause.</summary>
    /// <param name="history">The session's history, oldest first.</param>
    /// <param name="lineOf">The source line of a CS:IP, or <see langword="null"/>.</param>
    public void Show(IReadOnlyList<HistoryEntry> history, Func<ushort, ushort, int?> lineOf)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(lineOf);
        Rows.Clear();
        foreach (HistoryEntry entry in history)
        {
            Rows.Add(new HistoryRow(
                $"{entry.Cs:X4}:{entry.Ip:X4}",
                string.Join(" ", entry.Bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture))),
                entry.Disassembly ?? Strings.StripNoDisassembly,
                lineOf(entry.Cs, entry.Ip)));
        }

        IsNotCurrent = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>The program runs: dimmed until the next pause.</summary>
    public void MarkRunning() => IsNotCurrent = Rows.Count > 0;

    /// <summary>No session.</summary>
    public void Clear()
    {
        Rows.Clear();
        IsNotCurrent = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Enter or double-click on an entry: select its source line.</summary>
    [RelayCommand]
    private void Activate(HistoryRow? row)
    {
        if (row?.Line is int line)
        {
            GoToLineRequested?.Invoke(this, line);
        }
    }
}

/// <summary>One breakpoint in the breakpoints panel (UI-014).</summary>
/// <param name="Document">The document it is in.</param>
/// <param name="Breakpoint">The breakpoint.</param>
/// <param name="IsHit">The session is paused on it.</param>
public sealed record BreakpointItem(DocumentViewModel Document, EditorBreakpoint Breakpoint, bool IsHit)
{
    /// <summary>"hello.asm:7".</summary>
    public string Location => $"{Document.Title}:{Breakpoint.Line.ToString(CultureInfo.CurrentCulture)}";

    /// <summary>"0800:0103", or the state when it has no address.</summary>
    public string State => (Breakpoint.Binding, Breakpoint.IsEnabled) switch
    {
        (_, false) => Strings.BreakpointStateDisabled,
        (BreakpointBinding.Bound, _) => Breakpoint.Address ?? string.Empty,
        (BreakpointBinding.Unbound, _) => Strings.BreakpointStateUnbound,
        _ => Strings.BreakpointStateNotBuilt,
    };

    /// <summary>"hello.asm:7, 0800:0103, hit".</summary>
    public string AccessibleName => $"{Location}, {State}{(IsHit ? ", " + Strings.BreakpointStateHit : string.Empty)}";
}

/// <summary>
/// The breakpoints panel (UI-014, FR-052): every breakpoint of the open documents with its line, address or unbound state,
/// the one the session is paused on highlighted; enable/disable (Space), remove (Delete), go to the line (Enter).
/// </summary>
public sealed partial class BreakpointsViewModel : ObservableObject
{
    private readonly WorkspaceViewModel _workspace;
    private readonly HashSet<DocumentViewModel> _watched = [];
    private (DocumentViewModel Document, int Line)? _hit;

    /// <summary>Creates the panel over <paramref name="workspace"/>'s documents.</summary>
    public BreakpointsViewModel(WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _workspace = workspace;
        workspace.Documents.CollectionChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>The breakpoints, by document then line.</summary>
    public ObservableCollection<BreakpointItem> Items { get; } = [];

    /// <summary>No breakpoints ("No breakpoints").</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Raised when an entry is activated: the view activates the document and moves the caret to the line.</summary>
    public event EventHandler<BreakpointItem>? GoToRequested;

    /// <summary>Sets the breakpoint the session is paused on (or none).</summary>
    public void SetHit(DocumentViewModel? document, int? line)
    {
        _hit = document is not null && line is int l ? (document, l) : null;
        Refresh();
    }

    /// <summary>Rebuilds the list from the documents.</summary>
    public void Refresh()
    {
        Items.Clear();
        foreach (DocumentViewModel document in _workspace.Documents)
        {
            if (_watched.Add(document))
            {
                document.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(DocumentViewModel.Breakpoints) or nameof(DocumentViewModel.Title))
                    {
                        Refresh();
                    }
                };
            }

            foreach (EditorBreakpoint breakpoint in document.Breakpoints.OrderBy(b => b.Line))
            {
                Items.Add(new BreakpointItem(document, breakpoint, _hit is { } h && h.Document == document && h.Line == breakpoint.Line));
            }
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Space: enables or disables the breakpoint.</summary>
    [RelayCommand]
    private void ToggleEnabled(BreakpointItem? item)
    {
        if (item is not null)
        {
            DocumentViewModel document = item.Document;
            document.Breakpoints = [.. document.Breakpoints.Select(b => b.Line == item.Breakpoint.Line ? b with { IsEnabled = !b.IsEnabled } : b)];
        }

        Refresh();
    }

    /// <summary>Delete: removes the breakpoint.</summary>
    [RelayCommand]
    private void Remove(BreakpointItem? item)
    {
        if (item is not null)
        {
            item.Document.Breakpoints = [.. item.Document.Breakpoints.Where(b => b.Line != item.Breakpoint.Line)];
        }

        Refresh();
    }

    /// <summary>Enter or double-click: go to the line.</summary>
    [RelayCommand]
    private void Activate(BreakpointItem? item)
    {
        if (item is not null)
        {
            GoToRequested?.Invoke(this, item);
        }
    }
}
