using Xunit;

namespace AssemblyStudio.Engine.I8086.Tests.Oracle;

public class SingleStepOracleHelperTests
{
    [Fact]
    public void LocateDataDirectory_finds_valid_oracle_data_and_metadata()
    {
        var dataDir = SingleStepOracleHelper.LocateDataDirectory();

        Assert.True(Directory.Exists(dataDir), $"Directory does not exist: {dataDir}");
        var metadataPath = Path.Combine(dataDir, "metadata.json");
        Assert.True(File.Exists(metadataPath), $"metadata.json not found: {metadataPath}");

        var sourcePath = Path.Combine(dataDir, "SOURCE.txt");
        Assert.True(File.Exists(sourcePath), $"SOURCE.txt not found: {sourcePath}");
        var sourceContent = File.ReadAllText(sourcePath);
        Assert.Contains("license=MIT", sourceContent);
        Assert.Contains(SingleStepOracleHelper.PinnedCommit, sourceContent);
    }

    [Fact]
    public void LocateDataDirectory_fails_with_clear_instructions_when_data_is_absent()
    {
        var emptyTempDir = Path.Combine(Path.GetTempPath(), "AssemblyStudio_EmptyTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyTempDir);

        try
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                SingleStepOracleHelper.LocateDataDirectory(emptyTempDir);
            });

            Assert.Contains("SingleStepTests 8086 oracle data not found", ex.Message);
            Assert.Contains("build/fetch-singlesteptests.ps1", ex.Message);
            Assert.Contains("ADR-0008", ex.Message);
        }
        finally
        {
            if (Directory.Exists(emptyTempDir))
            {
                Directory.Delete(emptyTempDir, true);
            }
        }
    }

    [Theory]
    [InlineData("00")]
    [InlineData("01")]
    [InlineData("88")]
    public void GetOpcodeTestFilePath_locates_existing_oracle_files(string opcodeHex)
    {
        var path = SingleStepOracleHelper.GetOpcodeTestFilePath(opcodeHex);
        Assert.True(File.Exists(path), $"File should exist: {path}");
    }

    [Fact]
    public void GetOpcodeTestFilePath_fails_with_clear_instructions_for_missing_opcode()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
        {
            SingleStepOracleHelper.GetOpcodeTestFilePath("NON_EXISTENT_OPCODE");
        });

        Assert.Contains("build/fetch-singlesteptests.ps1", ex.Message);
    }
}
