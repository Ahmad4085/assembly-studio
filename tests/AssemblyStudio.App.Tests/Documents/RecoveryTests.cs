using System.IO;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Documents.Recovery;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>TASK-063, NFR-011: autosave, the recovery folder and restoring documents (in-process).</summary>
public sealed class RecoveryTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("asmstudio-task063-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static WorkspaceViewModel Workspace(WorkspaceViewModelTests.FakeDialogs? dialogs = null) =>
        new(dialogs ?? new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());

    private static string[] Entries(RecoveryStore store) => Directory.GetFiles(store.SessionFolder, "*.json");

    [Fact]
    public void Autosave_writes_only_unsaved_documents_and_only_when_their_text_changed()
    {
        using var store = new RecoveryStore(_root);
        WorkspaceViewModel workspace = Workspace();
        var autosave = new AutosaveService(workspace, store);
        workspace.New();                       // clean: nothing to save
        DocumentViewModel edited = workspace.New();
        edited.Text = "mov ax, 1";

        autosave.Tick();
        string[] files = Entries(store);
        Assert.Single(files);
        DateTime written = File.GetLastWriteTimeUtc(files[0]);
        File.SetLastWriteTimeUtc(files[0], written.AddMinutes(-5));
        autosave.Tick();                       // unchanged: not rewritten
        Assert.Equal(written.AddMinutes(-5), File.GetLastWriteTimeUtc(files[0]));

        edited.Text = "mov ax, 2";
        autosave.Tick();
        Assert.Contains("mov ax, 2", File.ReadAllText(files[0]), StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_or_closing_a_document_removes_its_autosave()
    {
        string path = Path.Combine(_root, "prog.asm");
        File.WriteAllText(path, "old");
        using var store = new RecoveryStore(Path.Combine(_root, "recovery"));
        var dialogs = new WorkspaceViewModelTests.FakeDialogs();
        WorkspaceViewModel workspace = Workspace(dialogs);
        var autosave = new AutosaveService(workspace, store);
        DocumentViewModel saved = workspace.OpenFile(path)!;
        DocumentViewModel closed = workspace.New();
        saved.Text = "new";
        closed.Text = "scratch";
        autosave.Tick();
        Assert.Equal(2, Entries(store).Length);

        Assert.True(workspace.SaveDocument(saved));
        dialogs.SaveChoices.Enqueue(SaveChoice.DontSave);
        workspace.Close(closed);
        autosave.Tick();

        Assert.Empty(Entries(store));
    }

    [Fact]
    public void A_session_that_ended_unexpectedly_is_recovered_with_text_file_encoding_and_line_endings()
    {
        string path = Path.Combine(_root, "lf.asm");
        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. "a\nb\n"u8.ToArray()]);
        var crashed = new RecoveryStore(_root);
        WorkspaceViewModel before = Workspace();
        DocumentViewModel document = before.OpenFile(path)!;
        document.Text = "a\nb\nc\n";
        DocumentViewModel untitled = before.New();
        untitled.Text = "; scratch";
        new AutosaveService(before, crashed).Tick();
        crashed.Abandon(); // the process ends without its clean-exit cleanup

        using var next = new RecoveryStore(_root);
        IReadOnlyList<RecoveredDocument> found = next.FindAbandoned();

        Assert.Equal(2, found.Count);
        RecoveredDocument file = found.Single(f => f.FilePath is not null);
        Assert.Equal((path, "a\nb\nc\n", TextEncodingKind.Utf8Bom, LineEnding.Lf), (file.FilePath, file.Text, file.Encoding.Kind, file.LineEnding));
        Assert.Equal("; scratch", found.Single(f => f.FilePath is null).Text);

        WorkspaceViewModel after = Workspace();
        DocumentViewModel restored = after.Restore(file);
        Assert.True(restored.IsDirty);
        Assert.Equal("lf.asm", restored.Title);
        Assert.Equal(TextEncodingKind.Utf8Bom, restored.Encoding.Kind);
        Assert.True(after.SaveDocument(restored));
        Assert.Equal([0xEF, 0xBB, 0xBF, .. "a\nb\nc\n"u8.ToArray()], File.ReadAllBytes(path));

        RecoveryStore.DeleteSessions(found);
        using var afterDelete = new RecoveryStore(_root);
        Assert.Empty(afterDelete.FindAbandoned());
    }

    [Fact]
    public void A_clean_exit_leaves_nothing_to_recover()
    {
        var store = new RecoveryStore(_root);
        WorkspaceViewModel workspace = Workspace();
        workspace.New().Text = "unsaved at exit (the user chose Don't Save)";
        new AutosaveService(workspace, store).Tick();

        store.Dispose();

        using var next = new RecoveryStore(_root);
        Assert.Empty(next.FindAbandoned());
        Assert.False(Directory.Exists(store.SessionFolder));
    }

    [Fact]
    public void Another_running_instance_is_not_mistaken_for_a_crash()
    {
        using var running = new RecoveryStore(_root);
        WorkspaceViewModel workspace = Workspace();
        workspace.New().Text = "live work";
        new AutosaveService(workspace, running).Tick();

        using var second = new RecoveryStore(_root);

        Assert.Empty(second.FindAbandoned());
    }

    [Fact]
    public void Empty_and_unreadable_abandoned_sessions_offer_nothing()
    {
        var empty = new RecoveryStore(_root);
        empty.Abandon();
        var corrupt = new RecoveryStore(_root);
        File.WriteAllText(Path.Combine(corrupt.SessionFolder, "x.json"), "{ not json");
        corrupt.Abandon();

        using var next = new RecoveryStore(_root);

        Assert.Empty(next.FindAbandoned());
        Assert.False(Directory.Exists(empty.SessionFolder)); // nothing to recover: cleaned up
    }

    [Fact]
    public void Restored_untitled_documents_are_numbered_like_new_ones()
    {
        WorkspaceViewModel workspace = Workspace();
        var recovered = new RecoveredDocument(_root, Guid.NewGuid(), null, "Untitled", "x", DocumentEncoding.Utf8, LineEnding.CrLf, DateTimeOffset.Now);

        workspace.Restore(recovered);
        workspace.New();

        Assert.Equal(["Untitled", "Untitled 2"], workspace.Documents.Select(d => d.Title));
        Assert.True(workspace.Documents[0].IsDirty);
    }
}
