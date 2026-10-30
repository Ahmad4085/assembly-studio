using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Settings;

/// <summary>The Settings dialog view (UI-017); the logic is in <see cref="SettingsViewModel"/>.</summary>
public partial class SettingsWindow : Window
{
    /// <summary>Creates the dialog for <paramref name="viewModel"/>.</summary>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, saved) =>
        {
            if (_modal)
            {
                DialogResult = saved;
            }
            else
            {
                Close();
            }
        };
        Loaded += (_, _) => AssemblerPathBox.Focus();
    }

    private bool _modal;

    /// <summary>Shows the dialog modally over <paramref name="owner"/>; returns whether settings were saved.</summary>
    public bool ShowModal(Window? owner)
    {
        Owner = owner;
        _modal = true;
        return ShowDialog() == true;
    }
}

/// <summary>Display names of the theme and number format choices.</summary>
public sealed class SettingsLabels : IValueConverter
{
    /// <summary>The shared instance.</summary>
    public static SettingsLabels Instance { get; } = new();

    /// <summary>The display name of a choice.</summary>
    public static string Label(object? value) => value switch
    {
        ThemePreference.System => Strings.SettingsThemeSystem,
        ThemePreference.Dark => Strings.SettingsThemeDark,
        ThemePreference.Light => Strings.SettingsThemeLight,
        NumberFormat.Hexadecimal => Strings.SettingsFormatHex,
        NumberFormat.UnsignedDecimal => Strings.SettingsFormatUnsigned,
        NumberFormat.SignedDecimal => Strings.SettingsFormatSigned,
        NumberFormat.Binary => Strings.SettingsFormatBinary,
        _ => value?.ToString() ?? "",
    };

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Label(value);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
