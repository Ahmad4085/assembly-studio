using System.Security.Cryptography;
using Xunit;

namespace AssemblyStudio.Toolchain.Nasm.Tests;

public class NasmLocatorTests
{
    [Fact]
    public void Locates_bundled_nasm_and_verifies_version_3_02()
    {
        var nasmPath = NasmLocator.GetNasmPath();

        Assert.True(File.Exists(nasmPath), $"nasm.exe should exist at {nasmPath}");

        var versionString = NasmLocator.GetVersion(nasmPath);
        Assert.Contains("NASM version 3.02", versionString);
    }

    [Fact]
    public void Locates_bundled_ndisasm()
    {
        var ndisasmPath = NasmLocator.GetNdisasmPath();

        Assert.True(File.Exists(ndisasmPath), $"ndisasm.exe should exist at {ndisasmPath}");
    }

    [Fact]
    public void License_and_source_metadata_exist_and_record_bsd2_clause()
    {
        var dir = NasmLocator.LocateNasmDirectory();

        var licensePath = Path.Combine(dir, "LICENSE");
        Assert.True(File.Exists(licensePath), $"LICENSE not found at {licensePath}");
        var licenseText = File.ReadAllText(licensePath);
        Assert.Contains("Redistribution and use in source and binary forms", licenseText);

        var sourcePath = Path.Combine(dir, "SOURCE.txt");
        Assert.True(File.Exists(sourcePath), $"SOURCE.txt not found at {sourcePath}");
        var sourceText = File.ReadAllText(sourcePath);
        Assert.Contains("version=3.02", sourceText);
        Assert.Contains("license=BSD-2-Clause", sourceText);
        Assert.Contains("nasm.us", sourceText);
    }

    [Fact]
    public void Sha256sums_manifest_exists_and_matches_all_bundled_files()
    {
        var dir = NasmLocator.LocateNasmDirectory();
        var sumsPath = Path.Combine(dir, "SHA256SUMS.txt");
        Assert.True(File.Exists(sumsPath), $"SHA256SUMS.txt not found at {sumsPath}");

        var lines = File.ReadAllLines(sumsPath);
        Assert.NotEmpty(lines);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Equal(2, parts.Length);

            var expectedHash = parts[0];
            var relativeFile = parts[1];
            var filePath = Path.Combine(dir, relativeFile);

            Assert.True(File.Exists(filePath), $"File {relativeFile} referenced in manifest does not exist at {filePath}");
            var actualBytes = File.ReadAllBytes(filePath);
            var actualHash = Convert.ToHexStringLower(SHA256.HashData(actualBytes));
            Assert.Equal(expectedHash, actualHash);
        }
    }

    [Fact]
    public void Fails_with_clear_instructions_when_nasm_is_absent()
    {
        var emptyTempDir = Path.Combine(Path.GetTempPath(), "AssemblyStudio_NasmMissingTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyTempDir);

        try
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                NasmLocator.LocateNasmDirectory(emptyTempDir);
            });

            Assert.Contains("Bundled NASM 3.02 not found", ex.Message);
            Assert.Contains("build/fetch-nasm.ps1", ex.Message);
            Assert.Contains("ADR-0005", ex.Message);
        }
        finally
        {
            if (Directory.Exists(emptyTempDir))
            {
                Directory.Delete(emptyTempDir, true);
            }
        }
    }
}
