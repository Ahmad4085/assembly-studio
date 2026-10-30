using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AssemblyStudio.App.Documents;

namespace AssemblyStudio.App.Settings;

/// <summary>The user's settings and their persistence (FR-080, FR-081, FR-082; ADR-0007 §4).</summary>
public interface ISettingsService
{
    /// <summary>The current settings.</summary>
    AppSettings Current { get; }

    /// <summary>When the settings file couldn't be used at startup: the path of the backup kept of it (FR-081).
    /// <see langword="null"/> when the file was fine or absent.</summary>
    string? InvalidFileBackup { get; }

    /// <summary>Raised after <see cref="Current"/> changed.</summary>
    event EventHandler? Changed;

    /// <summary>Saves <paramref name="settings"/> and makes them current.</summary>
    void Save(AppSettings settings);
}

/// <summary>
/// Stores the settings as the <c>"settings"</c> object of <c>%APPDATA%\AssemblyStudio\settings.json</c>, next to the
/// layout and recent files, which are kept. Nothing leaves the machine (NFR-009).
/// <para>
/// FR-081: a settings file that isn't a JSON object, or whose <c>"settings"</c> object can't be read, is set aside as
/// <c>settings.invalid-&lt;timestamp&gt;.json</c> before anything else reads the file, the application starts with
/// defaults, and <see cref="InvalidFileBackup"/> tells the shell to notify the user. Single values out of range (a
/// hand-edited font size of 500, say) are replaced by their defaults without a notice.
/// </para>
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private const string Key = "settings";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    private readonly string _path;

    /// <summary>Loads the settings from <paramref name="settingsPath"/>, recovering from an invalid file.</summary>
    public JsonSettingsService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _path = Path.GetFullPath(settingsPath);
        (Current, InvalidFileBackup) = Load(_path);
    }

    /// <inheritdoc />
    public AppSettings Current { get; private set; }

    /// <inheritdoc />
    public string? InvalidFileBackup { get; }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings = settings.Sanitized();
        JsonObject root;
        try
        {
            root = File.Exists(_path) && JsonNode.Parse(File.ReadAllText(_path)) is JsonObject existing ? existing : [];
        }
        catch (JsonException)
        {
            root = []; // another writer broke the file after startup: start it again rather than fail the save
        }

        root[Key] = JsonSerializer.SerializeToNode(settings, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        AtomicFile.Write(_path, Encoding.UTF8.GetBytes(root.ToJsonString(Options)));
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static (AppSettings Settings, string? Backup) Load(string path)
    {
        string text;
        try
        {
            if (!File.Exists(path))
            {
                return (AppSettings.Default, null);
            }

            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (AppSettings.Default, null); // unreadable now (locked, no access): don't move it, just use defaults
        }

        try
        {
            if (JsonNode.Parse(text) is not JsonObject root)
            {
                throw new JsonException("The settings file is not a JSON object.");
            }

            if (root[Key] is not JsonNode node)
            {
                return (AppSettings.Default, null);
            }

            AppSettings settings = Merge(node);
            return (settings.Sanitized(), null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or NotSupportedException)
        {
            return (AppSettings.Default, SetAside(path));
        }
    }

    /// <summary>Reads the stored object over the defaults, so a file written by an older version (missing new
    /// settings) keeps its values and gets defaults for the rest.</summary>
    private static AppSettings Merge(JsonNode stored)
    {
        if (stored is not JsonObject values)
        {
            throw new JsonException("The \"settings\" value is not an object.");
        }

        JsonObject merged = JsonSerializer.SerializeToNode(AppSettings.Default, Options)!.AsObject();
        foreach ((string name, JsonNode? value) in values)
        {
            if (merged.ContainsKey(name))
            {
                merged[name] = value?.DeepClone();
            }
        }

        return merged.Deserialize<AppSettings>(Options) ?? throw new JsonException("The settings are empty.");
    }

    /// <summary>Renames the invalid file to a timestamped backup (ADR-0007 §4); returns its path, or the original path
    /// when it can't be moved (it is then left untouched and the notice still names it).</summary>
    private static string SetAside(string path)
    {
        string backup = Path.Combine(Path.GetDirectoryName(path)!, $"settings.invalid-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        try
        {
            File.Move(path, backup, overwrite: true);
            return backup;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return path;
        }
    }
}
