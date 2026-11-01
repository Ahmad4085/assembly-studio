using System.Globalization;
using System.Text;
using AssemblyStudio.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssemblyStudio.App.Panels.Console;

/// <summary>
/// The program console panel (UI-010; FR-063, FR-064, FR-096, D-015): the program's output on a <see cref="ConsoleScreen"/>,
/// the waiting-for-input indicator, the end-of-program line and the trimmed-output notice. Keys typed into the focused
/// console go to the program while it runs or waits for input; Ctrl+C there is the program's Ctrl-C.
/// </summary>
public sealed partial class ConsoleViewModel : ObservableObject
{
    /// <summary>Creates an empty console.</summary>
    /// <param name="capacity">The most characters kept (NFR-015).</param>
    public ConsoleViewModel(int capacity = ConsoleScreen.DefaultCapacity) => Screen = new ConsoleScreen(capacity);

    /// <summary>The console text.</summary>
    public ConsoleScreen Screen { get; }

    /// <summary>The whole text (lines separated by <c>\n</c>).</summary>
    public string Text => Screen.Text;

    /// <summary>Raised after output was applied; the view updates the changed lines.</summary>
    public event EventHandler<ConsoleChange>? Changed;

    /// <summary>Raised after the console was emptied.</summary>
    public event EventHandler? Cleared;

    /// <summary>Delivers input bytes to the program (set by the session owner).</summary>
    public Action<IReadOnlyList<byte>>? InputSink { get; set; }

    /// <summary>Delivers Ctrl-C to the program (set by the session owner).</summary>
    public Action? CtrlCSink { get; set; }

    /// <summary>Nothing was written since the console was last emptied ("Program output appears here").</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; private set; } = true;

    /// <summary>The program waits for keyboard input ("⌨ Waiting for input").</summary>
    [ObservableProperty]
    public partial bool IsWaiting { get; set; }

    /// <summary>The program runs or waits: typed keys go to it.</summary>
    [ObservableProperty]
    public partial bool IsInputActive { get; set; }

    /// <summary>"— program ended (exit code 0) —" after the program terminated, or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? EndedText { get; set; }

    /// <summary>The notice shown once older output was trimmed, or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? TrimmedNotice { get; private set; }

    /// <summary>Applies program output.</summary>
    /// <param name="text">The output since the last call.</param>
    /// <param name="droppedBefore">Characters dropped before <paramref name="text"/> because they exceeded the cap.</param>
    public void Write(string text, long droppedBefore = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (droppedBefore > 0)
        {
            Screen.Clear(); // everything shown is older than what was dropped
            Cleared?.Invoke(this, EventArgs.Empty);
            Screen.NoteTrimmed(droppedBefore);
        }

        if (text.Length == 0 && droppedBefore == 0)
        {
            return;
        }

        ConsoleChange change = Screen.Write(text);
        IsEmpty = Screen.IsEmpty;
        TrimmedNotice = Screen.HasTrimmed
            ? string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.ConsoleTrimmed), Screen.Capacity)
            : null;
        Changed?.Invoke(this, change);
    }

    /// <summary>Empties the console (Run, Reset).</summary>
    public void Clear()
    {
        Screen.Clear();
        IsEmpty = true;
        TrimmedNotice = null;
        Cleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Sends typed bytes to the program; returns whether the console takes input now.</summary>
    public bool SendInput(IReadOnlyList<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (!IsInputActive || bytes.Count == 0 || InputSink is not { } sink)
        {
            return false;
        }

        sink(bytes);
        return true;
    }

    /// <summary>Ctrl+C in the focused console of an active program (D-015); returns whether it was delivered.</summary>
    public bool SendCtrlC()
    {
        if (!IsInputActive || CtrlCSink is not { } sink)
        {
            return false;
        }

        sink();
        return true;
    }
}
