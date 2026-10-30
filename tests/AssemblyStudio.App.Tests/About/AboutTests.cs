using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.About;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.App.Tests.Settings;
using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.App.Tests.About;

/// <summary>TASK-068: About / Diagnostics (AC-038, FR-076, FR-087) and the notices view (FR-090).</summary>
public sealed class AboutTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task068-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private AboutSources Installed(string? releaseNotes = null, string? notices = null)
    {
        if (releaseNotes is not null)
        {
            File.WriteAllText(Path.Combine(_folder, AboutSources.ReleaseNotesFile), releaseNotes);
        }

        if (notices is not null)
        {
            File.WriteAllText(Path.Combine(_folder, AboutSources.NoticesFile), notices);
        }

        return new AboutSources(_folder, @"C:\Users\x\AppData\Roaming\AssemblyStudio\settings.json", @"C:\Users\x\AppData\Local\AssemblyStudio\logs");
    }

    [Fact]
    public void AC_038_the_diagnostics_text_has_the_application_assembler_engine_dotnet_and_os_versions()
    {
        AssemblerInfo nasm = SettingsTests.Check(null);
        var about = new AboutViewModel(Installed(), nasm, _ => { });

        string[] lines = about.Diagnostics.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal($"Application: Assembly Studio {AboutViewModel.AppVersion}", lines[0]);
        Assert.Equal($"Assembler: NASM {nasm.Version}, Bundled, {SettingsTests.BundledNasm}", lines[1]);
        Assert.Matches(@"^Execution engine: AssemblyStudio\.Engine\.I8086 \d+\.\d+\.\d+.* \(Intel 8086, real mode, interpreter\)$", lines[2]);
        Assert.StartsWith("DOS services: AssemblyStudio.Os.Dos ", lines[3], StringComparison.Ordinal);
        Assert.Equal($".NET: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})", lines[4]);
        Assert.Equal($"Operating system: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})", lines[5]);
        Assert.Equal(@"Settings: C:\Users\x\AppData\Roaming\AssemblyStudio\settings.json", lines[6]);
        Assert.Equal(@"Log folder: C:\Users\x\AppData\Local\AssemblyStudio\logs", lines[7]);
        Assert.Matches(@"^\d+\.\d+\.\d+", AboutViewModel.AppVersion);
        Assert.DoesNotContain("+", AboutViewModel.AppVersion, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unusable_assembler_is_reported_with_its_reason()
    {
        AssemblerInfo missing = SettingsTests.Check(@"C:\no\such\nasm.exe");

        string text = AboutViewModel.BuildDiagnostics(Installed(), missing);

        Assert.Contains($"Assembler: not usable ({missing.Status}, Configured): {missing.Problem}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AC_038_copy_puts_the_diagnostics_text_on_the_clipboard()
    {
        string? copied = null;
        var about = new AboutViewModel(Installed(), SettingsTests.Check(null), text => copied = text);

        about.CopyDiagnosticsCommand.Execute(null);

        Assert.Equal(about.Diagnostics, copied);
        Assert.True(about.Copied);
    }

    [Fact]
    public void A_busy_clipboard_is_not_an_error()
    {
#pragma warning disable CA2201 // the exception Clipboard.SetText throws when another program holds the clipboard
        var about = new AboutViewModel(Installed(), SettingsTests.Check(null), _ => throw new COMException("OpenClipboard failed"));
#pragma warning restore CA2201

        about.CopyDiagnosticsCommand.Execute(null);

        Assert.False(about.Copied);
    }

    [Fact]
    public void AC_038_the_installed_release_notes_are_shown()
    {
        var about = new AboutViewModel(Installed(releaseNotes: "# Assembly Studio 0.1.0\n\n- First preview."), SettingsTests.Check(null), _ => { }, AboutPage.ReleaseNotes);

        Assert.Equal("# Assembly Studio 0.1.0\n\n- First preview.", about.ReleaseNotes);
        Assert.Equal(AboutPage.ReleaseNotes, about.SelectedPage);
    }

    [Fact]
    public void Missing_release_notes_and_notices_are_reported_with_their_paths_not_invented()
    {
        var about = new AboutViewModel(Installed(), SettingsTests.Check(null), _ => { });

        Assert.Equal($"This file isn't installed: {Path.Combine(_folder, "RELEASE-NOTES.md")}", about.ReleaseNotes);
        Assert.Equal($"This file isn't installed: {Path.Combine(_folder, "THIRD-PARTY-NOTICES.txt")}", about.Notices);
    }

    [Fact]
    public void FR_090_the_notices_view_shows_the_notices_file_and_each_bundled_tools_license()
    {
        string nasm = Path.Combine(_folder, "tools", "nasm");
        Directory.CreateDirectory(nasm);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "LICENSE"), Path.Combine(nasm, "LICENSE"));
        Installed(notices: "Third-party notices for Assembly Studio");

        string notices = AboutViewModel.BuildNotices(_folder);

        Assert.StartsWith("Third-party notices for Assembly Studio", notices, StringComparison.Ordinal);
        Assert.Contains($"nasm: {Path.Combine(nasm, "LICENSE")}", notices, StringComparison.Ordinal);
        Assert.Contains("BSD", notices, StringComparison.Ordinal); // NASM's 2-clause BSD license text
    }

    [Fact]
    public void The_bundled_NASM_license_is_installed_next_to_the_application()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "LICENSE")));
        Assert.Contains("nasm: ", AboutViewModel.BuildNotices(AppContext.BaseDirectory), StringComparison.Ordinal);
    }

    // ---- WPF ----

    [Fact]
    public void AC_038_the_window_shows_each_page_with_selectable_read_only_text()
    {
        StaTestThread.Run(() =>
        {
            Installed(releaseNotes: "# Assembly Studio (test release notes)\n\n- Written by the test.");
            var about = new AboutViewModel(new AboutSources(_folder, "settings.json", "logs"), SettingsTests.Check(null), _ => { });
            var window = new AboutWindow(about);
            try
            {
                window.Show();
                window.UpdateLayout();
                var pages = (TabControl)window.FindName("Pages");
                var diagnostics = (TextBox)window.FindName("DiagnosticsText");
                Assert.Equal(0, pages.SelectedIndex);
                Assert.True(diagnostics.IsReadOnly);
                Assert.Equal(about.Diagnostics, diagnostics.Text);
                Assert.True(((Button)window.FindName("CopyButton")).IsVisible);
                Render(window, "about-diagnostics.png");

                about.SelectedPage = AboutPage.ReleaseNotes;
                window.UpdateLayout();
                Assert.Equal(1, pages.SelectedIndex);
                Assert.True(((TextBox)window.FindName("ReleaseNotesText")).IsVisible);
                Assert.False(((Button)window.FindName("CopyButton")).IsVisible); // Copy is for the diagnostics
                Render(window, "about-release-notes.png");

                pages.SelectedIndex = 2;
                Assert.Equal(AboutPage.Notices, about.SelectedPage);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void The_help_menu_opens_about_release_notes_and_notices()
    {
        StaTestThread.Run(() =>
        {
            var opened = new List<AboutPage>();
            var workspace = new WorkspaceViewModel(new WorkspaceViewModelTests.FakeDialogs(), new MemoryRecentFilesStore());
            var window = new MainWindow(new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, openAbout: opened.Add), persistLayout: false);
            try
            {
                window.Show();
                window.UpdateLayout();
                Menu menu = FindChild<Menu>(window);
                MenuItem help = menu.Items.OfType<MenuItem>().Single(m => Equals(m.Header, Strings.MenuHelp));
                foreach (MenuItem item in help.Items.OfType<MenuItem>().Where(i => i.Command is not null))
                {
                    Assert.True(item.Command.CanExecute(item.CommandParameter), (string)item.Header);
                    item.Command.Execute(item.CommandParameter);
                }

                Assert.Equal([AboutPage.ReleaseNotes, AboutPage.Notices, AboutPage.Diagnostics], opened);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            if (FindChild<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null!;
    }

    private static void Render(Window window, string fileName)
    {
        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
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
