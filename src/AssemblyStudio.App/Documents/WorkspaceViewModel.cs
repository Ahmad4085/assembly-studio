using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using AssemblyStudio.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Documents;

/// <summary>
/// The open documents and the file workflow (C01, UI-003, UI-019, UI-020): New (empty or the .COM template), Open
/// (dialog, drag-and-drop, recent files), Save and Save As (atomic), and Close with the unsaved-changes prompt
/// (FR-002–FR-008). File errors are shown with the path and the reason and never lose the document's text.
/// </summary>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    /// <summary>Entries in the recent-files list (FR-007: 10).</summary>
    public const int RecentFilesCapacity = 10;

    private readonly IDialogService _dialogs;
    private readonly Logging.ILog _log;
    private readonly IRecentFilesStore _recentStore;
    private int _untitledCount;

    /// <summary>Creates the workspace.</summary>
    public WorkspaceViewModel(IDialogService dialogs, IRecentFilesStore recentStore, Logging.ILog? log = null)
    {
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(recentStore);
        _log = log ?? Logging.NullLog.Instance;
        _dialogs = dialogs;
        _recentStore = recentStore;
        foreach (string file in recentStore.Load().Take(RecentFilesCapacity))
        {
            RecentFiles.Add(file);
        }

        Documents.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasDocuments));
            CloseTabCommand.NotifyCanExecuteChanged();
        };
        RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));
    }

    /// <summary>The open documents, in tab order.</summary>
    public ObservableCollection<DocumentViewModel> Documents { get; } = [];

    /// <summary>Recently opened or saved files, most recent first.</summary>
    public ObservableCollection<string> RecentFiles { get; } = [];

    /// <summary>The document in the active tab (build and run use it, FR-006).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
    public partial DocumentViewModel? ActiveDocument { get; set; }

    /// <summary>Whether any document is open; otherwise the welcome page shows (UI-020).</summary>
    public bool HasDocuments => Documents.Count > 0;

    /// <summary>Whether the recent-files list has entries.</summary>
    public bool HasRecentFiles => RecentFiles.Count > 0;

    /// <summary>Creates an empty untitled document (UTF-8, CR LF) and activates it.</summary>
    public DocumentViewModel New() => Add(new DocumentViewModel(string.Empty, null, DocumentEncoding.Utf8, LineEnding.CrLf, ++_untitledCount));

    /// <summary>Creates an untitled document from the .COM template (FR-002).</summary>
    public DocumentViewModel NewFromTemplate() => Add(new DocumentViewModel(Strings.TemplateCom, null, DocumentEncoding.Utf8, LineEnding.CrLf, ++_untitledCount));

    /// <summary>Opens <paramref name="text"/> as an untitled copy named after <paramref name="originalName"/> (a sample,
    /// FR-069): not yet saved anywhere, so Save asks for a location; CR LF line endings like a new program.</summary>
    public DocumentViewModel OpenCopy(string text, string originalName)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
        return Add(new DocumentViewModel(text, null, DocumentEncoding.Utf8, LineEnding.CrLf, copyOf: originalName));
    }

    [RelayCommand]
    private void NewFile() => New();

    [RelayCommand]
    private void NewProgram() => NewFromTemplate();

    /// <summary>Opens a document recovered after a crash (TASK-063): its unsaved text, as an unsaved document of its
    /// file (or a new untitled one), with the file's encoding and line endings.</summary>
    public DocumentViewModel Restore(Recovery.RecoveredDocument recovered)
    {
        ArgumentNullException.ThrowIfNull(recovered);
        return Add(new DocumentViewModel(recovered.Text, recovered.FilePath, recovered.Encoding, recovered.LineEnding,
            recovered.FilePath is null ? ++_untitledCount : 1, isDirty: true));
    }

    /// <summary>Shows the Open dialog and opens the chosen files.</summary>
    [RelayCommand]
    public void Open() => OpenFiles(_dialogs.ShowOpenFiles());

    /// <summary>Opens files (Open dialog, drag-and-drop): each in its own tab; an already open file is activated.</summary>
    public void OpenFiles(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        foreach (string path in paths)
        {
            OpenFile(path);
        }
    }

    /// <summary>Opens <paramref name="path"/>; on failure shows the reason and returns <see langword="null"/>.</summary>
    public DocumentViewModel? OpenFile(string path)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            _log.Write(Logging.LogLevel.Warning, $"Open failed: {path}", ex);
            _dialogs.ShowError(Strings.OpenFailedTitle, FileErrors.Describe(ex, path), Strings.OpenFailedNext);
            return null;
        }

        if (Documents.FirstOrDefault(d => string.Equals(d.FilePath, fullPath, StringComparison.OrdinalIgnoreCase)) is { } open)
        {
            Activate(open);
            return open;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(fullPath);
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            _log.Write(Logging.LogLevel.Warning, $"Open failed: {fullPath}", ex);
            _dialogs.ShowError(Strings.OpenFailedTitle, FileErrors.Describe(ex, fullPath), Strings.OpenFailedNext);
            return null;
        }

        (string text, DocumentEncoding encoding, LineEnding lineEnding) = TextFileCodec.Decode(bytes);
        DocumentViewModel document = Add(new DocumentViewModel(text, fullPath, encoding, lineEnding));
        Remember(fullPath);
        return document;
    }

    /// <summary>Opens a recent file; a missing one offers to remove the entry (FR-007).</summary>
    [RelayCommand]
    public void OpenRecent(string path)
    {
        if (!File.Exists(path))
        {
            string message = string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.RecentMissingText), path);
            if (_dialogs.Confirm(Strings.RecentMissingTitle, message, Strings.DialogRemove, Strings.DialogKeep))
            {
                RecentFiles.Remove(path);
                PersistRecent();
            }

            return;
        }

        OpenFile(path);
    }

    /// <summary>Saves the active document (Save As when untitled).</summary>
    [RelayCommand(CanExecute = nameof(HasActiveDocument))]
    public void Save() => SaveDocument(ActiveDocument!);

    /// <summary>Saves the active document under a new name.</summary>
    [RelayCommand(CanExecute = nameof(HasActiveDocument))]
    public void SaveAs() => SaveDocumentAs(ActiveDocument!);

    /// <summary>Saves <paramref name="document"/>: to its file, or via Save As when untitled. Returns whether it was saved.</summary>
    public bool SaveDocument(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.FilePath is null ? SaveDocumentAs(document) : WriteTo(document, document.FilePath);
    }

    /// <summary>Asks for a path and saves <paramref name="document"/> there (the tab is renamed). Returns whether it was saved.</summary>
    public bool SaveDocumentAs(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        string suggested = document.SuggestedFileName;
        string? path = _dialogs.ShowSaveFile(suggested, document.FilePath is null ? null : Path.GetDirectoryName(document.FilePath));
        return path is not null && WriteTo(document, path);
    }

    [RelayCommand(CanExecute = nameof(HasDocuments))]
    private void CloseTab(DocumentViewModel? document) => Close(document);

    /// <summary>Closes a document (the active one when <paramref name="document"/> is null), asking first when it has
    /// unsaved changes (FR-005). Returns whether it was closed.</summary>
    public bool Close(DocumentViewModel? document)
    {
        document ??= ActiveDocument;
        if (document is null || !ConfirmDiscard(document))
        {
            return false;
        }

        int index = Documents.IndexOf(document);
        Documents.Remove(document);
        document.IsActive = false;
        if (ActiveDocument == document)
        {
            Activate(Documents.Count == 0 ? null : Documents[Math.Min(index, Documents.Count - 1)]);
        }

        return true;
    }

    /// <summary>Asks about every document with unsaved changes before the window closes (FR-005). Returns
    /// <see langword="false"/> when the user cancels (the window stays open, every document unchanged).</summary>
    public bool ConfirmCloseAll() => Documents.Where(d => d.IsDirty).ToList().All(ConfirmDiscard);

    /// <summary>Normalizes the document's line endings to CR LF after confirmation (FR-020).</summary>
    [RelayCommand]
    public void NormalizeLineEndings(DocumentViewModel? document)
    {
        document ??= ActiveDocument;
        if (document is null)
        {
            return;
        }

        string message = string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.NormalizeText), document.Title);
        if (_dialogs.Confirm(Strings.NormalizeTitle, message, Strings.DialogNormalize, Strings.DialogCancel))
        {
            document.NormalizeLineEndings();
        }
    }

    private bool HasActiveDocument() => ActiveDocument is not null;

    /// <summary>For a dirty document: Save (and only continue if it saved), Don't Save, or Cancel.</summary>
    private bool ConfirmDiscard(DocumentViewModel document)
    {
        if (!document.IsDirty)
        {
            return true;
        }

        Activate(document);
        return _dialogs.AskSaveChanges(document.Title) switch
        {
            SaveChoice.Save => SaveDocument(document),
            SaveChoice.DontSave => true,
            _ => false,
        };
    }

    private bool WriteTo(DocumentViewModel document, string path)
    {
        try
        {
            AtomicFile.Write(path, document.Encode());
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            _log.Write(Logging.LogLevel.Warning, $"Save failed: {path}", ex);
            _dialogs.ShowError(Strings.SaveFailedTitle, FileErrors.Describe(ex, path), Strings.SaveFailedKept);
            return false;
        }

        document.MarkSaved(Path.GetFullPath(path));
        Remember(document.FilePath!);
        return true;
    }

    private DocumentViewModel Add(DocumentViewModel document)
    {
        Documents.Add(document);
        Activate(document);
        return document;
    }

    private void Activate(DocumentViewModel? document)
    {
        foreach (DocumentViewModel each in Documents)
        {
            each.IsActive = each == document;
        }

        ActiveDocument = document;
    }

    partial void OnActiveDocumentChanged(DocumentViewModel? value)
    {
        foreach (DocumentViewModel each in Documents)
        {
            each.IsActive = each == value;
        }
    }

    private void Remember(string path)
    {
        string? existing = RecentFiles.FirstOrDefault(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentFiles.Remove(existing);
        }

        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > RecentFilesCapacity)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        PersistRecent();
    }

    private void PersistRecent()
    {
        try
        {
            _recentStore.Save([.. RecentFiles]);
        }
        catch (Exception ex) when (FileErrors.IsFileError(ex))
        {
            // The list stays in memory; losing it must not interrupt editing.
            _log.Write(Logging.LogLevel.Warning, "The recent files list could not be saved.", ex);
        }
    }
}
