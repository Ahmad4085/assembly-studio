using System.Globalization;
using System.IO;
using System.Text;
using AssemblyStudio.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssemblyStudio.App.Documents;

/// <summary>
/// One open source document (UI-003): its text, file, encoding, line endings and unsaved state. Encoding and line
/// endings are kept from the file and used again on save (FR-019, FR-020).
/// </summary>
public sealed partial class DocumentViewModel : ObservableObject
{
    private readonly int _untitledNumber;

    /// <summary>Creates a document.</summary>
    /// <param name="text">The content.</param>
    /// <param name="filePath">The file, or <see langword="null"/> for an untitled document.</param>
    /// <param name="encoding">The file's encoding (new files: UTF-8 without BOM).</param>
    /// <param name="lineEnding">The file's line ending (new files: CR LF).</param>
    /// <param name="untitledNumber">1 for "Untitled", n for "Untitled n".</param>
    /// <param name="isDirty">Whether the text differs from the file (a document restored after a crash).</param>
    /// <param name="copyOf">For an untitled copy of built-in content (a sample, FR-069): the original's file name, used for
    /// the title and as the suggested Save As name.</param>
    public DocumentViewModel(
        string text, string? filePath, DocumentEncoding encoding, LineEnding lineEnding, int untitledNumber = 1, bool isDirty = false, string? copyOf = null)
    {
        _text = text;
        CopyOf = copyOf;
        IsDirty = isDirty;
        FilePath = filePath;
        Encoding = encoding;
        LineEnding = lineEnding;
        _untitledNumber = untitledNumber;
    }

    /// <summary>Identifies the document within this run (its autosave file, TASK-063).</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The document text, as edited.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                IsDirty = true;
            }
        }
    }

    private string _text;

    /// <summary>The file, or <see langword="null"/> while untitled.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    [NotifyPropertyChangedFor(nameof(AutomationName))]
    public partial string? FilePath { get; private set; }

    /// <summary>Whether the text differs from the file.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    [NotifyPropertyChangedFor(nameof(AutomationName))]
    public partial bool IsDirty { get; private set; }

    /// <summary>The file's encoding, kept on save.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EncodingNotice))]
    [NotifyPropertyChangedFor(nameof(StatusEncoding))]
    public partial DocumentEncoding Encoding { get; private set; }

    /// <summary>The file's line ending, kept on save.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMixedLineEndings))]
    [NotifyPropertyChangedFor(nameof(StatusLineEnding))]
    public partial LineEnding LineEnding { get; private set; }

    /// <summary>Whether this is the document shown in the editor.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>For an untitled copy of a sample: the sample's file name (FR-069); otherwise <see langword="null"/>.</summary>
    public string? CopyOf { get; }

    /// <summary>The name Save As suggests: the file name, the sample's name, or the title plus ".asm".</summary>
    public string SuggestedFileName => FilePath is not null ? Path.GetFileName(FilePath) : CopyOf ?? Title + ".asm";

    /// <summary>The tab title: the file name, "hello.asm (copy)" for a sample copy, or "Untitled" / "Untitled n".</summary>
    public string Title => FilePath is not null
        ? Path.GetFileName(FilePath)
        : CopyOf is not null
        ? string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.DocSampleCopy), CopyOf)
        : _untitledNumber == 1 ? Strings.DocUntitled : string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.DocUntitledNumbered), _untitledNumber);

    /// <summary>The tab title with the unsaved marker.</summary>
    public string DisplayTitle => IsDirty ? $"{Title} {Strings.DocDirtyMarker}" : Title;

    /// <summary>The tab's name for screen readers ("…, unsaved changes"), since the marker is a symbol.</summary>
    public string AutomationName => IsDirty ? string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.DocDirtyName), Title) : Title;

    /// <summary>The notice for a file that wasn't valid UTF-8 (FR-019), or <see langword="null"/>.</summary>
    public string? EncodingNotice => Encoding.Kind == TextEncodingKind.Ansi
        ? string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.BannerEncoding), Encoding.DisplayName)
        : null;

    /// <summary>Whether the file mixes line endings (a banner offers to normalize them, FR-020).</summary>
    public bool HasMixedLineEndings => LineEnding == LineEnding.Mixed;

    /// <summary>The encoding shown in the status bar.</summary>
    public string StatusEncoding => Encoding.DisplayName;

    /// <summary>The line ending shown in the status bar.</summary>
    public string StatusLineEnding => LineEnding switch
    {
        LineEnding.CrLf => Strings.LineEndingCrLf,
        LineEnding.Lf => Strings.LineEndingLf,
        _ => Strings.LineEndingMixed,
    };

    /// <summary>The bytes to save.</summary>
    public byte[] Encode() => TextFileCodec.Encode(Text, Encoding, LineEnding);

    /// <summary>Records a successful save to <paramref name="path"/>.</summary>
    public void MarkSaved(string path)
    {
        FilePath = path;
        IsDirty = false;
    }

    /// <summary>Converts every line ending to CR LF; the change is saved with the file (FR-020).</summary>
    public void NormalizeLineEndings()
    {
        LineEnding = LineEnding.CrLf;
        Text = TextFileCodec.WithLineEnding(Text, LineEnding.CrLf);
        IsDirty = true;
    }
}
