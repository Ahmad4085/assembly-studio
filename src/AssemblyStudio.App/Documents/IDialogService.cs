namespace AssemblyStudio.App.Documents;

/// <summary>The answer to "Save changes?" (FR-005).</summary>
public enum SaveChoice
{
    /// <summary>Save, then continue closing.</summary>
    Save,

    /// <summary>Discard the changes and close.</summary>
    DontSave,

    /// <summary>Don't close; the document stays open and unchanged.</summary>
    Cancel,
}

/// <summary>
/// The dialogs the document workflow needs (UI-019), behind an interface so view models stay testable: the WPF
/// implementation shows file dialogs and <c>MessageDialog</c>; tests answer them directly.
/// </summary>
public interface IDialogService
{
    /// <summary>Shows the Open dialog; returns the chosen files (empty when cancelled).</summary>
    IReadOnlyList<string> ShowOpenFiles();

    /// <summary>Shows the Save As dialog; returns the chosen path, or <see langword="null"/> when cancelled.</summary>
    string? ShowSaveFile(string suggestedName, string? initialFolder);

    /// <summary>Shows an Open dialog for a program (the assembler in Settings); returns the chosen path or
    /// <see langword="null"/>.</summary>
    string? ShowOpenExecutable(string? current);

    /// <summary>Asks whether to save <paramref name="documentTitle"/> before closing it.</summary>
    SaveChoice AskSaveChanges(string documentTitle);

    /// <summary>Shows an error: what happened (<paramref name="title"/>), the reason with the path, and the next step.</summary>
    void ShowError(string title, string reason, string nextStep);

    /// <summary>After an unexpected exit, asks whether to restore <paramref name="documents"/> (UI-021). Returns
    /// <see langword="true"/> for Restore, <see langword="false"/> for Discard.</summary>
    bool AskRestore(IReadOnlyList<Recovery.RecoveredDocument> documents);

    /// <summary>Asks a yes/no question whose "yes" button reads <paramref name="confirmLabel"/>; the safe answer is no.</summary>
    bool Confirm(string title, string message, string confirmLabel, string cancelLabel);
}
