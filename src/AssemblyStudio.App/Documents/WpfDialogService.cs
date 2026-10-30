using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using AssemblyStudio.App.Shell;
using Microsoft.Win32;

namespace AssemblyStudio.App.Documents;

/// <summary>The WPF dialogs: the Windows Open and Save As dialogs, and <see cref="MessageDialog"/> (UI-019).</summary>
public sealed class WpfDialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    /// <inheritdoc />
    public IReadOnlyList<string> ShowOpenFiles()
    {
        var dialog = new OpenFileDialog { Filter = Strings.FileFilter, Multiselect = true, CheckFileExists = false };
        return dialog.ShowDialog(Owner) == true ? dialog.FileNames : [];
    }

    /// <inheritdoc />
    public string? ShowOpenExecutable(string? current)
    {
        var dialog = new OpenFileDialog { Filter = Strings.ExecutableFilter, FileName = "nasm.exe" };
        if (current is not null && Path.GetDirectoryName(current) is { Length: > 0 } folder && Directory.Exists(folder))
        {
            dialog.InitialDirectory = folder;
        }

        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ShowSaveFile(string suggestedName, string? initialFolder)
    {
        var dialog = new SaveFileDialog { Filter = Strings.FileFilter, FileName = suggestedName, DefaultExt = ".asm", AddExtension = true };
        if (initialFolder is not null)
        {
            dialog.InitialDirectory = initialFolder;
        }

        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public SaveChoice AskSaveChanges(string documentTitle)
    {
        string message = string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.SaveChangesText), documentTitle);
        int choice = MessageDialog.Show(Owner, Strings.SaveChangesTitle, message, null,
            [Strings.DialogSave, Strings.DialogDontSave, Strings.DialogCancel], defaultButton: 0, cancelButton: 2);
        return choice switch
        {
            0 => SaveChoice.Save,
            1 => SaveChoice.DontSave,
            _ => SaveChoice.Cancel,
        };
    }

    /// <inheritdoc />
    public bool AskRestore(IReadOnlyList<Recovery.RecoveredDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        CompositeFormat line = CompositeFormat.Parse(Strings.RecoveryEntry);
        string list = string.Join(Environment.NewLine, documents.Select(d =>
            string.Format(CultureInfo.CurrentCulture, line, d.FilePath ?? d.Title, d.SavedAt.LocalDateTime)));
        return MessageDialog.Show(Owner, Strings.RecoveryTitle, Strings.RecoveryText + Environment.NewLine + Environment.NewLine + list,
            Strings.RecoveryNext, [Strings.DialogRestore, Strings.DialogDiscard], defaultButton: 0, cancelButton: 0) == 0;
    }

    /// <inheritdoc />
    public void ShowError(string title, string reason, string nextStep) =>
        MessageDialog.Show(Owner, title, reason, nextStep, [Strings.DialogOk], defaultButton: 0, cancelButton: 0);

    /// <inheritdoc />
    public bool Confirm(string title, string message, string confirmLabel, string cancelLabel) =>
        MessageDialog.Show(Owner, title, message, null, [confirmLabel, cancelLabel], defaultButton: 1, cancelButton: 1) == 0;
}
