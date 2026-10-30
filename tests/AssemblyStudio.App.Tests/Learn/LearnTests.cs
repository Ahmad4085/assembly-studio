using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Learn;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.Content;
using Xunit;

namespace AssemblyStudio.App.Tests.Learn;

/// <summary>TASK-090: the Learn sidebar (UI-015) and opening samples as untitled copies (FR-069, AC-035 open-as-copy part).</summary>
public sealed class LearnTests : IDisposable
{
    private const string SampleText = "; Test sample (fixture, not built-in content)\norg 100h\nmov ax, 4C00h\nint 21h\n";

    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task090-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>A small in-memory catalog: one sample, one tutorial, one reference page (test data, labelled as such).</summary>
    internal sealed class TestCatalog : IContentCatalog
    {
        public static readonly ContentItem Sample = new("hello", ContentKind.Sample, "Hello (test)", "A test sample.", "Samples/hello.asm", "");
        public static readonly ContentItem Tutorial = new("registers", ContentKind.Tutorial, "Registers (test)", "A test tutorial.", "Tutorials/registers.md");
        public static readonly ContentItem Page = new("mov", ContentKind.Reference, "MOV (test)", "A test reference page.", "Reference/mov.md");

        public IReadOnlyList<ContentItem> Items { get; } = [Sample, Tutorial, Page];

        public int Reads { get; private set; }

        public string ReadText(ContentItem item)
        {
            Reads++;
            return item == Sample ? SampleText : $"Test text of {item.Title}";
        }
    }

    private static (LearnViewModel Learn, WorkspaceViewModel Workspace, WorkspaceViewModelTests.FakeDialogs Dialogs) Create()
    {
        var dialogs = new WorkspaceViewModelTests.FakeDialogs();
        var workspace = new WorkspaceViewModel(dialogs, new MemoryRecentFilesStore());
        return (new LearnViewModel(new TestCatalog(), workspace), workspace, dialogs);
    }

    [Fact]
    public void The_sidebar_groups_the_catalog_into_samples_tutorials_reference_and_help()
    {
        (LearnViewModel learn, _, _) = Create();

        Assert.Equal([ContentKind.Sample, ContentKind.Tutorial, ContentKind.Reference, ContentKind.Help], learn.Sections.Select(s => s.Kind));
        Assert.Equal(["Hello (test)"], learn.Sections[0].Items.Select(i => i.Title));
        Assert.Equal(["Registers (test)"], learn.Sections[1].Items.Select(i => i.Title));
        Assert.True(learn.Sections[3].IsEmpty);
        Assert.Equal(Strings.LearnNoHelp, learn.Sections[3].EmptyText);
    }

    [Fact]
    public void Without_built_in_content_every_section_says_nothing_is_installed()
    {
        var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
        var learn = new LearnViewModel(EmbeddedContentCatalog.BuiltIn(), workspace);

        // The shipped catalog is empty until TASK-091 (samples), TASK-092 (tutorials, reference) and TASK-093 (help).
        Assert.All(learn.Sections, s => Assert.Equal(EmbeddedContentCatalog.BuiltIn().Items.Count(i => i.Kind == s.Kind), s.Items.Count));
        Assert.Equal(
            [Strings.LearnNoSamples, Strings.LearnNoTutorials, Strings.LearnNoReference, Strings.LearnNoHelp],
            learn.Sections.Select(s => s.EmptyText));
    }

    [Fact]
    public void AC_035_opening_a_sample_creates_an_untitled_copy_named_after_it()
    {
        (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();

        DocumentViewModel copy = learn.OpenSample(TestCatalog.Sample);

        Assert.Same(copy, workspace.ActiveDocument);
        Assert.Null(copy.FilePath);
        Assert.Equal("hello.asm (copy)", copy.Title);
        Assert.Equal("hello.asm", copy.SuggestedFileName);
        Assert.Equal(SampleText, copy.Text);
        Assert.False(copy.IsDirty);
        Assert.Equal(LineEnding.CrLf, copy.LineEnding);
    }

    [Fact]
    public void AC_035_saving_a_sample_copy_asks_where_and_never_changes_the_original()
    {
        (LearnViewModel learn, WorkspaceViewModel workspace, WorkspaceViewModelTests.FakeDialogs dialogs) = Create();
        var catalog = new TestCatalog();
        learn = new LearnViewModel(catalog, workspace);
        DocumentViewModel copy = learn.OpenSample(TestCatalog.Sample);
        copy.Text += "; my change\n";
        string target = Path.Combine(_folder, "mine.asm");
        dialogs.SavePaths.Enqueue(target);

        Assert.True(workspace.SaveDocument(copy));

        Assert.Equal("hello.asm", Assert.Single(dialogs.SuggestedNames)); // Save on a copy is Save As, suggesting the sample's name
        Assert.Equal(target, copy.FilePath);
        Assert.Equal("mine.asm", copy.Title);
        Assert.Contains("; my change", File.ReadAllText(target), StringComparison.Ordinal);
        Assert.Equal(SampleText, catalog.ReadText(TestCatalog.Sample)); // the built-in original is unchanged
    }

    [Fact]
    public void Opening_the_same_sample_twice_gives_two_independent_copies()
    {
        (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();

        DocumentViewModel first = learn.OpenSample(TestCatalog.Sample);
        DocumentViewModel second = learn.OpenSample(TestCatalog.Sample);
        first.Text = "changed";

        Assert.Equal(2, workspace.Documents.Count);
        Assert.Equal(SampleText, second.Text);
    }

    [Fact]
    public void Only_samples_open_as_copies_and_other_entries_show_their_text_as_explanation()
    {
        (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();

        learn.Select(learn.Sections[1].Items[0]);
        Assert.False(learn.IsSampleSelected);
        Assert.False(learn.OpenSelectedCommand.CanExecute(null));
        Assert.Equal("Test text of Registers (test)", learn.SelectedText);

        learn.Select(learn.Sections[0].Items[0]);
        Assert.True(learn.IsSampleSelected);
        Assert.Null(learn.SelectedText); // a sample is opened, not read in the sidebar
        learn.OpenSelectedCommand.Execute(null);
        Assert.Equal("hello.asm (copy)", Assert.Single(workspace.Documents).Title);

        learn.Select(learn.Sections[2]); // a section heading
        Assert.False(learn.HasSelection);
        Assert.Throws<ArgumentException>(() => learn.OpenSample(TestCatalog.Page));
    }

    [Fact]
    public void Learn_menu_commands_show_the_sidebar_with_the_section_selected()
    {
        (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();
        var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, learn: learn);
        var activated = new List<ShellPanel>();
        shell.PanelActivated += (_, panel) => activated.Add(panel);
        learn.Select(learn.Sections[0].Items[0]);

        shell.ShowLearnSectionCommand.Execute(ContentKind.Tutorial);

        Assert.True(shell.IsLearnVisible);
        Assert.True(learn.Sections[1].IsSelected);
        Assert.False(learn.Sections[0].Items[0].IsSelected);
        Assert.Null(learn.SelectedItem);
        Assert.Equal([ShellPanel.Learn], activated);
    }

    // ---- WPF ----

    [Fact]
    public void The_sidebar_lists_the_content_and_enter_on_a_sample_opens_a_copy()
    {
        StaTestThread.Run(() =>
        {
            (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, learn: learn);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400 };
            try
            {
                window.Show();
                shell.ShowLearnSectionCommand.Execute(ContentKind.Sample);
                window.UpdateLayout();

                var tree = (TreeView)window.FindName("LearnTree");
                Assert.True(tree.IsVisible);
                Assert.True(tree.IsKeyboardFocusWithin || ReferenceEquals(FocusManager.GetFocusedElement(window), tree)); // Learn ▸ Samples moves focus there
                Assert.Equal(4, tree.Items.Count);

                learn.Sections[0].Items[0].IsSelected = true; // as the arrow keys would
                window.UpdateLayout();
                Assert.Same(learn.Sections[0].Items[0], learn.SelectedItem);
                Assert.True(((Button)window.FindName("LearnOpenCopy")).IsVisible);
                Render(window, "learn-sample-selected.png");

                KeyBinding enter = tree.InputBindings.OfType<KeyBinding>().Single(b => b.Key == Key.Enter);
                enter.Command.Execute(enter.CommandParameter);
                window.UpdateLayout();
                Assert.Equal("hello.asm (copy)", Assert.Single(workspace.Documents).Title);
                Assert.Equal(SampleText.ReplaceLineEndings("\r\n"), window.ActiveEditor()!.Text.ReplaceLineEndings("\r\n"));

                learn.Sections[1].Items[0].IsSelected = true;
                window.UpdateLayout();
                var text = (TextBox)window.FindName("LearnText");
                Assert.True(text.IsVisible && text.IsReadOnly);
                Assert.Equal("Test text of Registers (test)", text.Text);
                Render(window, "learn-tutorial-selected.png");
            }
            finally
            {
                foreach (DocumentViewModel document in workspace.Documents)
                {
                    document.MarkSaved(document.FilePath ?? "x.asm");
                }

                window.Close();
            }
        });
    }

    [Fact]
    public void The_built_in_samples_appear_in_the_sidebar_and_open_as_copies()
    {
        StaTestThread.Run(() =>
        {
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var learn = new LearnViewModel(EmbeddedContentCatalog.BuiltIn(), workspace);
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, learn: learn);
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            try
            {
                window.Show();
                shell.ShowLearnSectionCommand.Execute(ContentKind.Sample);
                LearnItemViewModel hello = learn.Sections[0].Items.Single(i => i.Item.Id == "hello");
                hello.IsSelected = true;
                learn.OpenSelectedCommand.Execute(null);
                window.UpdateLayout();

                Assert.True(learn.Sections[0].Items.Count >= 8);
                Assert.Equal("hello.asm (copy)", workspace.ActiveDocument!.Title);
                Assert.StartsWith("; Hello, World", window.ActiveEditor()!.Text, StringComparison.Ordinal);
                Render(window, "learn-built-in-samples.png");
            }
            finally
            {
                foreach (DocumentViewModel document in workspace.Documents)
                {
                    document.MarkSaved(document.FilePath ?? "x.asm");
                }

                window.Close();
            }
        });
    }

    [Fact]
    public void The_welcome_page_offers_open_a_sample()
    {
        StaTestThread.Run(() =>
        {
            (LearnViewModel learn, WorkspaceViewModel workspace, _) = Create();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, learn: learn);
            var window = new MainWindow(shell, persistLayout: false);
            try
            {
                window.Show();
                window.UpdateLayout();
                var button = (Button)window.FindName("WelcomeSample");
                Assert.True(button.IsVisible);

                button.Command.Execute(button.CommandParameter);

                Assert.True(shell.IsLearnVisible);
                Assert.True(learn.Sections[0].IsSelected);
            }
            finally
            {
                window.Close();
            }
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
