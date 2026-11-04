using Xunit;

namespace AssemblyStudio.ArchitectureTests;

/// <summary>
/// TASK-100 (NFR-030, D-025): the repository carries the product license chosen by the user, MIT with the copyright
/// holder "Assembly Studio contributors".
/// </summary>
public sealed class LicenseTests
{
    [Fact]
    public void TASK_100_the_repository_is_MIT_licensed_to_the_Assembly_Studio_contributors()
    {
        string path = Path.Combine(FindRepositoryRoot(), "LICENSE");
        Assert.True(File.Exists(path), $"{path} is missing (D-025).");
        string[] lines = File.ReadAllLines(path);

        Assert.Equal("MIT License", lines[0]);
        Assert.Contains("Copyright (c) 2026 Assembly Studio contributors", lines);
        Assert.Contains("The above copyright notice and this permission notice shall be included in all", lines);
        Assert.Contains("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR", lines);
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

        throw new DirectoryNotFoundException($"AssemblyStudio.slnx not found above {AppContext.BaseDirectory}.");
    }
}
