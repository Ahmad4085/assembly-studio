using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AssemblyStudio.App.Shell;

/// <summary>Loads and saves the shell layout (UI-001).</summary>
public interface ILayoutStore
{
    /// <summary>The saved layout, or <see cref="LayoutState.Default"/> when none is saved or the
    /// saved one can't be read.</summary>
    LayoutState Load();

    /// <summary>Saves the layout.</summary>
    void Save(LayoutState layout);
}

/// <summary>
/// Stores the layout as the <c>"layout"</c> object of the settings file
/// (<c>%APPDATA%\AssemblyStudio\settings.json</c>, ADR-0007 §4). Other top-level properties are
/// preserved, so the settings service (TASK-067) can share the file. An unreadable file yields
/// the default layout and is renamed to <c>settings.invalid-&lt;timestamp&gt;.json</c> on the next
/// save instead of being overwritten (FR-081 notification arrives with TASK-067).
/// </summary>
public sealed class JsonLayoutStore(string settingsPath) : ILayoutStore
{
    private const string LayoutKey = "layout";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private bool _fileIsInvalid;

    /// <summary>The default settings file path for the current user.</summary>
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssemblyStudio", "settings.json");

    /// <inheritdoc />
    public LayoutState Load()
    {
        JsonObject? root = ReadRoot();
        if (root?[LayoutKey] is not JsonObject layout)
        {
            return LayoutState.Default;
        }

        try
        {
            return (layout.Deserialize<LayoutState>(Options) ?? LayoutState.Default).Sanitized();
        }
        catch (JsonException)
        {
            return LayoutState.Default;
        }
    }

    /// <inheritdoc />
    public void Save(LayoutState layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        JsonObject root = ReadRoot() ?? new JsonObject();
        if (_fileIsInvalid)
        {
            // Keep the unreadable file for the user instead of overwriting it (ADR-0007 §4).
            string backup = Path.Combine(
                Path.GetDirectoryName(settingsPath)!,
                $"settings.invalid-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(settingsPath, backup, overwrite: true);
            _fileIsInvalid = false;
        }

        root[LayoutKey] = JsonSerializer.SerializeToNode(layout, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

        // Write to a temporary file first so a crash mid-write never leaves a truncated file.
        string temp = settingsPath + ".tmp";
        File.WriteAllText(temp, root.ToJsonString(Options));
        File.Move(temp, settingsPath, overwrite: true);
    }

    private JsonObject? ReadRoot()
    {
        if (!File.Exists(settingsPath))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject
                   ?? throw new JsonException("The settings file is not a JSON object.");
        }
        catch (JsonException)
        {
            _fileIsInvalid = true;
            return null;
        }
    }
}
