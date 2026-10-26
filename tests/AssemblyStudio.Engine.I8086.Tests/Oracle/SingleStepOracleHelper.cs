namespace AssemblyStudio.Engine.I8086.Tests.Oracle;

/// <summary>
/// Locates and validates SingleStepTests 8086 oracle data per ADR-0008.
/// Never skips when data is missing; fails with actionable instructions.
/// </summary>
public static class SingleStepOracleHelper
{
    public const string PinnedCommit = "e71c68d215a6bb8c356bd4cb3842de3bef345ca9";

    public static string LocateDataDirectory(string? overrideStartingDir = null)
    {
        var startDir = overrideStartingDir != null
            ? new DirectoryInfo(overrideStartingDir)
            : new DirectoryInfo(AppContext.BaseDirectory);

        var current = startDir;
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "testdata", "singlesteptests", "8086");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "metadata.json")))
            {
                return candidate;
            }

            var candidateDirect = Path.Combine(current.FullName, "testdata", "singlesteptests");
            if (Directory.Exists(candidateDirect) && File.Exists(Path.Combine(candidateDirect, "metadata.json")))
            {
                return candidateDirect;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(
            "SingleStepTests 8086 oracle data not found. " +
            "Per ADR-0008, hardware test data must be fetched before running engine tests.\n" +
            "Please run: powershell -ExecutionPolicy Bypass -File build/fetch-singlesteptests.ps1");
    }

    public static string GetOpcodeTestFilePath(string opcodeHex, string? dataDir = null)
    {
        var dir = dataDir ?? LocateDataDirectory();
        var fileName = $"{opcodeHex.ToUpperInvariant()}.json.gz";
        var filePath = Path.Combine(dir, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"SingleStepTests oracle file for opcode {opcodeHex} not found at {filePath}.\n" +
                "Please run: powershell -ExecutionPolicy Bypass -File build/fetch-singlesteptests.ps1",
                filePath);
        }

        return filePath;
    }
}
