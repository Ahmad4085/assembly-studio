using System.IO;
using System.Text.Json.Nodes;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using AssemblyStudio.Core;
using AssemblyStudio.Toolchain.Nasm;
using Xunit;

namespace AssemblyStudio.App.Tests.Settings;

/// <summary>
/// TASK-067: settings persistence and recovery (AC-039), editor preferences and reset (AC-053), the execution
/// preferences (AC-054, UI part) and assembler path validation in the dialog (AC-015, UI part).
/// </summary>
public sealed class SettingsTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-task067-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    /// <summary>The NASM copied next to the application build (FR-079), as the composition root finds it.</summary>
    internal static string BundledNasm => Path.Combine(AppContext.BaseDirectory, "tools", "nasm", "nasm.exe");

    internal static AssemblerInfo Check(string? configured) =>
        new NasmAssembler(new NasmOptions { BundledPath = BundledNasm, ConfiguredPath = configured }).Check();

    private static SettingsViewModel Dialog(ISettingsService settings, WorkspaceViewModelTests.FakeDialogs? dialogs = null, Action? resetLayout = null) =>
        new(settings, dialogs ?? new WorkspaceViewModelTests.FakeDialogs(), Check, resetLayout ?? (() => { }), ["Cascadia Mono", "Consolas"]);

    // ---- AC-039: persistence and recovery ----

    [Fact]
    public void Without_a_settings_file_the_defaults_apply_and_nothing_is_reported()
    {
        var settings = new JsonSettingsService(SettingsPath);

        Assert.Equal(AppSettings.Default, settings.Current);
        Assert.Null(settings.InvalidFileBackup);
        Assert.False(File.Exists(SettingsPath)); // nothing written until something changes
    }

    [Fact]
    public void AC_053_changed_editor_preferences_survive_a_restart_and_the_other_sections_of_the_file_are_kept()
    {
        new JsonLayoutStore(SettingsPath).Save(LayoutState.Default with { LearnWidth = 300 });
        new JsonRecentFilesStore(SettingsPath).Save([@"C:\work\a.asm"]);
        var changed = AppSettings.Default with
        {
            FontFamily = "Consolas", FontSize = 18, TabWidth = 8, UseSpaces = false, Theme = ThemePreference.Light,
        };

        new JsonSettingsService(SettingsPath).Save(changed);
        var restarted = new JsonSettingsService(SettingsPath);

        Assert.Equal(changed, restarted.Current);
        Assert.Equal(300, new JsonLayoutStore(SettingsPath).Load().LearnWidth);
        Assert.Equal([@"C:\work\a.asm"], new JsonRecentFilesStore(SettingsPath).Load());
        JsonObject saved = JsonNode.Parse(File.ReadAllText(SettingsPath))!["settings"]!.AsObject();
        Assert.Equal("light", (string?)saved["theme"]); // human-readable (FR-080)
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"settings\": 42 }")]
    [InlineData("{ \"settings\": { \"fontSize\": \"huge\" } }")]
    [InlineData("{ \"settings\": { \"theme\": \"purple\" } }")]
    public void AC_039_an_invalid_settings_file_is_kept_as_a_backup_and_the_defaults_apply(string content)
    {
        File.WriteAllText(SettingsPath, content);

        var settings = new JsonSettingsService(SettingsPath);

        Assert.Equal(AppSettings.Default, settings.Current);
        Assert.NotNull(settings.InvalidFileBackup);
        Assert.Matches(@"settings\.invalid-\d{8}-\d{6}\.json$", settings.InvalidFileBackup);
        Assert.Equal(content, File.ReadAllText(settings.InvalidFileBackup)); // the user's file is kept as it was
        Assert.False(File.Exists(SettingsPath));

        // The rest of the application starts clean, and saving works again.
        Assert.Equal(LayoutState.Default, new JsonLayoutStore(SettingsPath).Load());
        settings.Save(AppSettings.Default with { TabWidth = 2 });
        var restarted = new JsonSettingsService(SettingsPath);
        Assert.Equal(2, restarted.Current.TabWidth);
        Assert.Null(restarted.InvalidFileBackup);
    }

    [Fact]
    public void Values_out_of_range_get_their_defaults_and_settings_missing_from_an_older_file_are_added()
    {
        File.WriteAllText(SettingsPath, """
            { "settings": { "fontSize": 500, "tabWidth": 0, "instructionBudget": 5, "fontFamily": "  ", "useSpaces": false, "unknownFutureSetting": 1 } }
            """);

        var settings = new JsonSettingsService(SettingsPath);

        Assert.Null(settings.InvalidFileBackup);
        Assert.Equal(AppSettings.Default with { UseSpaces = false }, settings.Current);
    }

    [Fact]
    public void Settings_are_stored_under_the_users_application_data_folder_by_default()
    {
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssemblyStudio", "settings.json"),
            JsonLayoutStore.DefaultPath);
    }

    [Fact]
    public async Task AC_039_the_default_settings_build_a_program_with_the_bundled_assembler_without_configuration()
    {
        Assert.Null(AppSettings.Default.AssemblerPath);
        var nasm = new NasmAssembler(new NasmOptions { BundledPath = BundledNasm, ConfiguredPath = AppSettings.Default.AssemblerPath });

        AssemblerInfo info = nasm.Check();
        BuildResult result = await nasm.BuildAsync(new BuildRequest(null, Strings.TemplateCom), TestContext.Current.CancellationToken);

        Assert.Equal((AssemblerOrigin.Bundled, AssemblerStatus.Ready), (info.Origin, info.Status));
        Assert.True(result.Outcome == BuildOutcome.Succeeded, result.Output);
        Assert.False(result.Image.IsEmpty);
    }

    // ---- The dialog ----

    [Fact]
    public void AC_015_the_dialog_shows_the_assembler_in_use_with_its_origin_and_version()
    {
        SettingsViewModel dialog = Dialog(new JsonSettingsService(SettingsPath));

        Assert.Equal("", dialog.AssemblerPath);
        Assert.StartsWith($"{Strings.SettingsOriginBundled}: {BundledNasm} (NASM ", dialog.AssemblerSummary, StringComparison.Ordinal);
        Assert.False(dialog.HasAssemblerError);
    }

    [Theory]
    [InlineData("nasm.exe")]                  // not a full path
    [InlineData(@"C:\no\such\folder\nasm.exe")]
    [InlineData(@"\\server\share\nasm.exe")]  // network paths are refused
    public void AC_015_an_invalid_assembler_path_shows_an_error_and_is_not_saved(string path)
    {
        var settings = new JsonSettingsService(SettingsPath);
        SettingsViewModel dialog = Dialog(settings);
        bool? closed = null;
        dialog.CloseRequested += (_, saved) => closed = saved;
        dialog.AssemblerPath = path;
        dialog.FontSize = "20";

        Assert.False(dialog.Save());

        Assert.True(dialog.HasAssemblerError);
        Assert.Null(closed);
        Assert.Equal(AppSettings.Default, settings.Current);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void AC_015_a_file_that_is_not_a_working_assembler_is_rejected_after_running_it()
    {
        string fake = Path.Combine(_folder, "nasm.exe");
        File.WriteAllText(fake, "not a program");
        var settings = new JsonSettingsService(SettingsPath);
        SettingsViewModel dialog = Dialog(settings);
        dialog.AssemblerPath = fake;

        dialog.CheckPathCommand.Execute(null);
        Assert.True(dialog.HasAssemblerError);
        Assert.StartsWith($"{Strings.SettingsOriginConfigured}: ", dialog.AssemblerSummary, StringComparison.Ordinal);
        Assert.False(dialog.Save());
        Assert.Null(settings.Current.AssemblerPath);
    }

    [Fact]
    public void AC_015_a_working_assembler_path_is_checked_saved_and_used_after_a_restart()
    {
        string copy = Path.Combine(_folder, "my nasm", "nasm.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.Copy(BundledNasm, copy);
        var dialogs = new WorkspaceViewModelTests.FakeDialogs();
        dialogs.ExecutableAnswers.Enqueue(copy);
        var settings = new JsonSettingsService(SettingsPath);
        SettingsViewModel dialog = Dialog(settings, dialogs);
        bool? closed = null;
        dialog.CloseRequested += (_, saved) => closed = saved;

        dialog.BrowseCommand.Execute(null);
        Assert.Equal(copy, dialog.AssemblerPath);
        Assert.StartsWith($"{Strings.SettingsOriginConfigured}: {copy} (NASM ", dialog.AssemblerSummary, StringComparison.Ordinal);
        Assert.True(dialog.Save());

        Assert.True(closed);
        Assert.Equal(copy, new JsonSettingsService(SettingsPath).Current.AssemblerPath);
    }

    [Fact]
    public void AC_053_AC_054_the_dialog_saves_editor_and_execution_preferences()
    {
        var settings = new JsonSettingsService(SettingsPath);
        int changes = 0;
        settings.Changed += (_, _) => changes++;
        SettingsViewModel dialog = Dialog(settings);

        dialog.FontFamily = "Consolas";
        dialog.FontSize = "16";
        dialog.TabWidth = "8";
        dialog.UseSpaces = false;
        dialog.Theme = ThemePreference.Dark;
        dialog.InstructionBudget = 1000.ToString("N0", System.Globalization.CultureInfo.CurrentCulture); // "1,000"
        dialog.NumberFormat = NumberFormat.SignedDecimal;
        dialog.OkCommand.Execute(null);

        Assert.Equal(1, changes);
        Assert.Equal(new AppSettings(null, "Consolas", 16, 8, false, ThemePreference.Dark, 1000, NumberFormat.SignedDecimal),
            new JsonSettingsService(SettingsPath).Current);
    }

    [Theory]
    [InlineData("FontFamily", " ")]
    [InlineData("FontSize", "500")]
    [InlineData("FontSize", "big")]
    [InlineData("TabWidth", "0")]
    [InlineData("TabWidth", "2.5")]
    [InlineData("InstructionBudget", "10")]
    [InlineData("InstructionBudget", "-1")]
    public void Invalid_values_are_explained_and_nothing_is_saved(string field, string value)
    {
        var settings = new JsonSettingsService(SettingsPath);
        SettingsViewModel dialog = Dialog(settings);
        typeof(SettingsViewModel).GetProperty(field)!.SetValue(dialog, value);

        Assert.False(dialog.Save());

        Assert.True(dialog.HasValueError);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void Cancel_discards_the_changes()
    {
        var settings = new JsonSettingsService(SettingsPath);
        SettingsViewModel dialog = Dialog(settings);
        bool? closed = null;
        dialog.CloseRequested += (_, saved) => closed = saved;
        dialog.FontSize = "30";

        dialog.CancelCommand.Execute(null);

        Assert.False(closed);
        Assert.Equal(AppSettings.Default, settings.Current);
    }

    [Fact]
    public void AC_053_reset_all_returns_every_setting_to_its_default_after_confirmation()
    {
        var settings = new JsonSettingsService(SettingsPath);
        settings.Save(new AppSettings(BundledNasm, "Consolas", 20, 2, false, ThemePreference.Light, 5000, NumberFormat.Binary));
        var dialogs = new WorkspaceViewModelTests.FakeDialogs();
        SettingsViewModel dialog = Dialog(settings, dialogs);

        dialogs.Confirms.Enqueue(false);
        dialog.ResetAllCommand.Execute(null);
        Assert.Equal(BundledNasm, settings.Current.AssemblerPath); // declined: unchanged

        dialogs.Confirms.Enqueue(true);
        dialog.ResetAllCommand.Execute(null);

        Assert.Equal(AppSettings.Default, settings.Current);
        Assert.Equal(AppSettings.Default, new JsonSettingsService(SettingsPath).Current);
        Assert.Equal(("", AppSettings.Default.FontFamily), (dialog.AssemblerPath, dialog.FontFamily));
        Assert.StartsWith(Strings.SettingsOriginBundled, dialog.AssemblerSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void Reset_layout_in_the_dialog_runs_the_shells_reset_layout()
    {
        int resets = 0;
        SettingsViewModel dialog = Dialog(new JsonSettingsService(SettingsPath), resetLayout: () => resets++);

        dialog.ResetLayoutCommand.Execute(null);

        Assert.Equal(1, resets);
    }
}
