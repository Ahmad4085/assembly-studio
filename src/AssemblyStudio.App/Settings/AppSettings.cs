using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Settings;

/// <summary>How the registers and value panels show numbers by default (FR-047, FR-078).</summary>
public enum NumberFormat
{
    /// <summary>Hexadecimal (FR-046 default).</summary>
    Hexadecimal,

    /// <summary>Unsigned decimal.</summary>
    UnsignedDecimal,

    /// <summary>Signed decimal.</summary>
    SignedDecimal,

    /// <summary>Binary.</summary>
    Binary,
}

/// <summary>
/// The user's settings (FR-075, FR-077, FR-078; UI-017). Immutable; the settings service owns the current value.
/// Defaults need no configuration (FR-079): the bundled assembler, the code font, 4-space indentation, the Windows
/// theme, a 10,000,000-instruction budget and hexadecimal values.
/// </summary>
/// <param name="AssemblerPath">The configured nasm.exe, or <see langword="null"/> to use the bundled one.</param>
/// <param name="FontFamily">Editor font family (a comma-separated fallback list is allowed).</param>
/// <param name="FontSize">Editor font size in device-independent pixels, before Windows text scaling.</param>
/// <param name="TabWidth">Columns per tab stop / indentation level.</param>
/// <param name="UseSpaces">Indent with spaces instead of tab characters.</param>
/// <param name="Theme">Dark, light, or follow Windows.</param>
/// <param name="InstructionBudget">Instructions one Run may execute before pausing (FR-042).</param>
/// <param name="NumberFormat">Default number format of the value panels.</param>
public sealed record AppSettings(
    string? AssemblerPath,
    string FontFamily,
    double FontSize,
    int TabWidth,
    bool UseSpaces,
    ThemePreference Theme,
    ulong InstructionBudget,
    NumberFormat NumberFormat)
{
    /// <summary>Smallest editor font size.</summary>
    public const double MinFontSize = 8;

    /// <summary>Largest editor font size.</summary>
    public const double MaxFontSize = 36;

    /// <summary>Smallest tab width.</summary>
    public const int MinTabWidth = 1;

    /// <summary>Largest tab width.</summary>
    public const int MaxTabWidth = 16;

    /// <summary>Smallest instruction budget.</summary>
    public const ulong MinInstructionBudget = 1_000;

    /// <summary>Largest instruction budget (10,000 times the default).</summary>
    public const ulong MaxInstructionBudget = 100_000_000_000;

    /// <summary>The defaults (FR-079, FR-082 Reset all).</summary>
    public static AppSettings Default { get; } = new(
        AssemblerPath: null,
        FontFamily: "Cascadia Mono, Consolas",
        FontSize: 14,
        TabWidth: 4,
        UseSpaces: true,
        Theme: ThemePreference.System,
        InstructionBudget: 10_000_000,
        NumberFormat: NumberFormat.Hexadecimal);

    /// <summary>Replaces values a hand-edited file may hold but the application can't use with their defaults.</summary>
    public AppSettings Sanitized() => new(
        string.IsNullOrWhiteSpace(AssemblerPath) ? null : AssemblerPath.Trim(),
        string.IsNullOrWhiteSpace(FontFamily) ? Default.FontFamily : FontFamily.Trim(),
        double.IsFinite(FontSize) && FontSize is >= MinFontSize and <= MaxFontSize ? FontSize : Default.FontSize,
        TabWidth is >= MinTabWidth and <= MaxTabWidth ? TabWidth : Default.TabWidth,
        UseSpaces,
        Enum.IsDefined(Theme) ? Theme : Default.Theme,
        InstructionBudget is >= MinInstructionBudget and <= MaxInstructionBudget ? InstructionBudget : Default.InstructionBudget,
        Enum.IsDefined(NumberFormat) ? NumberFormat : Default.NumberFormat);
}
