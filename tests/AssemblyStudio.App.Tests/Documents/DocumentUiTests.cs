using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>
/// TASK-062 in the real main window: welcome page and tabs, per-tab caret and undo history (AC-006), the unsaved
/// prompt on closing the window (AC-005), the status bar, F6 into the editor, and renders for review.
/// </summary>
public sealed class DocumentUiTests
{
    private static void WithWindow(Action<MainWindow, WorkspaceViewModel, WorkspaceViewModelTests.FakeDialogs> body)
    {
        StaTestThread.Run(() =>
        {
            var dialogs = new WorkspaceViewModelTests.FakeDialogs();
            var workspace = new WorkspaceViewModel(dialogs, new MemoryRecentFilesStore());
            var window = new MainWindow(new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace), persistLayout: false);
            try
            {
                window.Show();
                window.UpdateLayout();
                body(window, workspace, dialogs);
            }
            finally
            {
                foreach (DocumentViewModel document in workspace.Documents)
                {
                    document.MarkSaved(document.FilePath ?? "x.asm"); // never prompt while the test closes the window
                }

                window.Close();
            }
        });
    }

    private static T Named<T>(MainWindow window, string name) where T : class => Assert.IsAssignableFrom<T>(window.FindName(name));

    [Fact]
    public void The_welcome_page_shows_until_a_document_is_open()
    {
        WithWindow((window, workspace, _) =>
        {
            Assert.True(Named<UIElement>(window, "WelcomePage").IsVisible);
            Assert.False(Named<UIElement>(window, "DocumentArea").IsVisible);

            workspace.NewFromTemplate();
            window.UpdateLayout();

            Assert.False(Named<UIElement>(window, "WelcomePage").IsVisible);
            Assert.True(Named<UIElement>(window, "DocumentArea").IsVisible);
            Assert.Equal(Strings.TemplateCom, window.ActiveEditor()!.Text);
            Render(window, "editor-template.png");

            workspace.Close(workspace.ActiveDocument);
            window.UpdateLayout();
            Assert.True(Named<UIElement>(window, "WelcomePage").IsVisible);
        });
    }

    [Fact]
    public void AC_006_each_tab_keeps_its_own_editor_caret_and_undo_history()
    {
        WithWindow((window, workspace, _) =>
        {
            DocumentViewModel[] documents = [workspace.New(), workspace.New(), workspace.New()];
            var editors = new AssemblyStudio.App.Editor.CodeEditor[3];
            for (int i = 0; i < 3; i++)
            {
                workspace.ActiveDocument = documents[i];
                window.UpdateLayout();
                editors[i] = window.ActiveEditor()!;
                editors[i].AppendText($"line {i}\r\nmore {i}");
                editors[i].CaretOffset = 2 + i;
            }

            Assert.Equal(3, editors.Distinct().Count()); // one editor per document

            workspace.ActiveDocument = documents[1];
            window.UpdateLayout();

            Assert.Same(editors[1], window.ActiveEditor());
            Assert.Equal(3, editors[1].CaretOffset);
            Assert.Equal(["line 0\r\nmore 0", "line 1\r\nmore 1", "line 2\r\nmore 2"], documents.Select(d => d.Text));

            editors[1].Undo();
            Assert.Equal(string.Empty, documents[1].Text);   // only this tab's edit was undone
            Assert.Equal("line 2\r\nmore 2", documents[2].Text);
            Assert.True(editors[2].CanUndo);

            var tabs = Named<TabControl>(window, "DocumentTabs");
            Assert.Equal(3, tabs.Items.Count);
            Assert.Same(documents[1], tabs.SelectedItem);
            Render(window, "editor-tabs.png");
        });
    }

    [Fact]
    public void AC_005_closing_the_window_with_unsaved_changes_asks_and_cancel_keeps_it_open()
    {
        WithWindow((window, workspace, dialogs) =>
        {
            workspace.New().Text = "unsaved work";
            dialogs.SaveChoices.Enqueue(SaveChoice.Cancel);

            window.Close();

            Assert.True(window.IsVisible);
            Assert.Equal(["Untitled"], dialogs.AskedToSave);
            Assert.Equal("unsaved work", workspace.Documents.Single().Text);
        });
    }

    [Fact]
    public void The_status_bar_shows_the_active_documents_encoding_and_line_ending()
    {
        WithWindow((window, workspace, _) =>
        {
            string path = Path.Combine(Path.GetTempPath(), $"asmstudio-task062-{Guid.NewGuid():N}.asm");
            File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. "a\nb\n"u8.ToArray()]);
            try
            {
                workspace.OpenFile(path);
                window.UpdateLayout();

                Assert.Equal("UTF-8 with BOM", Named<TextBlock>(window, "StatusEncoding").Text);
                Assert.Equal("LF", Named<TextBlock>(window, "StatusLineEnding").Text);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public void F6_into_the_editor_focuses_the_active_documents_text()
    {
        WithWindow((window, workspace, _) =>
        {
            workspace.New();
            window.UpdateLayout();
            window.CyclePane(backwards: false); // to Machine
            window.CyclePane(backwards: true);  // back to the editor

            var focused = (DependencyObject)FocusManager.GetFocusedElement(window)!;
            Assert.True(window.ActiveEditor()!.IsAncestorOf(focused) || ReferenceEquals(window.ActiveEditor(), focused)); // the editor's text area
        });
    }

    [Fact]
    public void The_file_menu_commands_are_enabled_and_bound()
    {
        WithWindow((window, workspace, _) =>
        {
            Assert.True(workspace.NewFileCommand.CanExecute(null));
            Assert.False(workspace.SaveCommand.CanExecute(null)); // nothing open
            workspace.New();
            Assert.True(workspace.SaveCommand.CanExecute(null));
            Assert.True(workspace.CloseTabCommand.CanExecute(null));
            Assert.False(Named<MenuItem>(window, "RecentMenu").IsEnabled); // empty recent list
        });
    }

    private static void Render(Window window, string fileName)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string dir = Path.Combine(MainWindowSmokeTests.RepoRoot(), "artifacts", "ui");
        Directory.CreateDirectory(dir);
        using FileStream file = File.Create(Path.Combine(dir, fileName));
        encoder.Save(file);
    }
}
