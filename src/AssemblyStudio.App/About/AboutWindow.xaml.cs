using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssemblyStudio.App.About;

/// <summary>The About / Diagnostics window view (UI-018); the content is in <see cref="AboutViewModel"/>.</summary>
public partial class AboutWindow : Window
{
    /// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
    public AboutWindow(AboutViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

/// <summary>Maps <see cref="AboutPage"/> to the tab index and back.</summary>
public sealed class AboutPageConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is AboutPage page ? (int)page : 0;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int index && Enum.IsDefined((AboutPage)index) ? (AboutPage)index : AboutPage.Diagnostics;
}
