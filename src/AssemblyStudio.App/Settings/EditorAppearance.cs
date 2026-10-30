using System.Windows;
using System.Windows.Media;

namespace AssemblyStudio.App.Settings;

/// <summary>
/// Applies the editor font settings (FR-077) to the <c>Font.Editor</c> and <c>FontSize.Editor</c> tokens, scaled by the
/// Windows text size like the other font tokens (NFR-026). Tab width and tabs/spaces are applied by the editor control
/// (AvalonEdit, TASK-064); the interim TextBox editor has no such options.
/// </summary>
public static class EditorAppearance
{
    /// <summary>Sets the editor font tokens in <paramref name="resources"/>.</summary>
    public static void Apply(ResourceDictionary resources, AppSettings settings, double textScale)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(settings);
        resources["Font.Editor"] = new FontFamily(settings.FontFamily);
        resources["FontSize.Editor"] = settings.FontSize * textScale;
    }
}
