using System.Windows;
using Microsoft.Win32;

namespace AssemblyStudio.App.Shell;

/// <summary>
/// Windows "Make text bigger" (Settings ▸ Accessibility ▸ Text size). WPF doesn't apply it, so the
/// shell scales the font-size tokens itself (design tokens §4, NFR-026). The factor is read once at
/// startup.
/// </summary>
public static class TextScale
{
    internal static readonly string[] FontSizeKeys = ["FontSize.UI", "FontSize.UI.Small", "FontSize.Title", "FontSize.Code"];

    /// <summary>The scale factor (1.0–2.25) for a raw registry value: a percentage from 100 to 225.
    /// Missing or out-of-range values mean no scaling.</summary>
    public static double FactorFrom(object? registryValue) =>
        registryValue is int percent && percent is >= 100 and <= 225 ? percent / 100.0 : 1.0;

    /// <summary>Reads the current user's text scale factor.</summary>
    public static double ReadFactor()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Accessibility");
        return FactorFrom(key?.GetValue("TextScaleFactor"));
    }

    /// <summary>Overrides the font-size tokens in <paramref name="resources"/> with scaled values.</summary>
    public static void Apply(ResourceDictionary resources, double factor)
    {
        ArgumentNullException.ThrowIfNull(resources);
        foreach (string key in FontSizeKeys)
        {
            if (resources[key] is double size)
            {
                resources[key] = size * factor;
            }
        }
    }
}
