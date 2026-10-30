using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Content;
using Xunit;

namespace AssemblyStudio.App.Tests.Learn;

/// <summary>TASK-093, AC-051 (NFR-033): the help pages agree with the application: every shortcut the menus show is
/// documented, and the documented instruction budget is the settings default.</summary>
public sealed class HelpPagesTests
{
    private static readonly EmbeddedContentCatalog Catalog = EmbeddedContentCatalog.BuiltIn();

    private static string Help(string id) => Catalog.ReadText(Catalog.Items.Single(i => i.Id == "help-" + id));

    public static TheoryData<string, string> Shortcuts() =>
    [
        .. typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.Name.StartsWith("Key", StringComparison.Ordinal) && p.PropertyType == typeof(string))
            .Select(p => (p.Name, (string)p.GetValue(null)!)),
    ];

    [Theory]
    [MemberData(nameof(Shortcuts))]
    public void Every_shortcut_the_menus_show_is_documented(string name, string shortcut)
    {
        var whole = new Regex($@"(?<![\w+]){Regex.Escape(shortcut)}(?![\w+])");

        Assert.True(whole.IsMatch(Help("shortcuts")), $"{name} ({shortcut}) is missing from the shortcuts help page");
    }

    [Fact]
    public void The_documented_instruction_budget_is_the_default()
    {
        string budget = AppSettings.Default.InstructionBudget.ToString("N0", CultureInfo.InvariantCulture);

        Assert.Contains($"at most {budget} instructions by default", Help("scope"), StringComparison.Ordinal);
        Assert.Contains($"({budget} instructions by default", Help("differences"), StringComparison.Ordinal);
    }

    [Fact]
    public void The_help_section_lists_every_page()
    {
        Assert.Equal(7, Catalog.Items.Count(i => i.Kind == ContentKind.Help));
        Assert.All(Catalog.Items.Where(i => i.Kind == ContentKind.Help), i => Assert.False(string.IsNullOrWhiteSpace(Catalog.ReadText(i)), i.Id));
    }
}
