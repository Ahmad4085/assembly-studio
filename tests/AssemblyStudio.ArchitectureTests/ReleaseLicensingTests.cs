using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace AssemblyStudio.ArchitectureTests;

/// <summary>
/// TASK-102 (FR-090, NFR-030, NFR-031; AC-042, AC-061; ADR-0009, D-025, D-026): the notices, the installer's license page, the
/// license inventory and the SBOM written by <c>build/installer/license-inventory.ps1</c> agree with the pinned dependencies,
/// Microsoft's per-file licenses and the bundled NASM's checksums.
/// </summary>
public sealed partial class ReleaseLicensingTests
{
    private static readonly string Root = FindRepositoryRoot();

    private static string Read(params string[] path) => File.ReadAllText(Path.Combine([Root, .. path]));

    /// <summary>The NuGet packages the shipped projects reference, with their pinned versions.</summary>
    private static Dictionary<string, string> ShippedPackages()
    {
        var pinned = XDocument.Load(Path.Combine(Root, "Directory.Packages.props")).Descendants("PackageVersion")
            .ToDictionary(e => (string)e.Attribute("Include")!, e => (string)e.Attribute("Version")!);
        return Directory.GetFiles(Path.Combine(Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .SelectMany(p => XDocument.Load(p).Descendants("PackageReference").Select(e => (string)e.Attribute("Include")!))
            .Distinct()
            .ToDictionary(n => n, n => pinned[n]);
    }

    [Fact]
    public void AC_061_every_shipped_package_is_pinned_to_an_exact_version_and_inventoried_with_source_license_and_maintenance()
    {
        string inventory = Read("docs", "09-release", "license-inventory.md");

        foreach ((string name, string version) in ShippedPackages())
        {
            Assert.Matches(@"^\d+(\.\d+){1,3}$", version); // no range, no wildcard
            string row = Assert.Single(inventory.Split('\n'), l => l.StartsWith($"| {name} | {version} | https://www.nuget.org/packages/{name}/{version} |", StringComparison.Ordinal));
            Assert.Equal(8, row.Split('|').Length - 1); // component, version, source, license, files, obligations, maintenance
            Assert.DoesNotContain("|  |", row, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AC_061_the_bundled_NASM_matches_its_recorded_checksums()
    {
        string tools = Path.Combine(Root, "tools", "nasm");
        foreach (Match m in ChecksumLine().Matches(Read("tools", "nasm", "SHA256SUMS.txt")))
        {
            using FileStream file = File.OpenRead(Path.Combine(tools, m.Groups[2].Value));
            Assert.Equal(m.Groups[1].Value, Convert.ToHexStringLower(SHA256.HashData(file)));
        }
    }

    [Fact]
    public void AC_061_the_SBOM_is_SPDX_with_every_component_and_a_SHA256_for_every_file()
    {
        using JsonDocument sbom = JsonDocument.Parse(Read("docs", "09-release", "sbom.spdx.json"));
        JsonElement doc = sbom.RootElement;
        string inventory = Read("docs", "09-release", "license-inventory.md");
        int files = int.Parse(FilesCount().Match(inventory).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal("SPDX-2.3", doc.GetProperty("spdxVersion").GetString());
        var packages = doc.GetProperty("packages").EnumerateArray().ToDictionary(p => p.GetProperty("name").GetString()!, p => p.GetProperty("versionInfo").GetString());
        foreach ((string name, string version) in ShippedPackages())
        {
            Assert.Equal(version, packages[name]);
        }

        Assert.Equal(files, doc.GetProperty("files").GetArrayLength());
        Assert.All(doc.GetProperty("files").EnumerateArray(), f => Assert.Matches("^[0-9a-f]{64}$", f.GetProperty("checksums")[0].GetProperty("checksumValue").GetString()!));
    }

    [Fact]
    public void AC_042_the_notices_start_with_the_product_license_and_name_every_component_of_the_inventory()
    {
        string notices = Read("THIRD-PARTY-NOTICES.txt").ReplaceLineEndings("\n");
        string inventory = Read("docs", "09-release", "license-inventory.md");

        Assert.Contains(Read("LICENSE").ReplaceLineEndings("\n").TrimEnd(), notices, StringComparison.Ordinal);
        foreach (Match row in ComponentRow().Matches(inventory))
        {
            Assert.Contains($"\n{row.Groups[1].Value} {row.Groups[2].Value}\n", notices, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void D_026_the_notices_and_the_installer_license_page_carry_the_Microsoft_terms_for_the_files_they_cover()
    {
        string notices = Read("THIRD-PARTY-NOTICES.txt");
        string agreement = Read("build", "installer", "license-agreement.txt");
        string[] covered = [.. MicrosoftFile().Matches(Read("build", "installer", "licenses", "license-information-windows.md")).Select(m => m.Groups[1].Value.Replace("{x86|amd64|arm|arm64}", "amd64", StringComparison.Ordinal))];

        Assert.Equal(6, covered.Length);
        foreach (string text in new[] { notices, agreement })
        {
            Assert.Contains("MICROSOFT .NET LIBRARY", text, StringComparison.Ordinal);
            Assert.Contains("MICROSOFT WINDOWS SOFTWARE DEVELOPMENT KIT (SDK) FOR WINDOWS 10", text, StringComparison.Ordinal);
            Assert.All(covered, f => Assert.Contains(f, text, StringComparison.Ordinal));
        }

        Assert.StartsWith("Assembly Studio is open source under the MIT License:", agreement, StringComparison.Ordinal);
        Assert.Contains("\nLicenseFile=license-agreement.txt", Read("build", "installer", "AssemblyStudio.iss").ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^([0-9a-f]{64})\s+(\S+)$", RegexOptions.Multiline)]
    private static partial Regex ChecksumLine();

    [GeneratedRegex(@"\| Files \| (\d+) published files")]
    private static partial Regex FilesCount();

    [GeneratedRegex(@"^\| ([^|]+?) \| ([0-9][^|]*?) \| ", RegexOptions.Multiline)]
    private static partial Regex ComponentRow();

    [GeneratedRegex(@"^\* (\S+\.dll)", RegexOptions.Multiline)]
    private static partial Regex MicrosoftFile();

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException($"AssemblyStudio.slnx not found above {AppContext.BaseDirectory}.");
    }
}
