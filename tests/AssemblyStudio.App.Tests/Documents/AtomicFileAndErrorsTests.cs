using System.IO;
using AssemblyStudio.App.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>TASK-062, FR-004/FR-008/NFR-029: atomic saves and file error messages that name the path and reason.</summary>
public sealed class AtomicFileAndErrorsTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task062-").FullName;

    public void Dispose()
    {
        foreach (string file in Directory.GetFiles(_folder))
        {
            System.IO.File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_folder, recursive: true);
    }

    private string Existing(string name, string content)
    {
        string path = Path.Combine(_folder, name);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Writes_a_new_file_and_replaces_an_existing_one()
    {
        string path = Path.Combine(_folder, "new.asm");

        AtomicFile.Write(path, "one"u8.ToArray());
        AtomicFile.Write(path, "two"u8.ToArray());

        Assert.Equal("two", System.IO.File.ReadAllText(path));
        Assert.Equal(["new.asm"], Directory.GetFiles(_folder).Select(Path.GetFileName));
    }

    [Fact]
    public void AC_004_a_write_that_fails_part_way_leaves_the_original_unchanged_and_no_temporary_file()
    {
        string path = Existing("program.asm", "original content");
        byte[] replacement = new byte[10_000];

        IOException error = Assert.Throws<IOException>(() => AtomicFile.Write(path, replacement, (stream, bytes) =>
        {
            stream.Write(bytes, 0, bytes.Length / 2);
            throw new IOException("Simulated: the disk is full.");
        }));

        Assert.Equal("Simulated: the disk is full.", error.Message);
        Assert.Equal("original content", System.IO.File.ReadAllText(path));
        Assert.Equal(["program.asm"], Directory.GetFiles(_folder).Select(Path.GetFileName));
    }

    [Fact]
    public void A_read_only_target_is_refused_and_unchanged()
    {
        string path = Existing("readonly.asm", "keep");
        System.IO.File.SetAttributes(path, FileAttributes.ReadOnly);

        var error = Assert.Throws<UnauthorizedAccessException>(() => AtomicFile.Write(path, "new"u8.ToArray()));

        Assert.Equal("keep", System.IO.File.ReadAllText(path));
        Assert.Contains("is read-only", FileErrors.Describe(error, path), StringComparison.Ordinal);
        Assert.Contains(path, FileErrors.Describe(error, path), StringComparison.Ordinal);
    }

    [Fact]
    public void A_target_locked_by_another_program_is_refused_and_unchanged()
    {
        string path = Existing("locked.asm", "keep");
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var error = Assert.ThrowsAny<IOException>(() => AtomicFile.Write(path, "new"u8.ToArray()));
            Assert.Contains("being used by another program", FileErrors.Describe(error, path), StringComparison.Ordinal);
        }

        Assert.Equal("keep", System.IO.File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Error_messages_name_the_path_and_reason()
    {
        const string path = @"C:\work\x.asm";

        Assert.Equal($"{path} doesn't exist.", FileErrors.Describe(new FileNotFoundException(), path));
        Assert.Equal($"The folder of {path} doesn't exist.", FileErrors.Describe(new DirectoryNotFoundException(), path));
        Assert.Equal($"You don't have permission to access {path}.", FileErrors.Describe(new UnauthorizedAccessException(), path));
        Assert.Equal($"There isn't enough disk space to write {path}.", FileErrors.Describe(new IOException("full", unchecked((int)0x80070070)), path));
        Assert.Equal($"{path} is being used by another program.", FileErrors.Describe(new IOException("busy", unchecked((int)0x80070020)), path));
        Assert.Equal($"{path} isn't a valid file path.", FileErrors.Describe(new ArgumentException(), path));
        Assert.Equal($"The path {path} is too long.", FileErrors.Describe(new PathTooLongException(), path));
        Assert.Equal($"{path} couldn't be accessed: device error", FileErrors.Describe(new IOException("device error"), path));
    }
}
