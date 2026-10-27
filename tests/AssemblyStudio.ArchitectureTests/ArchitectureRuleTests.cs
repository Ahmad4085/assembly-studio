using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace AssemblyStudio.ArchitectureTests;

public class ArchitectureRuleTests
{
    private static readonly string[] ProductAssemblyNames =
    [
        "AssemblyStudio.Core",
        "AssemblyStudio.Engine.I8086",
        "AssemblyStudio.Runtime",
        "AssemblyStudio.Os.Dos",
        "AssemblyStudio.Toolchain.Nasm",
        "AssemblyStudio.Debugging",
        "AssemblyStudio.Content",
        "AssemblyStudio.App"
    ];

    private static readonly string[] ForbiddenUiAssemblies =
    [
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "AvalonEdit",
        "System.Windows.Forms"
    ];

    internal static readonly Dictionary<string, string[]> AllowedProductDependencies = new()
    {
        ["AssemblyStudio.Core"] = [],
        ["AssemblyStudio.Engine.I8086"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.Runtime"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.Os.Dos"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.Toolchain.Nasm"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.Debugging"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.Content"] = ["AssemblyStudio.Core"],
        ["AssemblyStudio.App"] =
        [
            "AssemblyStudio.Core",
            "AssemblyStudio.Engine.I8086",
            "AssemblyStudio.Runtime",
            "AssemblyStudio.Os.Dos",
            "AssemblyStudio.Toolchain.Nasm",
            "AssemblyStudio.Debugging",
            "AssemblyStudio.Content"
        ]
    };

    [Theory]
    [InlineData("AssemblyStudio.Core")]
    [InlineData("AssemblyStudio.Engine.I8086")]
    [InlineData("AssemblyStudio.Runtime")]
    [InlineData("AssemblyStudio.Os.Dos")]
    [InlineData("AssemblyStudio.Toolchain.Nasm")]
    [InlineData("AssemblyStudio.Debugging")]
    [InlineData("AssemblyStudio.Content")]
    public void Non_app_assemblies_do_not_reference_ui_frameworks(string assemblyName)
    {
        var referencedAssemblies = GetReferencedAssemblies(assemblyName);

        var forbiddenFound = referencedAssemblies
            .Where(r => ForbiddenUiAssemblies.Any(ui => string.Equals(r, ui, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(
            forbiddenFound.Count == 0,
            $"ADR-0002 violation: {assemblyName} references forbidden UI assembly: {string.Join(", ", forbiddenFound)}");
    }

    [Theory]
    [InlineData("AssemblyStudio.Core")]
    [InlineData("AssemblyStudio.Engine.I8086")]
    [InlineData("AssemblyStudio.Runtime")]
    [InlineData("AssemblyStudio.Os.Dos")]
    [InlineData("AssemblyStudio.Toolchain.Nasm")]
    [InlineData("AssemblyStudio.Debugging")]
    [InlineData("AssemblyStudio.Content")]
    public void Non_app_assemblies_do_not_reference_App(string assemblyName)
    {
        var referencedAssemblies = GetReferencedAssemblies(assemblyName);

        Assert.DoesNotContain("AssemblyStudio.App", referencedAssemblies);
    }

    [Fact]
    public void Debugging_does_not_reference_Engine_or_Runtime()
    {
        var referencedAssemblies = GetReferencedAssemblies("AssemblyStudio.Debugging");

        Assert.DoesNotContain("AssemblyStudio.Engine.I8086", referencedAssemblies);
        Assert.DoesNotContain("AssemblyStudio.Runtime", referencedAssemblies);
        Assert.DoesNotContain("AssemblyStudio.Os.Dos", referencedAssemblies);
        Assert.DoesNotContain("AssemblyStudio.Toolchain.Nasm", referencedAssemblies);
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
    public void Product_assemblies_only_reference_allowed_product_assemblies(string assemblyName)
    {
        var referencedAssemblies = GetReferencedAssemblies(assemblyName);
        var allowed = AllowedProductDependencies[assemblyName];

        var referencedProductAssemblies = referencedAssemblies
            .Where(r => ProductAssemblyNames.Contains(r))
            .ToList();

        var disallowed = referencedProductAssemblies
            .Except(allowed)
            .ToList();

        Assert.True(
            disallowed.Count == 0,
            $"ADR-0002 graph violation: {assemblyName} references disallowed product assemblies: {string.Join(", ", disallowed)}");
    }

    public static IReadOnlyList<string> GetReferencedAssemblies(string assemblyName)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly under test not found: {assemblyPath}", assemblyPath);
        }

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadataReader = peReader.GetMetadataReader();

        return metadataReader.AssemblyReferences
            .Select(handle => metadataReader.GetString(metadataReader.GetAssemblyReference(handle).Name))
            .ToList();
    }
}
