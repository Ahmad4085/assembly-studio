using System.IO;
using System.Text;
using System.Text.Json;

namespace AssemblyStudio.App.Documents.Recovery;

/// <summary>A document found in the recovery folder of an instance that ended unexpectedly (UI-021).</summary>
/// <param name="SessionFolder">The abandoned session's folder.</param>
/// <param name="Id">The document's id in that session.</param>
/// <param name="FilePath">The file it belonged to, or <see langword="null"/> when untitled.</param>
/// <param name="Title">The tab title it had.</param>
/// <param name="Text">The unsaved text.</param>
/// <param name="Encoding">The file's encoding.</param>
/// <param name="LineEnding">The file's line ending.</param>
/// <param name="SavedAt">When the autosave wrote it.</param>
public sealed record RecoveredDocument(
    string SessionFolder, Guid Id, string? FilePath, string Title, string Text, DocumentEncoding Encoding, LineEnding LineEnding, DateTimeOffset SavedAt);

/// <summary>
/// The recovery folder (NFR-011, architecture §5: <c>%LOCALAPPDATA%\AssemblyStudio\recovery\</c>). Each running instance
/// has its own session folder holding one autosave file per document with unsaved changes and a lock file the instance
/// keeps open. A session folder whose lock can be taken belongs to an instance that is gone: if it still holds files,
/// that instance ended unexpectedly (a clean exit deletes its folder), and its documents can be recovered. Another
/// running instance's folder stays locked, so its live work is never offered as a crash.
/// </summary>
public sealed class RecoveryStore : IDisposable
{
    private const string LockFile = "session.lock";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly string _root;
    private FileStream? _lock;

    /// <summary>Creates this instance's session folder under <paramref name="root"/> and locks it.</summary>
    public RecoveryStore(string root)
    {
        _root = Path.GetFullPath(root);
        SessionFolder = Path.Combine(_root, $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(SessionFolder);
        _lock = new FileStream(Path.Combine(SessionFolder, LockFile), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>The default recovery folder for the current user.</summary>
    public static string DefaultRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssemblyStudio", "recovery");

    /// <summary>This instance's session folder.</summary>
    public string SessionFolder { get; }

    /// <summary>Writes (atomically) the autosave file of a document with unsaved changes.</summary>
    public void Write(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var entry = new Entry(document.Id, document.FilePath, document.Title, document.Text,
            document.Encoding.Kind, document.Encoding.CodePage, document.LineEnding, DateTimeOffset.Now);
        AtomicFile.Write(EntryPath(document.Id), Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, Json)));
    }

    /// <summary>Removes a document's autosave file (saved, closed or clean again).</summary>
    public void Remove(Guid id)
    {
        string path = EntryPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>The documents in session folders of instances that are gone, oldest session first. Folders with nothing
    /// to recover are deleted; unreadable entries are skipped.</summary>
    public IReadOnlyList<RecoveredDocument> FindAbandoned()
    {
        var found = new List<RecoveredDocument>();
        if (!Directory.Exists(_root))
        {
            return found;
        }

        foreach (string folder in Directory.GetDirectories(_root).Order(StringComparer.Ordinal))
        {
            if (string.Equals(folder, SessionFolder, StringComparison.OrdinalIgnoreCase) || IsInUse(folder))
            {
                continue;
            }

            List<RecoveredDocument> documents = [.. Directory.GetFiles(folder, "*.json").Select(f => Read(folder, f)).OfType<RecoveredDocument>()];
            if (documents.Count == 0)
            {
                TryDeleteFolder(folder);
            }

            found.AddRange(documents.OrderBy(d => d.SavedAt));
        }

        return found;
    }

    /// <summary>Deletes the abandoned session folders of <paramref name="documents"/> (after Restore or Discard).</summary>
    public static void DeleteSessions(IEnumerable<RecoveredDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        foreach (string folder in documents.Select(d => d.SessionFolder).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TryDeleteFolder(folder);
        }
    }

    /// <summary>A clean exit: releases the lock and deletes this session's folder, so nothing is offered next time.</summary>
    public void Dispose()
    {
        if (_lock is null)
        {
            return;
        }

        _lock.Dispose();
        _lock = null;
        TryDeleteFolder(SessionFolder);
    }

    /// <summary>Releases the lock but keeps the files, as a process that ends unexpectedly does (tests only).</summary>
    internal void Abandon()
    {
        _lock?.Dispose();
        _lock = null;
    }

    private string EntryPath(Guid id) => Path.Combine(SessionFolder, $"{id:N}.json");

    private static bool IsInUse(string folder)
    {
        string lockPath = Path.Combine(folder, LockFile);
        if (!File.Exists(lockPath))
        {
            return false;
        }

        try
        {
            using var probe = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false; // nobody holds it: the instance is gone
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static RecoveredDocument? Read(string folder, string file)
    {
        try
        {
            Entry? entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(file), Json);
            return entry is null
                ? null
                : new RecoveredDocument(folder, entry.Id, entry.FilePath, entry.Title, entry.Text,
                    new DocumentEncoding(entry.EncodingKind, entry.CodePage), entry.LineEnding, entry.SavedAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left for the next start; recovery must never block the application.
        }
    }

    private sealed record Entry(
        Guid Id, string? FilePath, string Title, string Text, TextEncodingKind EncodingKind, int CodePage, LineEnding LineEnding, DateTimeOffset SavedAt);
}
