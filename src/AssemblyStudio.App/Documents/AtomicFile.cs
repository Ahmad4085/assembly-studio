using System.IO;

namespace AssemblyStudio.App.Documents;

/// <summary>
/// Atomic file replacement (FR-004, NFR-029): the bytes go to a temporary file in the target's folder, are flushed
/// to disk, and only then replace the target in one operation. If anything fails before that, the target is
/// untouched and the temporary file is removed, so a failed save never leaves a partial or corrupted file.
/// </summary>
public static class AtomicFile
{
    /// <summary>Writes <paramref name="bytes"/> to <paramref name="path"/> atomically.</summary>
    /// <param name="path">The target file.</param>
    /// <param name="bytes">The new content.</param>
    /// <param name="writeContent">Writes the content to the temporary file; tests pass one that fails part-way.</param>
    /// <exception cref="IOException">The write or the replacement failed (the target is unchanged).</exception>
    /// <exception cref="UnauthorizedAccessException">Access denied, or the target is read-only (the target is unchanged).</exception>
    public static void Write(string path, byte[] bytes, Action<Stream, byte[]>? writeContent = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        string target = Path.GetFullPath(path);
        string folder = Path.GetDirectoryName(target) ?? throw new IOException($"{target} has no folder.");
        if (File.Exists(target) && File.GetAttributes(target).HasFlag(FileAttributes.ReadOnly))
        {
            throw new UnauthorizedAccessException($"{target} is read-only.");
        }

        string temporary = Path.Combine(folder, $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                (writeContent ?? WriteAll)(stream, bytes);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(target))
            {
                File.Replace(temporary, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporary, target);
            }
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
    }

    private static void WriteAll(Stream stream, byte[] bytes) => stream.Write(bytes);

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leave it: the target is what matters, and it is unchanged.
        }
    }
}
