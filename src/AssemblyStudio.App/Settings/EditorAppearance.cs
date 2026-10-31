using System.Windows;
using System.Windows.Media;

namespace AssemblyStudio.App.Settings;

/// <summary>
/// Applies the editor settings (FR-077, FR-014) and the zoom (FR-022) to resource tokens: <c>Font.Editor</c> and
/// <c>FontSize.Editor</c> (scaled by the Windows text size, NFR-026, and the zoom), <c>Editor.TabWidth</c> and
/// <c>Editor.UseSpaces</c> (read by <see cref="Editor.CodeEditor"/>), and <c>FontSize.Code</c> for the debug panels.
/// </summary>
public static class EditorAppearance
{
    /// <summary>Sets the editor tokens in <paramref name="resources"/>.</summary>
    /// <param name="resources">The application resources.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="textScale">The Windows text scale factor.</param>
    /// <param name="zoom">The zoom factor (1.0 = 100 %).</param>
    public static void Apply(ResourceDictionary resources, AppSettings settings, double textScale, double zoom = 1.0)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(settings);
        resources["Font.Editor"] = new FontFamily(settings.FontFamily);
        resources["FontSize.Editor"] = settings.FontSize * textScale * zoom;
        resources["Editor.TabWidth"] = settings.TabWidth;
        resources["Editor.UseSpaces"] = settings.UseSpaces;
    }

    /// <summary>Scales the debug panels' code font: <c>FontSize.Code</c> = <paramref name="codeSize"/> × <paramref name="zoom"/>.</summary>
    /// <param name="resources">The application resources.</param>
    /// <param name="codeSize">The code font size at 100 % (after Windows text scaling).</param>
    /// <param name="zoom">The zoom factor.</param>
    public static void ApplyCodeZoom(ResourceDictionary resources, double codeSize, double zoom)
    {
        ArgumentNullException.ThrowIfNull(resources);
        resources["FontSize.Code"] = codeSize * zoom;
    }
}
