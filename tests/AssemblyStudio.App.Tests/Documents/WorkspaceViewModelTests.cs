using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>
/// TASK-062: the document workflow against real files, with scripted dialog answers. AC-002, AC-003, AC-004, AC-005,
/// AC-006, AC-007, AC-008 and AC-012 at view-model level.
/// </summary>
public sealed class WorkspaceViewModelTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task062-").FullName;
    private readonly FakeDialogs _dialogs = new();
    private readonly MemoryRecentFilesStore _recent = new();

    public void Dispose()
    {
        foreach (string file in Directory.GetFiles(_folder))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_folder, recursive: true);
    }

    private WorkspaceViewModel Workspace() => new(_dialogs, _recent);

    private string FileWith(string name, byte[] bytes)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string FileWith(string name, string text) => FileWith(name, Encoding.UTF8.GetBytes(text));

    // ---- AC-002 ----

    [Fact]
    public void AC_002_new_from_template_and_opened_files_each_get_a_tab_with_their_exact_content()
    {
        string path = FileWith("hello.asm", "org 100h\r\nint 20h\r\n");
        WorkspaceViewModel workspace = Workspace();

        DocumentViewModel template = workspace.NewFromTemplate();
        workspace.OpenFiles([path]);

        Assert.Equal(["Untitled", "hello.asm"], workspace.Documents.Select(d => d.Title));
        Assert.Equal(Strings.TemplateCom, template.Text);
        Assert.Contains("org 100h", template.Text, StringComparison.Ordinal);
        Assert.Equal("org 100h\r\nint 20h\r\n", workspace.Documents[1].Text);
        Assert.Same(workspace.Documents[1], workspace.ActiveDocument);
        Assert.False(workspace.Documents[1].IsDirty);
    }

    [Fact]
    public void More_new_documents_are_numbered_and_opening_an_open_file_activates_its_tab()
    {
        string path = FileWith("a.asm", "x");
        WorkspaceViewModel workspace = Workspace();
        workspace.New();
        workspace.New();
        workspace.OpenFile(path);
        workspace.ActiveDocument = workspace.Documents[0];

        workspace.OpenFile(path);

        Assert.Equal(["Untitled", "Untitled 2", "a.asm"], workspace.Documents.Select(d => d.Title));
        Assert.Equal("a.asm", workspace.ActiveDocument!.Title);
    }

    // ---- AC-003 ----

    [Fact]
    public void AC_003_save_writes_the_text_and_clears_the_unsaved_marker_and_save_as_retitles_the_tab()
    {
        string path = FileWith("prog.asm", "nop\r\n");
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.OpenFile(path)!;

        document.Text = "nop\r\nint 20h\r\n";
        Assert.True(document.IsDirty);
        Assert.EndsWith("●", document.DisplayTitle, StringComparison.Ordinal);
        workspace.SaveCommand.Execute(null);

        Assert.Equal("nop\r\nint 20h\r\n", File.ReadAllText(path));
        Assert.False(document.IsDirty);
        Assert.Equal("prog.asm", document.DisplayTitle);

        string copy = Path.Combine(_folder, "copy.asm");
        _dialogs.SavePaths.Enqueue(copy);
        workspace.SaveAsCommand.Execute(null);

        Assert.Equal("copy.asm", document.Title);
        Assert.Equal(copy, document.FilePath);
        Assert.Equal(File.ReadAllText(path), File.ReadAllText(copy));
        Assert.Equal(copy, workspace.RecentFiles[0]);
    }

    [Fact]
    public void Saving_an_untitled_document_asks_for_a_path_and_writes_utf8_without_bom_and_crlf()
    {
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.New();
        document.Text = "a\nb";
        string path = Path.Combine(_folder, "new.asm");
        _dialogs.SavePaths.Enqueue(path);

        Assert.True(workspace.SaveDocument(document));

        Assert.Equal("Untitled.asm", _dialogs.SuggestedNames.Single());
        Assert.Equal("a\r\nb"u8.ToArray(), File.ReadAllBytes(path));
    }

    // ---- AC-004 / AC-008 ----

    [Fact]
    public void AC_004_a_failed_save_shows_the_path_and_reason_keeps_the_original_and_the_unsaved_text()
    {
        string path = FileWith("busy.asm", "original");
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.OpenFile(path)!;
        document.Text = "edited";

        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.False(workspace.SaveDocument(document));
        }

        (string title, string reason, string next) = _dialogs.Errors.Single();
        Assert.Equal(Strings.SaveFailedTitle, title);
        Assert.Equal($"{path} is being used by another program.", reason);
        Assert.Equal(Strings.SaveFailedKept, next);
        Assert.Equal("original", File.ReadAllText(path));
        Assert.Equal("edited", document.Text);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void AC_008_a_missing_file_a_locked_file_and_a_read_only_target_each_report_path_and_reason()
    {
        WorkspaceViewModel workspace = Workspace();
        string missing = Path.Combine(_folder, "missing.asm");
        string locked = FileWith("locked.asm", "x");
        string readOnly = FileWith("ro.asm", "keep");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);

        Assert.Null(workspace.OpenFile(missing));
        using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Null(workspace.OpenFile(locked));
        }

        DocumentViewModel document = workspace.New();
        document.Text = "unsaved";
        _dialogs.SavePaths.Enqueue(readOnly);
        Assert.False(workspace.SaveDocument(document));

        Assert.Equal(
            [$"{missing} doesn't exist.", $"{locked} is being used by another program.", $"{readOnly} is read-only."],
            _dialogs.Errors.Select(e => e.Reason));
        Assert.Equal("unsaved", document.Text);
        Assert.Equal("keep", File.ReadAllText(readOnly));
    }

    [Fact]
    public void AC_008_a_file_without_read_permission_reports_path_and_reason()
    {
        string path = FileWith("denied.asm", "secret");
        var info = new FileInfo(path);
        FileSecurity security = info.GetAccessControl();
        var deny = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.ReadData, AccessControlType.Deny);
        security.AddAccessRule(deny);
        info.SetAccessControl(security);
        try
        {
            Assert.Null(Workspace().OpenFile(path));
        }
        finally
        {
            security.RemoveAccessRule(deny);
            info.SetAccessControl(security);
        }

        Assert.Equal($"You don't have permission to access {path}.", _dialogs.Errors.Single().Reason);
    }

    [Fact]
    public void An_invalid_path_reports_reason_without_throwing()
    {
        Assert.Null(Workspace().OpenFile("C:\\bad\0name.asm"));

        Assert.Contains("isn't a valid file path", _dialogs.Errors.Single().Reason, StringComparison.Ordinal);
    }

    // ---- AC-005 ----

    [Fact]
    public void AC_005_closing_an_unsaved_document_asks_and_cancel_keeps_it_unchanged()
    {
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.New();
        document.Text = "work";
        _dialogs.SaveChoices.Enqueue(SaveChoice.Cancel);

        Assert.False(workspace.Close(document));

        Assert.Equal(["Untitled"], _dialogs.AskedToSave);
        Assert.Single(workspace.Documents);
        Assert.Equal("work", document.Text);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void AC_005_dont_save_closes_and_save_writes_before_closing()
    {
        string path = FileWith("keep.asm", "old");
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel discard = workspace.New();
        discard.Text = "throw away";
        DocumentViewModel keep = workspace.OpenFile(path)!;
        keep.Text = "new";
        _dialogs.SaveChoices.Enqueue(SaveChoice.DontSave);
        _dialogs.SaveChoices.Enqueue(SaveChoice.Save);

        Assert.True(workspace.Close(discard));
        Assert.True(workspace.Close(keep));

        Assert.Empty(workspace.Documents);
        Assert.Null(workspace.ActiveDocument);
        Assert.Equal("new", File.ReadAllText(path));
    }

    [Fact]
    public void AC_005_choosing_save_then_cancelling_save_as_keeps_the_document_open()
    {
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.New();
        document.Text = "x";
        _dialogs.SaveChoices.Enqueue(SaveChoice.Save);
        _dialogs.SavePaths.Enqueue(null);

        Assert.False(workspace.Close(document));
        Assert.Single(workspace.Documents);
    }

    [Fact]
    public void AC_005_exiting_asks_for_each_unsaved_document_and_cancel_stops()
    {
        WorkspaceViewModel workspace = Workspace();
        workspace.New().Text = "a";
        workspace.New();
        workspace.New().Text = "c";
        _dialogs.SaveChoices.Enqueue(SaveChoice.DontSave);
        _dialogs.SaveChoices.Enqueue(SaveChoice.Cancel);

        Assert.False(workspace.ConfirmCloseAll());

        Assert.Equal(["Untitled", "Untitled 3"], _dialogs.AskedToSave); // the clean one isn't asked about
        Assert.Equal(3, workspace.Documents.Count);
    }

    // ---- AC-006 (view-model part) ----

    [Fact]
    public void AC_006_each_tab_keeps_its_own_text_and_the_active_one_is_marked()
    {
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel[] documents = [workspace.New(), workspace.New(), workspace.New()];
        for (int i = 0; i < 3; i++)
        {
            workspace.ActiveDocument = documents[i];
            documents[i].Text = $"text {i}";
        }

        workspace.ActiveDocument = documents[1];

        Assert.Equal(["text 0", "text 1", "text 2"], documents.Select(d => d.Text));
        Assert.Equal([false, true, false], documents.Select(d => d.IsActive));
    }

    // ---- AC-007 ----

    [Fact]
    public void AC_007_a_missing_recent_file_names_the_path_and_offers_to_remove_the_entry()
    {
        string gone = Path.Combine(_folder, "gone.asm");
        _recent.Save([gone]);
        WorkspaceViewModel workspace = Workspace();
        _dialogs.Confirms.Enqueue(false);
        _dialogs.Confirms.Enqueue(true);

        workspace.OpenRecentCommand.Execute(gone); // Keep
        Assert.Single(workspace.RecentFiles);
        workspace.OpenRecentCommand.Execute(gone); // Remove

        Assert.Empty(workspace.RecentFiles);
        Assert.Empty(_recent.Load());
        Assert.All(_dialogs.Confirmations, c => Assert.Contains(gone, c, StringComparison.Ordinal));
        Assert.Empty(workspace.Documents);
    }

    [Fact]
    public void The_recent_list_keeps_the_last_10_most_recent_first_without_duplicates()
    {
        WorkspaceViewModel workspace = Workspace();
        string[] files = [.. Enumerable.Range(0, 12).Select(i => FileWith($"f{i}.asm", "x"))];
        foreach (string file in files)
        {
            workspace.OpenFile(file);
        }

        workspace.Close(workspace.Documents[0]);
        workspace.OpenFile(files[0]);

        Assert.Equal(10, workspace.RecentFiles.Count);
        Assert.Equal(files[0], workspace.RecentFiles[0]);
        Assert.Equal(files[11], workspace.RecentFiles[1]);
        Assert.Equal(workspace.RecentFiles, _recent.Load());
    }

    [Fact]
    public void Recent_files_are_stored_in_the_settings_file_beside_the_layout()
    {
        string settings = FileWith("settings.json", "{ \"layout\": { \"learnWidth\": 200 } }");
        var store = new JsonRecentFilesStore(settings);

        store.Save([@"C:\a.asm", @"C:\b.asm"]);

        Assert.Equal([@"C:\a.asm", @"C:\b.asm"], new JsonRecentFilesStore(settings).Load());
        Assert.Contains("learnWidth", File.ReadAllText(settings), StringComparison.Ordinal);
    }

    // ---- AC-012 ----

    [Fact]
    public void AC_012_bom_and_crlf_and_plain_lf_are_unchanged_after_edit_and_save()
    {
        string bomCrlf = FileWith("bom.asm", [0xEF, 0xBB, 0xBF, .. "a\r\nb\r\n"u8.ToArray()]);
        string lf = FileWith("lf.asm", "a\nb\n");
        WorkspaceViewModel workspace = Workspace();

        foreach (string path in new[] { bomCrlf, lf })
        {
            DocumentViewModel document = workspace.OpenFile(path)!;
            document.Text += "c\r\n"; // the text box inserts CR LF
            Assert.True(workspace.SaveDocument(document));
        }

        Assert.Equal([0xEF, 0xBB, 0xBF, .. "a\r\nb\r\nc\r\n"u8.ToArray()], File.ReadAllBytes(bomCrlf));
        Assert.Equal("a\nb\nc\n"u8.ToArray(), File.ReadAllBytes(lf));
    }

    [Fact]
    public void AC_012_a_non_utf8_file_shows_an_encoding_notice_and_keeps_its_encoding()
    {
        string path = FileWith("ansi.asm", [.. "; caf"u8.ToArray(), 0xE9, 0x0D, 0x0A]);
        DocumentViewModel document = Workspace().OpenFile(path)!;

        Assert.NotNull(document.EncodingNotice);
        Assert.Contains("isn't valid UTF-8", document.EncodingNotice, StringComparison.Ordinal);
        Assert.Equal(TextEncodingKind.Ansi, document.Encoding.Kind);
    }

    [Fact]
    public void Mixed_line_endings_are_normalized_only_after_confirmation()
    {
        string path = FileWith("mixed.asm", "a\r\nb\nc\r\n");
        WorkspaceViewModel workspace = Workspace();
        DocumentViewModel document = workspace.OpenFile(path)!;
        Assert.True(document.HasMixedLineEndings);
        _dialogs.Confirms.Enqueue(false);
        _dialogs.Confirms.Enqueue(true);

        workspace.NormalizeLineEndingsCommand.Execute(document);
        Assert.True(document.HasMixedLineEndings);
        Assert.False(document.IsDirty);

        workspace.NormalizeLineEndingsCommand.Execute(document);
        workspace.SaveDocument(document);

        Assert.False(document.HasMixedLineEndings);
        Assert.Equal("a\r\nb\r\nc\r\n", File.ReadAllText(path));
    }

    /// <summary>Dialog answers scripted by the test; records what was asked.</summary>
    internal sealed class FakeDialogs : IDialogService
    {
        public Queue<IReadOnlyList<string>> OpenAnswers { get; } = new();

        public Queue<string?> SavePaths { get; } = new();

        public Queue<SaveChoice> SaveChoices { get; } = new();

        public Queue<bool> Confirms { get; } = new();

        public List<string> SuggestedNames { get; } = [];

        public List<string> AskedToSave { get; } = [];

        public List<(string Title, string Reason, string Next)> Errors { get; } = [];

        public List<string> Confirmations { get; } = [];

        public IReadOnlyList<string> ShowOpenFiles() => OpenAnswers.Count > 0 ? OpenAnswers.Dequeue() : [];

        public string? ShowSaveFile(string suggestedName, string? initialFolder)
        {
            SuggestedNames.Add(suggestedName);
            return SavePaths.Dequeue();
        }

        public SaveChoice AskSaveChanges(string documentTitle)
        {
            AskedToSave.Add(documentTitle);
            return SaveChoices.Dequeue();
        }

        public void ShowError(string title, string reason, string nextStep) => Errors.Add((title, reason, nextStep));

        public bool Confirm(string title, string message, string confirmLabel, string cancelLabel)
        {
            Confirmations.Add(message);
            return Confirms.Dequeue();
        }
    }
}
