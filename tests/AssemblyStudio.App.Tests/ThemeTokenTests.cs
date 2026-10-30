using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// TASK-061: the theme dictionaries against the approved design tokens (docs/04-design/design-tokens.md v1.0).
/// Every theme defines every color token; Dark and Light match the token table exactly; the WCAG 2.2 contrast pairs
/// pass (text ≥ 4.5:1, UI components, glyphs and squiggles ≥ 3:1, NFR-022); high contrast maps every token to the
/// system color of §3; and switching themes updates an open window.
/// </summary>
public sealed partial class ThemeTokenTests
{
    private static readonly string TokenTable = File.ReadAllText(Path.Combine(MainWindowSmokeTests.RepoRoot(), "docs", "04-design", "design-tokens.md"));

    /// <summary>The 32 color and syntax tokens with their approved dark and light values.</summary>
    private static readonly (string Key, Color Dark, Color Light)[] Tokens =
        [.. TokenRow().Matches(TokenTable).Select(m => (m.Groups[1].Value, Parse(m.Groups[2].Value), Parse(m.Groups[3].Value)))];

    private static readonly string[] UiText = ["Text.Primary", "Text.Secondary", "Text.Muted", "Accent.Primary", "Status.Error", "Status.Warning", "Status.Info", "Status.Success", "Value.Changed"];
    private static readonly string[] UiBackgrounds = ["Surface.Window", "Surface.Panel", "Surface.Raised"];
    private static readonly string[] CodeBackgrounds = ["Surface.Editor", "Editor.CurrentLine", "Editor.ExecutionLine", "Editor.Selection"];
    private static readonly string[] NonText = ["Focus.Ring", "Border.Strong", "Accent.Primary", "Accent.ExecutionArrow", "Accent.Breakpoint", "Status.Error", "Status.Warning"];
    private static readonly string[] Surfaces = ["Surface.Window", "Surface.Panel", "Surface.Raised", "Surface.Editor", "Surface.Gutter"];
    private static readonly string[] MeaningByGlyph = ["Accent.Breakpoint", "Accent.ExecutionArrow", "Status.Error", "Status.Warning", "Status.Info", "Status.Success", "Value.Changed"];
    private static readonly string[] NonTextBackgrounds = ["Surface.Window", "Surface.Panel", "Surface.Editor", "Surface.Gutter", "Editor.ExecutionLine"];

    /// <summary>
    /// The pairs of design tokens §2, rebuilt from its description because the original script is not in the repository:
    /// UI text on window, panel and raised surfaces; every syntax color on every editor background (plain, current,
    /// execution and selection lines); text on the accent; and focus ring, control borders, accent, glyphs and
    /// squiggles on window, panel, editor, gutter and execution-line backgrounds. 95 pairs per theme, a superset of
    /// the 83 per theme the token document counted (its exact list wasn't kept).
    /// </summary>
    private static IEnumerable<(string Foreground, string Background, double Minimum)> Pairs() =>
        UiText.SelectMany(f => UiBackgrounds.Select(b => (f, b, 4.5)))
            .Concat(Tokens.Where(t => t.Key.StartsWith("Syntax.", StringComparison.Ordinal)).SelectMany(t => CodeBackgrounds.Select(b => (t.Key, b, 4.5))))
            .Append(("Text.OnAccent", "Accent.Primary", 4.5))
            .Concat(NonText.SelectMany(f => NonTextBackgrounds.Select(b => (f, b, 3.0))));

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static ResourceDictionary Load(AppTheme theme) => new() { Source = ThemeManager.DictionaryUri(theme) };

    [Fact]
    public void The_token_table_has_the_32_color_tokens()
    {
        Assert.Equal(32, Tokens.Length);
        Assert.Equal(8, Tokens.Count(t => t.Key.StartsWith("Syntax.", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.HighContrast)]
    public void Every_theme_defines_exactly_the_color_tokens(AppTheme theme)
    {
        StaTestThread.Run(() =>
        {
            ResourceDictionary dictionary = Load(theme);

            Assert.Equal(Tokens.Select(t => t.Key).Order(), dictionary.Keys.Cast<string>().Order());
            Assert.All(Tokens, t => Assert.IsType<SolidColorBrush>(dictionary[t.Key]));
        });
    }

    [Theory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    public void Dark_and_light_match_the_approved_token_values(AppTheme theme)
    {
        StaTestThread.Run(() =>
        {
            ResourceDictionary dictionary = Load(theme);
            foreach ((string key, Color dark, Color light) in Tokens)
            {
                Assert.Equal(theme == AppTheme.Dark ? dark : light, ((SolidColorBrush)dictionary[key]).Color);
            }
        });
    }

    [Theory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    public void Every_contrast_pair_meets_wcag_2_2_aa(AppTheme theme)
    {
        StaTestThread.Run(() =>
        {
            ResourceDictionary dictionary = Load(theme);
            Color ColorOf(string key) => ((SolidColorBrush)dictionary[key]).Color;
            var measured = Pairs().Select(p => (p.Foreground, p.Background, p.Minimum, Ratio: ContrastRatio(ColorOf(p.Foreground), ColorOf(p.Background)))).ToList();

            List<string> failures = [.. measured.Where(m => m.Ratio < m.Minimum).Select(m => $"{m.Foreground} on {m.Background}: {m.Ratio:F2} < {m.Minimum}")];
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{theme}: {measured.Count - failures.Count}/{measured.Count} pairs pass; lowest: " +
                string.Join("; ", measured.OrderBy(m => m.Ratio / m.Minimum).Take(3).Select(m => $"{m.Foreground} on {m.Background} {m.Ratio:F2}:1")));
            Assert.Equal(95, measured.Count);
            Assert.Empty(failures);
        });
    }

    [Theory]
    [InlineData("Status.Success", "Surface.Window", AppTheme.Light, 4.85)] // design tokens §2, measured 2026-10-26
    [InlineData("Border.Strong", "Surface.Window", AppTheme.Light, 3.47)]
    [InlineData("Accent.Primary", "Surface.Raised", AppTheme.Dark, 5.28)]
    public void The_contrast_formula_reproduces_the_documented_measurements(string foreground, string background, AppTheme theme, double documented)
    {
        (string _, Color dark, Color light) fg = Tokens.Single(t => t.Key == foreground);
        (string _, Color dark, Color light) bg = Tokens.Single(t => t.Key == background);
        bool isDark = theme == AppTheme.Dark;

        double ratio = ContrastRatio(isDark ? fg.dark : fg.light, isDark ? bg.dark : bg.light);

        Assert.Equal(documented, Math.Round(ratio, 2));
    }

    [Fact]
    public void High_contrast_maps_every_token_to_the_system_color_of_section_3()
    {
        string xaml = File.ReadAllText(Path.Combine(MainWindowSmokeTests.RepoRoot(), "src", "AssemblyStudio.App", "Themes", "HighContrast.xaml"));
        Dictionary<string, string> mapping = HighContrastEntry().Matches(xaml).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        Assert.Equal(32, mapping.Count);
        foreach (string key in Surfaces)
        {
            Assert.Equal("Window", mapping[key]);
        }

        Assert.All(Tokens.Where(t => t.Key.StartsWith("Syntax.", StringComparison.Ordinal)), t => Assert.Equal("WindowText", mapping[t.Key])); // syntax highlighting off
        Assert.Equal(("WindowText", "WindowText"), (mapping["Text.Primary"], mapping["Text.Secondary"]));
        Assert.Equal(("GrayText", "GrayText"), (mapping["Text.Muted"], mapping["Text.Disabled"]));
        Assert.Equal("Highlight", mapping["Editor.Selection"]);
        Assert.Equal("HotTrack", mapping["Accent.Primary"]);
        Assert.Equal(("WindowText", "WindowText"), (mapping["Focus.Ring"], mapping["Border.Strong"]));
        Assert.All(MeaningByGlyph, key => Assert.Equal("WindowText", mapping[key])); // meaning by glyph and weight, not hue
    }

    // ---- Theme selection ----

    [Theory]
    [InlineData(ThemePreference.System, false, false, AppTheme.Dark)]
    [InlineData(ThemePreference.System, false, true, AppTheme.Light)]
    [InlineData(ThemePreference.Dark, false, true, AppTheme.Dark)]
    [InlineData(ThemePreference.Light, false, false, AppTheme.Light)]
    [InlineData(ThemePreference.System, true, true, AppTheme.HighContrast)]
    [InlineData(ThemePreference.Light, true, false, AppTheme.HighContrast)]
    public void High_contrast_wins_and_system_follows_the_windows_app_mode(ThemePreference preference, bool highContrast, bool lightApps, AppTheme expected)
    {
        Assert.Equal(expected, ThemeManager.Resolve(preference, highContrast, lightApps));
    }

    [Fact]
    public void Switching_the_theme_updates_an_open_window_and_renders_light_and_high_contrast_for_review()
    {
        StaTestThread.Run(() =>
        {
            var manager = new ThemeManager(Application.Current.Resources);
            var window = new MainWindow(new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test"), persistLayout: false);
            try
            {
                manager.Apply(AppTheme.Dark);
                window.Show();
                window.UpdateLayout();
                Assert.Equal(Tokens.Single(t => t.Key == "Surface.Window").Dark, ((SolidColorBrush)window.Background).Color);

                manager.Apply(AppTheme.Light);
                window.UpdateLayout();
                Assert.Equal(Tokens.Single(t => t.Key == "Surface.Window").Light, ((SolidColorBrush)window.Background).Color);
                Render(window, "shell-empty-light.png");

                manager.Apply(AppTheme.HighContrast);
                window.UpdateLayout();
                Assert.Equal(SystemColors.WindowColor, ((SolidColorBrush)window.Background).Color);
                Render(window, "shell-empty-highcontrast.png");

                Assert.Single(Application.Current.Resources.MergedDictionaries, d => d.Source?.OriginalString.Contains("Themes/", StringComparison.Ordinal) == true
                    && !d.Source.OriginalString.EndsWith("Shared.xaml", StringComparison.Ordinal)); // replaced, not stacked
            }
            finally
            {
                window.Close();
                manager.Apply(AppTheme.Dark); // the application resources are shared by every WPF test
            }
        });
    }

    /// <summary>WCAG 2.2 contrast ratio from relative luminance.</summary>
    private static double ContrastRatio(Color a, Color b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color color)
    {
        static double Channel(byte value)
        {
            double c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
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

    [GeneratedRegex(@"^\| `([\w.]+)` \| `(#[0-9A-F]{6})` \| `(#[0-9A-F]{6})` \|", RegexOptions.Multiline)]
    private static partial Regex TokenRow();

    [GeneratedRegex(@"x:Key=""([\w.]+)"" Color=""\{DynamicResource \{x:Static SystemColors\.(\w+)ColorKey\}\}""")]
    private static partial Regex HighContrastEntry();
}
