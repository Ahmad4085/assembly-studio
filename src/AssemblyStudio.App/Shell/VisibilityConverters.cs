using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssemblyStudio.App.Shell;

/// <summary>Collapsed when the bound value is <see langword="true"/>, visible otherwise.</summary>
public sealed class InverseVisibilityConverter : IValueConverter
{
    /// <summary>Shared instance for XAML (<c>{x:Static}</c>).</summary>
    public static InverseVisibilityConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Collapsed when the bound value is <see langword="null"/>, visible otherwise.</summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    /// <summary>Shared instance for XAML (<c>{x:Static}</c>).</summary>
    public static NullToCollapsedConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
