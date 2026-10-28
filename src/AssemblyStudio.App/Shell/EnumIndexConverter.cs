using System.Globalization;
using System.Windows.Data;

namespace AssemblyStudio.App.Shell;

/// <summary>Converts between an enum value and a selector index (the enum's declaration order),
/// e.g. <see cref="BottomTab"/> and the bottom tab group's <c>SelectedIndex</c>.</summary>
public sealed class EnumIndexConverter : IValueConverter
{
    /// <summary>Shared instance for XAML (<c>{x:Static}</c>).</summary>
    public static EnumIndexConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Enum e ? System.Convert.ToInt32(e, CultureInfo.InvariantCulture) : -1;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int index && index >= 0 && Enum.IsDefined(targetType, index)
            ? Enum.ToObject(targetType, index)
            : Binding.DoNothing;
}
