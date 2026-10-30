using System.Globalization;
using System.IO;
using System.Text;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Documents;

/// <summary>
/// Turns a file exception into a message that names the path and the reason (FR-008, NFR-024): missing file or
/// folder, permission denied, read-only, locked by another program, disk full, invalid or too long path.
/// </summary>
public static class FileErrors
{
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);
    private const int DiskFull = unchecked((int)0x80070070);
    private const int HandleDiskFull = unchecked((int)0x80070027);

    /// <summary>The reason sentence for <paramref name="error"/> on <paramref name="path"/>.</summary>
    public static string Describe(Exception error, string path)
    {
        ArgumentNullException.ThrowIfNull(error);
        CompositeFormat format = error switch
        {
            FileNotFoundException => CompositeFormat.Parse(Strings.ErrNotFound),
            DirectoryNotFoundException => CompositeFormat.Parse(Strings.ErrFolderNotFound),
            PathTooLongException => CompositeFormat.Parse(Strings.ErrPathTooLong),
            UnauthorizedAccessException when IsReadOnly(path) => CompositeFormat.Parse(Strings.ErrReadOnly),
            UnauthorizedAccessException => CompositeFormat.Parse(Strings.ErrAccessDenied),
            IOException { HResult: SharingViolation or LockViolation } => CompositeFormat.Parse(Strings.ErrLocked),
            IOException { HResult: DiskFull or HandleDiskFull } => CompositeFormat.Parse(Strings.ErrDiskFull),
            ArgumentException or NotSupportedException => CompositeFormat.Parse(Strings.ErrInvalidPath),
            _ => CompositeFormat.Parse(Strings.ErrOther),
        };

        return string.Format(CultureInfo.CurrentCulture, format, path, error.Message);
    }

    /// <summary>Whether <paramref name="error"/> is one a file operation reports for a bad path or file (and so is shown, not thrown).</summary>
    public static bool IsFileError(Exception error) =>
        error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;

    private static bool IsReadOnly(string path)
    {
        try
        {
            return File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            return false;
        }
    }
}
