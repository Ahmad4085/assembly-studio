using System.IO;
using System.Text.Json.Nodes;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>Layout persistence in the settings file (TASK-060, UI-001; ADR-0007 §4).</summary>
public sealed class JsonLayoutStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "AssemblyStudio.Tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Missing_file_gives_the_default_layout()
    {
        Assert.Equal(LayoutState.Default, new JsonLayoutStore(SettingsPath).Load());
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        var layout = LayoutState.Default with { LearnWidth = 250, IsLearnVisible = true, BottomTab = BottomTab.Runtime, IsMaximized = true };

        new JsonLayoutStore(SettingsPath).Save(layout);

        Assert.Equal(layout, new JsonLayoutStore(SettingsPath).Load());
    }

    [Fact]
    public void Save_keeps_other_settings_in_the_file()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "editor": { "fontSize": 16 } }""");

        new JsonLayoutStore(SettingsPath).Save(LayoutState.Default);

        JsonObject root = JsonNode.Parse(File.ReadAllText(SettingsPath))!.AsObject();
        Assert.Equal(16, root["editor"]!["fontSize"]!.GetValue<int>());
        Assert.NotNull(root["layout"]);
    }

    [Fact]
    public void Invalid_file_gives_defaults_and_is_kept_as_a_backup_on_save()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, "{ not json");
        var store = new JsonLayoutStore(SettingsPath);

        Assert.Equal(LayoutState.Default, store.Load());
        store.Save(LayoutState.Default);

        string backup = Assert.Single(Directory.GetFiles(_dir, "settings.invalid-*.json"));
        Assert.Equal("{ not json", File.ReadAllText(backup));
        Assert.Equal(LayoutState.Default, new JsonLayoutStore(SettingsPath).Load());
    }

    [Fact]
    public void Out_of_range_values_are_sanitized()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """
            { "layout": { "learnWidth": -5, "machineWidth": 99999, "bottomHeight": 220, "memoryStackWidth": 520,
              "isLearnVisible": false, "isMachineVisible": true, "isBottomVisible": true, "bottomTab": "console",
              "windowWidth": 10, "windowHeight": 900, "isMaximized": false } }
            """);

        LayoutState layout = new JsonLayoutStore(SettingsPath).Load();

        Assert.Equal(120, layout.LearnWidth);
        Assert.Equal(800, layout.MachineWidth);
        Assert.Equal(LayoutState.MinWindowWidth, layout.WindowWidth);
    }

    [Fact]
    public void A_layout_object_with_wrong_types_gives_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "layout": { "learnWidth": "wide" } }""");

        Assert.Equal(LayoutState.Default, new JsonLayoutStore(SettingsPath).Load());
    }
}
