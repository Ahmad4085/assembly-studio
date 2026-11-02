using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Editor.Search;

/// <summary>
/// The Go to Line dialog (UI-004, FR-017): asks for a line from 1 to the last line, starting with the caret's line selected.
/// An entry that isn't such a number shows why and keeps the dialog open.
/// </summary>
public partial class GoToLineDialog : Window
{
    private readonly int _lineCount;

    internal GoToLineDialog(int lineCount, int currentLine)
    {
        InitializeComponent();
        _lineCount = lineCount;
        string prompt = Format(Strings.GoToLinePrompt, lineCount);
        Prompt.Content = prompt;
        AutomationProperties.SetName(LineBox, prompt.Replace("_", string.Empty, StringComparison.Ordinal));
        LineBox.Text = currentLine.ToString(CultureInfo.InvariantCulture);
        Loaded += (_, _) =>
        {
            LineBox.Focus();
            LineBox.SelectAll();
        };
    }

    /// <summary>The chosen line, after OK.</summary>
    internal int? Line { get; private set; }

    /// <summary>The entry field (tests).</summary>
    internal string Entry
    {
        get => LineBox.Text;
        set => LineBox.Text = value;
    }

    /// <summary>The error shown, or empty.</summary>
    internal string ErrorText => Error.Visibility == Visibility.Visible ? Error.Text : string.Empty;

    /// <summary>Shows the dialog over <paramref name="owner"/>: the line, or <see langword="null"/> when cancelled.</summary>
    public static int? Ask(Window? owner, int lineCount, int currentLine)
    {
        var dialog = new GoToLineDialog(lineCount, currentLine);
        if (owner is { IsLoaded: true })
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog() == true ? dialog.Line : null;
    }

    /// <summary>OK: accepts a valid line, otherwise says why (true when accepted).</summary>
    internal bool TryAccept()
    {
        if (!FindReplaceViewModel.TryParseLine(LineBox.Text, _lineCount, out int line))
        {
            Error.Text = Format(Strings.GoToLineInvalid, _lineCount);
            Error.Visibility = Visibility.Visible;
            LineBox.Focus();
            LineBox.SelectAll();
            return false;
        }

        Line = line;
        return true;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (TryAccept())
        {
            DialogResult = true;
        }
    }

    private static string Format(string format, int value) =>
        string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(format), value);
}
