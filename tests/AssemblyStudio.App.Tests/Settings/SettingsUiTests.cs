using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Settings;

/// <summary>TASK-067 in WPF: the Settings dialog (UI-017), Ctrl+, in the main window, and the editor font setting.</summary>
public sealed class SettingsUiTests : IDisposable
{
    private static readonly string[] Fields = ["AssemblerPathBox", "FontBox", "FontSizeBox", "TabWidthBox", "ThemeBox", "BudgetBox", "NumberFormatBox"];

    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task067-ui-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void The_settings_dialog_shows_the_assembler_and_explains_an_invalid_path()
    {
        StaTestThread.Run(() =>
        {
            var settings = new JsonSettingsService(Path.Combine(_folder, "settings.json"));
            var viewModel = new SettingsViewModel(settings, new WorkspaceViewModelTests.FakeDialogs(), SettingsTests.Check, () => { }, ["Cascadia Mono", "Consolas"]);
            var window = new SettingsWindow(viewModel);
            try
            {
                window.Show();
                window.UpdateLayout();
                var summary = (TextBlock)window.FindName("AssemblerSummaryText");
                var error = (TextBlock)window.FindName("AssemblerErrorText");
                Assert.StartsWith(Strings.SettingsOriginBundled, summary.Text, StringComparison.Ordinal);
                Assert.False(error.IsVisible);
                Render(window, "settings.png");

                ((TextBox)window.FindName("AssemblerPathBox")).Text = @"C:\no\such\nasm.exe";
                ((Button)window.FindName("OkButton")).Command.Execute(null);
                window.UpdateLayout();

                Assert.True(window.IsVisible); // not closed: nothing saved
                Assert.True(error.IsVisible);
                Assert.Contains(@"C:\no\such\nasm.exe", error.Text, StringComparison.Ordinal);
                Assert.DoesNotContain("Settings", error.Text, StringComparison.Ordinal); // already in Settings
                Render(window, "settings-invalid-path.png");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Every_field_has_a_label_with_an_access_key_and_an_automation_name()
    {
        StaTestThread.Run(() =>
        {
            var settings = new JsonSettingsService(Path.Combine(_folder, "settings.json"));
            var window = new SettingsWindow(new SettingsViewModel(settings, new WorkspaceViewModelTests.FakeDialogs(), SettingsTests.Check, () => { }, []));
            try
            {
                window.Show();
                window.UpdateLayout();
                foreach (string name in Fields)
                {
                    var field = (Control)window.FindName(name);
                    Assert.False(string.IsNullOrEmpty(System.Windows.Automation.AutomationProperties.GetName(field)), name);
                    Assert.Contains(LogicalChildren(window).OfType<Label>(), l => ReferenceEquals(l.Target, field) && ((string)l.Content).Contains('_', StringComparison.Ordinal));
                }

                Assert.Same(window.FindName("AssemblerPathBox"), Keyboard.FocusedElement); // opens on the first field
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Ctrl_comma_and_the_menu_open_settings_and_the_editor_font_follows_the_setting()
    {
        StaTestThread.Run(() =>
        {
            int opened = 0;
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            workspace.New();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, () => opened++);
            var window = new MainWindow(shell, persistLayout: false);
            ResourceDictionary resources = Application.Current.Resources;
            object? oldFont = resources["Font.Editor"], oldSize = resources["FontSize.Editor"];
            try
            {
                window.Show();
                window.UpdateLayout();
                KeyBinding binding = window.InputBindings.OfType<KeyBinding>().Single(b => b.Key == Key.OemComma && b.Modifiers == ModifierKeys.Control);
                binding.Command.Execute(binding.CommandParameter);
                Assert.Equal(1, opened);

                EditorAppearance.Apply(resources, AppSettings.Default with { FontFamily = "Consolas", FontSize = 20 }, textScale: 1.5);
                window.UpdateLayout();

                AssemblyStudio.App.Editor.CodeEditor editor = window.ActiveEditor()!;
                Assert.Equal("Consolas", editor.FontFamily.Source);
                Assert.Equal(30, editor.FontSize);
            }
            finally
            {
                resources["Font.Editor"] = oldFont;
                resources["FontSize.Editor"] = oldSize;
                window.Close();
            }
        });
    }

    private static IEnumerable<DependencyObject> LogicalChildren(DependencyObject parent)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is DependencyObject element)
            {
                yield return element;
                foreach (DependencyObject descendant in LogicalChildren(element))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static void Render(Window window, string fileName)
    {
        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0); // the window's border: content with its margin
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string dir = Path.Combine(MainWindowSmokeTests.RepoRoot(), "artifacts", "ui");
        Directory.CreateDirectory(dir);
        using FileStream file = File.Create(Path.Combine(dir, fileName));
        encoder.Save(file);
    }
}
