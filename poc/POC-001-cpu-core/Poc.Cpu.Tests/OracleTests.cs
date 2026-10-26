using Xunit;

namespace Poc.Cpu.Tests;

/// <summary>Every in-scope SingleStepTests 8086 file must pass completely (exit criterion 1).</summary>
public class OracleTests
{
    public static string TestDataDir
    {
        get
        {
            // tests run from Poc.Cpu.Tests/bin/<cfg>/net10.0/; testdata lives next to the solution.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "testdata"))) dir = dir.Parent;
            return dir is null
                ? throw new DirectoryNotFoundException("testdata/ not found: run fetch-testdata.ps1 first.")
                : Path.Combine(dir.FullName, "testdata");
        }
    }

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string f in Directory.GetFiles(TestDataDir, "*.json.gz").Select(Path.GetFileName).Order()!)
            data.Add(f!);
        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Opcode_file_passes_completely(string file)
    {
        var result = SingleStepTests.Run(TestDataDir, file);
        Assert.True(result.Total > 0, $"{file}: no test cases");
        Assert.True(result.Passed == result.Total,
            $"{file}: {result.Passed}/{result.Total} passed. First failures:\n  " + string.Join("\n  ", result.Failures));
    }
}
