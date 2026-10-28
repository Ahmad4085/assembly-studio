using System.Windows;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>Command-line options (NFR-005 smoke mode) and Windows text scaling (NFR-026).</summary>
public class StartupAndScalingTests
{
    [Fact]
    public void Parses_settings_and_smoke_report_paths()
    {
        var options = StartupOptions.Parse(["--settings", @"C:\s.json", "--smoke-report", @"C:\r.txt"]);

        Assert.Equal(new StartupOptions(@"C:\s.json", @"C:\r.txt"), options);
    }

    [Fact]
    public void Ignores_unknown_arguments_and_a_trailing_flag_without_value()
    {
        Assert.Equal(new StartupOptions(null, null), StartupOptions.Parse(["hello.asm", "--settings"]));
    }

    [Theory]
    [InlineData(100, 1.0)]
    [InlineData(150, 1.5)]
    [InlineData(225, 2.25)]
    [InlineData(99, 1.0)]
    [InlineData(300, 1.0)]
    public void Text_scale_factor_comes_from_the_registry_percentage(int percent, double expected) =>
        Assert.Equal(expected, TextScale.FactorFrom(percent));

    [Fact]
    public void Missing_text_scale_value_means_no_scaling() => Assert.Equal(1.0, TextScale.FactorFrom(null));

    [Fact]
    public void Apply_scales_every_font_size_token()
    {
        StaTestThread.Run(() =>
        {
            var resources = new ResourceDictionary
            {
                ["FontSize.UI"] = 13.0, ["FontSize.UI.Small"] = 12.0, ["FontSize.Title"] = 15.0, ["FontSize.Code"] = 14.0,
            };

            TextScale.Apply(resources, 2.0);

            Assert.Equal(26.0, resources["FontSize.UI"]);
            Assert.Equal(24.0, resources["FontSize.UI.Small"]);
            Assert.Equal(30.0, resources["FontSize.Title"]);
            Assert.Equal(28.0, resources["FontSize.Code"]);
        });
    }
}
