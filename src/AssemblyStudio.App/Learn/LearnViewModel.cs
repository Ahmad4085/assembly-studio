using System.Collections.ObjectModel;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Content;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Learn;

/// <summary>One entry of the Learn sidebar.</summary>
public sealed partial class LearnItemViewModel : ObservableObject
{
    /// <summary>Wraps <paramref name="item"/>.</summary>
    public LearnItemViewModel(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Item = item;
    }

    /// <summary>The catalog item.</summary>
    public ContentItem Item { get; }

    /// <summary>The title.</summary>
    public string Title => Item.Title;

    /// <summary>The one-sentence summary.</summary>
    public string Summary => Item.Summary;

    /// <summary>Whether the tree shows it selected.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>A group of the Learn sidebar: Samples, Tutorials, Reference or Help.</summary>
public sealed partial class LearnSectionViewModel : ObservableObject
{
    /// <summary>Creates the section for <paramref name="kind"/> with its items.</summary>
    public LearnSectionViewModel(ContentKind kind, IEnumerable<LearnItemViewModel> items)
    {
        Kind = kind;
        Items = [.. items];
    }

    /// <summary>The kind of content in this section.</summary>
    public ContentKind Kind { get; }

    /// <summary>The section heading.</summary>
    public string Header => Kind switch
    {
        ContentKind.Sample => Strings.LearnSamples,
        ContentKind.Tutorial => Strings.LearnTutorials,
        ContentKind.Reference => Strings.LearnReference,
        _ => Strings.LearnHelp,
    };

    /// <summary>The items, in catalog order.</summary>
    public ReadOnlyCollection<LearnItemViewModel> Items { get; }

    /// <summary>Whether the section has no items (its empty-state text shows).</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>What the section shows while empty.</summary>
    public string EmptyText => Kind switch
    {
        ContentKind.Sample => Strings.LearnNoSamples,
        ContentKind.Tutorial => Strings.LearnNoTutorials,
        ContentKind.Reference => Strings.LearnNoReference,
        _ => Strings.LearnNoHelp,
    };

    /// <summary>Whether the tree shows it expanded.</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    /// <summary>Whether the tree shows it selected.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// The Learn sidebar (UI-015): the content catalog grouped into Samples, Tutorials, Reference and Help, navigable with
/// the keyboard (a tree: arrows move, Enter opens). Selecting a tutorial, reference or help page shows its text, marked
/// as explanation, never as machine state (FR-072). Opening a sample creates an untitled copy in the editor; the
/// built-in original can't be changed (FR-069).
/// </summary>
public sealed partial class LearnViewModel : ObservableObject
{
    private readonly IContentCatalog _catalog;
    private readonly WorkspaceViewModel _workspace;

    /// <summary>Builds the sidebar from <paramref name="catalog"/>; samples open into <paramref name="workspace"/>.</summary>
    public LearnViewModel(IContentCatalog catalog, WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(workspace);
        _catalog = catalog;
        _workspace = workspace;
        Sections = [.. Enum.GetValues<ContentKind>().Select(kind =>
            new LearnSectionViewModel(kind, catalog.Items.Where(i => i.Kind == kind).Select(i => new LearnItemViewModel(i))))];
    }

    /// <summary>The sections, in the order Samples, Tutorials, Reference, Help.</summary>
    public ReadOnlyCollection<LearnSectionViewModel> Sections { get; }

    /// <summary>The selected entry, or <see langword="null"/> when a section heading (or nothing) is selected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(IsSampleSelected), nameof(SelectedText))]
    [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
    public partial LearnItemViewModel? SelectedItem { get; set; }

    /// <summary>Whether an entry is selected.</summary>
    public bool HasSelection => SelectedItem is not null;

    /// <summary>Whether the selected entry is a sample (the details show "Open a copy").</summary>
    public bool IsSampleSelected => SelectedItem?.Item.Kind == ContentKind.Sample;

    /// <summary>The selected tutorial, reference or help text; samples show their summary only until opened.</summary>
    public string? SelectedText => SelectedItem is { Item.Kind: not ContentKind.Sample } selected ? _catalog.ReadText(selected.Item) : null;

    /// <summary>Selects what the tree selected: an entry, or a section heading.</summary>
    public void Select(object? treeItem) => SelectedItem = treeItem as LearnItemViewModel;

    /// <summary>Learn ▸ Samples / Tutorials / Reference: selects that section's heading and expands it.</summary>
    public void ShowSection(ContentKind kind)
    {
        foreach (LearnSectionViewModel section in Sections)
        {
            section.IsSelected = section.Kind == kind;
            if (section.Kind == kind)
            {
                section.IsExpanded = true;
            }
        }

        foreach (LearnItemViewModel item in Sections.SelectMany(s => s.Items))
        {
            item.IsSelected = false;
        }

        SelectedItem = null;
    }

    /// <summary>Opens the selected sample as an untitled copy (Enter or "Open a copy").</summary>
    [RelayCommand(CanExecute = nameof(IsSampleSelected))]
    private void OpenSelected()
    {
        if (SelectedItem is { Item.Kind: ContentKind.Sample } sample)
        {
            OpenSample(sample.Item);
        }
    }

    /// <summary>FR-069: a new untitled document holding the sample's text, named after the sample; the original stays
    /// untouched and Save asks where to save the copy.</summary>
    public DocumentViewModel OpenSample(ContentItem sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (sample.Kind != ContentKind.Sample)
        {
            throw new ArgumentException($"'{sample.Id}' is not a sample.", nameof(sample));
        }

        return _workspace.OpenCopy(_catalog.ReadText(sample), sample.FileName);
    }
}
