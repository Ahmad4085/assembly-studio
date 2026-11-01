using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Panels.Problems;

/// <summary>One row of the Problems panel (UI-011): severity glyph, code, message, location and next step.</summary>
/// <param name="Diagnostic">The build message.</param>
/// <param name="Document">The document the message is about, when it has a line in one; activating the row jumps there.</param>
public sealed record ProblemItem(Diagnostic Diagnostic, DocumentViewModel? Document)
{
    /// <summary>⨯ ▲ ⓘ.</summary>
    public string Glyph => Diagnostic.Severity switch
    {
        DiagnosticSeverity.Error => "⨯",
        DiagnosticSeverity.Warning => "▲",
        _ => "ⓘ",
    };

    /// <summary>"Error", "Warning" or "Information": the severity in words, so it doesn't depend on the glyph's color.</summary>
    public string SeverityText => Diagnostic.Severity switch
    {
        DiagnosticSeverity.Error => Strings.SeverityError,
        DiagnosticSeverity.Warning => Strings.SeverityWarning,
        _ => Strings.SeverityInfo,
    };

    /// <summary>"hello.asm:7", "Line 7" or empty for a message about the whole build.</summary>
    public string Location => Diagnostic.Line is not int line
        ? string.Empty
        : Diagnostic.File is { } file
            ? $"{Path.GetFileName(file)}:{line.ToString(CultureInfo.CurrentCulture)}"
            : string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.ProblemsLine), line);

    /// <summary>Whether activating the row moves the caret to a line.</summary>
    public bool CanGoTo => Document is not null && Diagnostic.Line is > 0;

    /// <summary>The whole row as one sentence, for screen readers and copying.</summary>
    public string AccessibleText => string.Join(" ", new[] { SeverityText, Diagnostic.Code, Location, Diagnostic.What, Diagnostic.NextStep }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>
/// The Problems panel (UI-011, FR-018, FR-029): the last build's messages, errors first, separate from the console and
/// the runtime messages. "Building…" while a build runs, "No problems" when there are none, and "outdated" once the built
/// document is edited. Activating an entry asks the view to move the caret to its line (AC-013).
/// </summary>
public sealed partial class ProblemsViewModel : ObservableObject
{
    /// <summary>The rows, errors first, then warnings, then information, each in output order.</summary>
    public ObservableCollection<ProblemItem> Items { get; } = [];

    /// <summary>A build is running.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyText), nameof(IsEmpty))]
    public partial bool IsBuilding { get; private set; }

    /// <summary>The built document changed since the build.</summary>
    [ObservableProperty]
    public partial bool IsOutdated { get; set; }

    /// <summary>No assembler could be used: the panel offers Settings (FR-024, AC-016).</summary>
    [ObservableProperty]
    public partial bool OffersSettings { get; private set; }

    /// <summary>Whether the list is empty (the empty-state text shows instead).</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>"Building…" or "No problems".</summary>
    public string EmptyText => IsBuilding ? Strings.ProblemsBuilding : Strings.EmptyProblems;

    /// <summary>Raised when an entry with a line is activated: the view activates the document and moves the caret.</summary>
    public event EventHandler<ProblemItem>? GoToRequested;

    /// <summary>Clears the list and shows "Building…".</summary>
    public void BeginBuild()
    {
        Items.Clear();
        IsOutdated = false;
        OffersSettings = false;
        IsBuilding = true;
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Shows <paramref name="result"/>'s messages for <paramref name="document"/>.</summary>
    public void Show(BuildResult result, DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(document);
        Items.Clear();
        foreach (Diagnostic diagnostic in result.Diagnostics.OrderByDescending(d => d.Severity)) // stable: output order within a severity
        {
            Items.Add(new ProblemItem(diagnostic, IsAbout(diagnostic, document) && diagnostic.Line is > 0 ? document : null));
        }

        OffersSettings = result.Outcome == BuildOutcome.AssemblerMissing;
        IsOutdated = false;
        IsBuilding = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Whether <paramref name="diagnostic"/> is about <paramref name="document"/>: no file (the built document) or the same path.</summary>
    public static bool IsAbout(Diagnostic diagnostic, DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        ArgumentNullException.ThrowIfNull(document);
        return diagnostic.File is null || (document.FilePath is { } path && string.Equals(Path.GetFullPath(diagnostic.File), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Enter or double-click on an entry: go to its line.</summary>
    [RelayCommand]
    private void Activate(ProblemItem? item)
    {
        if (item is { CanGoTo: true })
        {
            GoToRequested?.Invoke(this, item);
        }
    }
}
