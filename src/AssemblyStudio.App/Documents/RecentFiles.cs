using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssemblyStudio.App.Documents;

/// <summary>Loads and saves the recent-files list (FR-007).</summary>
public interface IRecentFilesStore
{
    /// <summary>The saved list, most recent first (empty when none or unreadable).</summary>
    IReadOnlyList<string> Load();

    /// <summary>Saves the list.</summary>
    void Save(IReadOnlyList<string> files);
}

/// <summary>A recent-files store that keeps the list in memory only (tests, or when settings can't be written).</summary>
public sealed class MemoryRecentFilesStore : IRecentFilesStore
{
    private IReadOnlyList<string> _files = [];

    /// <inheritdoc />
    public IReadOnlyList<string> Load() => _files;

    /// <inheritdoc />
    public void Save(IReadOnlyList<string> files) => _files = [.. files];
}

/// <summary>
/// Stores the list as the <c>"recentFiles"</c> array of the settings file (ADR-0007 §4), keeping the other properties
/// (the layout, and later the settings service, TASK-067). An unreadable file yields an empty list and is left alone.
/// </summary>
public sealed class JsonRecentFilesStore(string settingsPath) : IRecentFilesStore
{
    private const string Key = "recentFiles";

    /// <inheritdoc />
    public IReadOnlyList<string> Load()
    {
        try
        {
            return File.Exists(settingsPath) && JsonNode.Parse(File.ReadAllText(settingsPath)) is JsonObject root && root[Key] is JsonArray array
                ? [.. array.Select(n => n?.GetValue<string>()).OfType<string>()]
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public void Save(IReadOnlyList<string> files)
    {
        JsonObject root;
        try
        {
            root = File.Exists(settingsPath) && JsonNode.Parse(File.ReadAllText(settingsPath)) is JsonObject existing ? existing : [];
        }
        catch (JsonException)
        {
            return; // never overwrite a settings file we can't read (the layout store renames it, TASK-060)
        }

        root[Key] = new JsonArray([.. files.Select(f => (JsonNode?)JsonValue.Create(f))]);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settingsPath))!);
        AtomicFile.Write(settingsPath, System.Text.Encoding.UTF8.GetBytes(root.ToJsonString(new JsonSerializerOptions { WriteIndented = true })));
    }
}
