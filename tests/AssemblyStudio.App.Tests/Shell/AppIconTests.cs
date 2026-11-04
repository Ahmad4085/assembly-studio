using System.Collections;
using System.IO;
using System.Resources;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Xunit;

namespace AssemblyStudio.App.Tests.Shell;

/// <summary>
/// TASK-118: the logo is the application's icon: a multi-size Windows icon that WPF can decode, embedded in the executable
/// (<c>ApplicationIcon</c>), shipped as a resource for the main window's title bar, and used by the installer.
/// </summary>
public sealed class AppIconTests
{
    private static readonly string Root = MainWindowSmokeTests.RepoRoot();
    private static readonly string IconPath = Path.Combine(Root, "src", "AssemblyStudio.App", "Assets", "AssemblyStudio.ico");

    [Fact]
    public void TASK_118_the_icon_holds_every_size_Windows_asks_for_and_WPF_decodes_them()
    {
        using FileStream stream = File.OpenRead(IconPath);
        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        int[] sizes = [.. decoder.Frames.Select(f => f.PixelWidth).Order()];
        Assert.Equal([16, 24, 32, 48, 64, 128, 256], sizes);
        Assert.All(decoder.Frames, f => Assert.Equal(f.PixelWidth, f.PixelHeight));
    }

    [Fact]
    public void TASK_118_the_executable_main_window_and_installer_use_the_icon()
    {
        XDocument project = XDocument.Load(Path.Combine(Root, "src", "AssemblyStudio.App", "AssemblyStudio.App.csproj"));
        Assert.Equal(@"Assets\AssemblyStudio.ico", project.Descendants("ApplicationIcon").Single().Value);

        string window = File.ReadAllText(Path.Combine(Root, "src", "AssemblyStudio.App", "Shell", "MainWindow.xaml"));
        Assert.Contains("Icon=\"/AssemblyStudio.App;component/Assets/AssemblyStudio.ico\"", window, StringComparison.Ordinal);

        string resourceName = typeof(App).Assembly.GetName().Name + ".g.resources";
        using Stream resources = typeof(App).Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new ResourceReader(resources);
        Assert.Contains("assets/assemblystudio.ico", reader.Cast<DictionaryEntry>().Select(e => (string)e.Key));

        string installer = File.ReadAllText(Path.Combine(Root, "build", "installer", "AssemblyStudio.iss"));
        Assert.Contains(@"SetupIconFile=..\..\src\AssemblyStudio.App\Assets\AssemblyStudio.ico", installer, StringComparison.Ordinal);
    }
}
