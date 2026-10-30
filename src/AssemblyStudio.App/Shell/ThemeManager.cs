using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace AssemblyStudio.App.Shell;

/// <summary>A color theme: one of the token dictionaries in <c>Themes/</c> (design tokens v1.0).</summary>
public enum AppTheme
{
    /// <summary><c>Themes/Dark.xaml</c>.</summary>
    Dark,

    /// <summary><c>Themes/Light.xaml</c>.</summary>
    Light,

    /// <summary><c>Themes/HighContrast.xaml</c>: Windows system colors.</summary>
    HighContrast,
}

/// <summary>The user's theme choice (the Settings override, TASK-067).</summary>
public enum ThemePreference
{
    /// <summary>Follow the Windows app mode (dark or light).</summary>
    System,

    /// <summary>Always dark.</summary>
    Dark,

    /// <summary>Always light.</summary>
    Light,
}

/// <summary>
/// Chooses and applies the color theme (UI-001, FR-094, design tokens §3): Windows high contrast always wins;
/// otherwise the user's preference, where "System" follows the Windows app mode. The theme dictionary in the
/// application resources is replaced, and every token reference is a DynamicResource, so open windows update at
/// once. <see cref="Watch"/> re-applies the theme when Windows switches high contrast or the app mode.
/// </summary>
public sealed class ThemeManager : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private readonly ResourceDictionary _resources;
    private ThemePreference _preference;
    private bool _watching;

    /// <summary>Creates a manager for <paramref name="resources"/> (the application resources).</summary>
    public ThemeManager(ResourceDictionary resources, ThemePreference preference = ThemePreference.System)
    {
        ArgumentNullException.ThrowIfNull(resources);
        _resources = resources;
        _preference = preference;
    }

    /// <summary>The theme currently applied.</summary>
    public AppTheme Current { get; private set; } = AppTheme.Dark;

    /// <summary>The user's choice; setting it re-applies the theme.</summary>
    public ThemePreference Preference
    {
        get => _preference;
        set
        {
            _preference = value;
            Apply();
        }
    }

    /// <summary>The theme for a preference and the Windows state; high contrast wins.</summary>
    public static AppTheme Resolve(ThemePreference preference, bool highContrast, bool appsUseLightTheme) =>
        highContrast ? AppTheme.HighContrast : preference switch
        {
            ThemePreference.Dark => AppTheme.Dark,
            ThemePreference.Light => AppTheme.Light,
            _ => appsUseLightTheme ? AppTheme.Light : AppTheme.Dark,
        };

    /// <summary>The Windows app mode ("Choose your app mode"); dark when the setting is missing.</summary>
    public static bool ReadAppsUseLightTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int value && value != 0;
    }

    /// <summary>The pack URI of a theme dictionary.</summary>
    public static Uri DictionaryUri(AppTheme theme) => new($"pack://application:,,,/AssemblyStudio.App;component/Themes/{theme}.xaml", UriKind.Absolute);

    /// <summary>Applies the theme for the current preference and Windows state.</summary>
    public void Apply() => Apply(Resolve(_preference, SystemParameters.HighContrast, ReadAppsUseLightTheme()));

    /// <summary>Puts <paramref name="theme"/>'s dictionary in place of the current theme dictionary.</summary>
    public void Apply(AppTheme theme)
    {
        var dictionary = new ResourceDictionary { Source = DictionaryUri(theme) };
        Collection<ResourceDictionary> merged = _resources.MergedDictionaries;
        int index = -1;
        for (int i = 0; i < merged.Count; i++)
        {
            if (IsThemeDictionary(merged[i]))
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
        {
            merged[index] = dictionary;
        }
        else
        {
            merged.Insert(Math.Min(1, merged.Count), dictionary); // after Shared.xaml, before the styles
        }

        Current = theme;
    }

    /// <summary>Re-applies the theme when Windows high contrast or the app mode changes.</summary>
    public void Watch()
    {
        if (_watching)
        {
            return;
        }

        _watching = true;
        SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_watching)
        {
            SystemParameters.StaticPropertyChanged -= OnSystemParameterChanged;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _watching = false;
        }
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary) =>
        dictionary.Source is { } source && Enum.GetNames<AppTheme>().Any(name =>
            source.OriginalString.EndsWith($"Themes/{name}.xaml", StringComparison.OrdinalIgnoreCase));

    private void OnSystemParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast))
        {
            ApplyOnUiThread();
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.Accessibility)
        {
            ApplyOnUiThread();
        }
    }

    private void ApplyOnUiThread()
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(() => Apply());
        }
        else
        {
            Apply();
        }
    }
}
