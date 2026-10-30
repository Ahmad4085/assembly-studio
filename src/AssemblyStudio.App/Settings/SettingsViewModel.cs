using System.Globalization;
using System.Text;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Settings;

/// <summary>
/// The Settings dialog (UI-017). Edits a copy of the settings; OK validates and saves them, Cancel discards. The
/// assembler path is checked by running it (FR-075, AC-015: exists, executes, reports a version) and an unusable path
/// is not saved. The current assembler's path, origin and version are shown when the dialog opens.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly Func<string?, AssemblerInfo> _checkAssembler;
    private readonly Action _resetLayout;

    /// <summary>Creates the dialog state from the current settings.</summary>
    /// <param name="settings">The settings service.</param>
    /// <param name="dialogs">Browse and confirmation dialogs.</param>
    /// <param name="checkAssembler">Checks an assembler: a configured path, or <see langword="null"/> for the bundled one.</param>
    /// <param name="resetLayout">View ▸ Reset Layout.</param>
    /// <param name="fontFamilies">Font families offered (the field also accepts any name).</param>
    public SettingsViewModel(
        ISettingsService settings, IDialogService dialogs, Func<string?, AssemblerInfo> checkAssembler, Action resetLayout, IReadOnlyList<string> fontFamilies)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(checkAssembler);
        ArgumentNullException.ThrowIfNull(resetLayout);
        ArgumentNullException.ThrowIfNull(fontFamilies);
        _settings = settings;
        _dialogs = dialogs;
        _checkAssembler = checkAssembler;
        _resetLayout = resetLayout;
        FontFamilies = fontFamilies;
        Load(settings.Current);
        AssemblerSummary = Describe(checkAssembler(settings.Current.AssemblerPath));
    }

    /// <summary>Raised when the dialog should close; the argument is <see langword="true"/> after a save.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>Font families offered in the font field.</summary>
    public IReadOnlyList<string> FontFamilies { get; }

    /// <summary>Theme choices.</summary>
    public IReadOnlyList<ThemePreference> Themes { get; } = Enum.GetValues<ThemePreference>();

    /// <summary>Number format choices.</summary>
    public IReadOnlyList<NumberFormat> NumberFormats { get; } = Enum.GetValues<NumberFormat>();

    /// <summary>The configured nasm.exe; empty to use the bundled assembler.</summary>
    [ObservableProperty]
    public partial string AssemblerPath { get; set; } = "";

    /// <summary>The assembler in use: path, origin and version, or why it can't be used (AC-015).</summary>
    [ObservableProperty]
    public partial string AssemblerSummary { get; private set; } = "";

    /// <summary>Why the entered assembler path can't be used; <see langword="null"/> when it is fine.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAssemblerError))]
    public partial string? AssemblerError { get; private set; }

    /// <summary>Whether <see cref="AssemblerError"/> is shown.</summary>
    public bool HasAssemblerError => AssemblerError is not null;

    /// <summary>Editor font family.</summary>
    [ObservableProperty]
    public partial string FontFamily { get; set; } = "";

    /// <summary>Editor font size, as typed.</summary>
    [ObservableProperty]
    public partial string FontSize { get; set; } = "";

    /// <summary>Tab width, as typed.</summary>
    [ObservableProperty]
    public partial string TabWidth { get; set; } = "";

    /// <summary>Indent with spaces.</summary>
    [ObservableProperty]
    public partial bool UseSpaces { get; set; }

    /// <summary>Theme.</summary>
    [ObservableProperty]
    public partial ThemePreference Theme { get; set; }

    /// <summary>Instruction budget, as typed.</summary>
    [ObservableProperty]
    public partial string InstructionBudget { get; set; } = "";

    /// <summary>Default number format.</summary>
    [ObservableProperty]
    public partial NumberFormat NumberFormat { get; set; }

    /// <summary>Why the editor or execution values can't be saved; <see langword="null"/> when they are fine.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValueError))]
    public partial string? ValueError { get; private set; }

    /// <summary>Whether <see cref="ValueError"/> is shown.</summary>
    public bool HasValueError => ValueError is not null;

    /// <summary>Validates and saves; on success asks the view to close. Returns whether it saved.</summary>
    public bool Save()
    {
        AppSettings? values = ReadValues();
        string? path = string.IsNullOrWhiteSpace(AssemblerPath) ? null : AssemblerPath.Trim();
        AssemblerError = path is null ? null : AssemblerProblem(path, _checkAssembler(path));
        if (values is null || AssemblerError is not null)
        {
            return false;
        }

        _settings.Save(values with { AssemblerPath = path });
        CloseRequested?.Invoke(this, true);
        return true;
    }

    [RelayCommand]
    private void Ok() => Save();

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    /// <summary>Browse for nasm.exe.</summary>
    [RelayCommand]
    private void Browse()
    {
        if (_dialogs.ShowOpenExecutable(string.IsNullOrWhiteSpace(AssemblerPath) ? null : AssemblerPath) is { } chosen)
        {
            AssemblerPath = chosen;
            CheckPath();
        }
    }

    /// <summary>Checks the entered path now and shows what was found (or uses the bundled one when empty).</summary>
    [RelayCommand]
    private void CheckPath()
    {
        string? path = string.IsNullOrWhiteSpace(AssemblerPath) ? null : AssemblerPath.Trim();
        AssemblerInfo info = _checkAssembler(path);
        AssemblerError = path is null ? null : AssemblerProblem(path, info);
        AssemblerSummary = Describe(info);
    }

    /// <summary>FR-082: after confirmation, saves the defaults (the dialog stays open showing them).</summary>
    [RelayCommand]
    private void ResetAll()
    {
        if (!_dialogs.Confirm(Strings.SettingsResetTitle, Strings.SettingsResetText, Strings.SettingsResetConfirm, Strings.DialogCancel))
        {
            return;
        }

        _settings.Save(AppSettings.Default);
        Load(AppSettings.Default);
        AssemblerError = null;
        ValueError = null;
        AssemblerSummary = Describe(_checkAssembler(null));
    }

    [RelayCommand]
    private void ResetLayout() => _resetLayout();

    private void Load(AppSettings settings)
    {
        AssemblerPath = settings.AssemblerPath ?? "";
        FontFamily = settings.FontFamily;
        FontSize = settings.FontSize.ToString(CultureInfo.CurrentCulture);
        TabWidth = settings.TabWidth.ToString(CultureInfo.CurrentCulture);
        UseSpaces = settings.UseSpaces;
        Theme = settings.Theme;
        InstructionBudget = settings.InstructionBudget.ToString("N0", CultureInfo.CurrentCulture);
        NumberFormat = settings.NumberFormat;
    }

    /// <summary>The typed values as settings, or <see langword="null"/> with <see cref="ValueError"/> set.</summary>
    private AppSettings? ReadValues()
    {
        ValueError = null;
        if (string.IsNullOrWhiteSpace(FontFamily))
        {
            ValueError = Strings.SettingsErrFont;
        }
        else if (!double.TryParse(FontSize, NumberStyles.Float, CultureInfo.CurrentCulture, out double size)
                 || size is < AppSettings.MinFontSize or > AppSettings.MaxFontSize)
        {
            ValueError = Format(Strings.SettingsErrFontSize, AppSettings.MinFontSize, AppSettings.MaxFontSize);
        }
        else if (!int.TryParse(TabWidth, NumberStyles.Integer, CultureInfo.CurrentCulture, out int tab)
                 || tab is < AppSettings.MinTabWidth or > AppSettings.MaxTabWidth)
        {
            ValueError = Format(Strings.SettingsErrTabWidth, AppSettings.MinTabWidth, AppSettings.MaxTabWidth);
        }
        else if (!ulong.TryParse(InstructionBudget, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out ulong budget)
                 || budget is < AppSettings.MinInstructionBudget or > AppSettings.MaxInstructionBudget)
        {
            ValueError = Format(Strings.SettingsErrBudget, AppSettings.MinInstructionBudget, AppSettings.MaxInstructionBudget);
        }
        else
        {
            return new AppSettings(null, FontFamily.Trim(), size, tab, UseSpaces, Theme, budget, NumberFormat);
        }

        return null;
    }

    /// <summary>Why <paramref name="path"/> can't be used: the path check's own reason (without the build-time advice
    /// to open Settings), else the version check's problem.</summary>
    private static string? AssemblerProblem(string path, AssemblerInfo info) =>
        Toolchain.Nasm.NasmAssembler.ValidateConfiguredPath(path)
        ?? (info.IsUsable ? null : info.Problem ?? Strings.SettingsErrAssembler);

    /// <summary>"Bundled: C:\…\nasm.exe — NASM 3.02", or the problem.</summary>
    internal static string Describe(AssemblerInfo info)
    {
        string origin = info.Origin == AssemblerOrigin.Configured ? Strings.SettingsOriginConfigured : Strings.SettingsOriginBundled;
        return info.IsUsable
            ? Format(Strings.SettingsAssemblerFound, origin, info.Path, info.Version ?? Strings.SettingsVersionUnknown)
                + (info.Problem is null ? "" : Environment.NewLine + info.Problem)
            : Format(Strings.SettingsAssemblerUnusable, origin, info.Problem ?? Strings.SettingsErrAssembler);
    }

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(format), args);
}
