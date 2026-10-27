using System.Xml.Linq;
using Xunit;

namespace AssemblyStudio.ArchitectureTests;

/// <summary>
/// ADR-0002 rules checked on the project files. The compiler drops assembly references that no code uses,
/// so <see cref="ArchitectureRuleTests"/> alone cannot see a forbidden reference until code uses it.
/// </summary>
public class ProjectFileRuleTests
{
    private static readonly string[] ForbiddenUiPackagePrefixes =
    [
        "AvalonEdit"
    ];

    [Fact]
    public void Every_src_project_is_covered_by_the_ADR_0002_graph()
    {
        var projects = Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        var unknown = projects.Where(p => !ArchitectureRuleTests.AllowedProductDependencies.ContainsKey(p!)).ToList();

        Assert.True(
            unknown.Count == 0,
            $"ADR-0002: src projects missing from the allowed dependency graph: {string.Join(", ", unknown)}");
    }

    [Theory]
    [InlineData("AssemblyStudio.Core")]
    [InlineData("AssemblyStudio.Engine.I8086")]
    [InlineData("AssemblyStudio.Runtime")]
    [InlineData("AssemblyStudio.Os.Dos")]
    [InlineData("AssemblyStudio.Toolchain.Nasm")]
    [InlineData("AssemblyStudio.Debugging")]
    [InlineData("AssemblyStudio.Content")]
    [InlineData("AssemblyStudio.App")]
    public void Project_references_follow_the_ADR_0002_graph(string projectName)
    {
        var project = LoadProject(projectName);
        var allowed = ArchitectureRuleTests.AllowedProductDependencies[projectName];

        var disallowed = project.Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string?)e.Attribute("Include") ?? string.Empty).Replace('\\', '/')))
            .Where(name => !allowed.Contains(name))
            .ToList();

        Assert.True(
            disallowed.Count == 0,
            $"ADR-0002 graph violation in {projectName}.csproj: disallowed ProjectReference to {string.Join(", ", disallowed)}");
    }

    [Theory]
    [InlineData("AssemblyStudio.Core")]
    [InlineData("AssemblyStudio.Engine.I8086")]
    [InlineData("AssemblyStudio.Runtime")]
    [InlineData("AssemblyStudio.Os.Dos")]
    [InlineData("AssemblyStudio.Toolchain.Nasm")]
    [InlineData("AssemblyStudio.Debugging")]
    [InlineData("AssemblyStudio.Content")]
    public void Non_app_projects_do_not_enable_or_reference_ui_frameworks(string projectName)
    {
        var project = LoadProject(projectName);
        var violations = new List<string>();

        foreach (var property in new[] { "UseWPF", "UseWindowsForms" })
        {
            if (project.Descendants(property).Any(e => string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase)))
            {
                violations.Add($"{property}=true");
            }
        }

        violations.AddRange(project.Descendants()
            .Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .Where(e => e.Value.Contains("-windows", StringComparison.OrdinalIgnoreCase))
            .Select(e => $"{e.Name.LocalName}={e.Value.Trim()}"));

        violations.AddRange(project.Descendants("FrameworkReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .Where(name => name.StartsWith("Microsoft.WindowsDesktop.App", StringComparison.OrdinalIgnoreCase))
            .Select(name => $"FrameworkReference {name}"));

        violations.AddRange(project.Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .Where(name => ForbiddenUiPackagePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(name => $"PackageReference {name}"));

        Assert.True(
            violations.Count == 0,
            $"ADR-0002 violation: only App may use UI frameworks, but {projectName}.csproj has: {string.Join(", ", violations)}");
    }

    private static XDocument LoadProject(string projectName)
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", projectName, $"{projectName}.csproj");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Project file not found: {path}", path);
        }

        return XDocument.Load(path);
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"AssemblyStudio.slnx not found above {AppContext.BaseDirectory}. Run the architecture tests from the repository build output.");
    }
}
