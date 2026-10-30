using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace AssemblyStudio.App.Shell;

/// <summary>
/// The application's message dialog (UI-019): a heading, the message (with the path and reason), an optional next
/// step and labelled buttons, themed with the design tokens. Returns the index of the chosen button; Esc and the
/// close box choose the cancel button.
/// </summary>
public partial class MessageDialog : Window
{
    private int _result;

    private MessageDialog(string heading, string message, string? nextStep, IReadOnlyList<string> buttons, int defaultButton, int cancelButton)
    {
        InitializeComponent();
        Title = heading;
        Heading.Text = heading;
        Message.Text = message;
        NextStep.Text = nextStep ?? string.Empty;
        NextStep.Visibility = nextStep is null ? Visibility.Collapsed : Visibility.Visible;
        _result = cancelButton;
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Content = buttons[i],
                MinWidth = 88,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 4, 12, 4),
                IsDefault = i == defaultButton,
                IsCancel = i == cancelButton,
            };
            AutomationProperties.SetName(button, buttons[i].Replace("_", string.Empty, StringComparison.Ordinal));
            button.Click += (_, _) =>
            {
                _result = index;
                DialogResult = true;
            };
            Buttons.Children.Add(button);
        }

        Loaded += (_, _) => ((Button)Buttons.Children[defaultButton]).Focus();
    }

    /// <summary>Shows the dialog over <paramref name="owner"/> and returns the chosen button's index.</summary>
    /// <param name="owner">The owning window (or <see langword="null"/>).</param>
    /// <param name="heading">What happened, or the question.</param>
    /// <param name="message">The details: the path and the reason.</param>
    /// <param name="nextStep">A suggested next step, or <see langword="null"/>.</param>
    /// <param name="buttons">Button labels (access keys with '_').</param>
    /// <param name="defaultButton">The button Enter chooses: the safest one.</param>
    /// <param name="cancelButton">The button Esc and the close box choose.</param>
    public static int Show(Window? owner, string heading, string message, string? nextStep, IReadOnlyList<string> buttons, int defaultButton, int cancelButton)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        var dialog = new MessageDialog(heading, message, nextStep, buttons, defaultButton, cancelButton);
        if (owner is { IsLoaded: true })
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
        return dialog._result;
    }
}
