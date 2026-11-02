using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using AssemblyStudio.App.Editor;
using Xunit;

namespace AssemblyStudio.App.Tests.Accessibility;

/// <summary>
/// TASK-069 (FR-093, NFR-022, ADR-0006): what the code editor exposes to screen readers through UI Automation, the interface
/// Narrator reads. The focused element (AvalonEdit's TextArea) is a named Document with the Text pattern (line, word,
/// selection at the caret), and a client outside the process receives the selection-changed event Narrator uses to follow
/// the caret. What Narrator actually says is checked by hand (docs/06-implementation/accessibility-findings.md).
/// </summary>
[Collection(nameof(AppProcessTests))]
public sealed class EditorAccessibilityTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-a11y-").FullName;
    private Process? _process;

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }

        _process?.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void The_focused_part_of_the_editor_is_a_named_document_with_the_text_at_the_caret()
    {
        StaTestThread.Run(() =>
        {
            var editor = new CodeEditor { BoundText = "org 100h\nmov ax, 1\nint 20h\n" };
            AutomationProperties.SetName(editor, "hello.asm");
            var window = new Window { Content = editor, Width = 500, Height = 300 };
            try
            {
                window.Show();
                window.UpdateLayout();
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(editor.TextArea);

                Assert.Equal(AutomationControlType.Document, peer.GetAutomationControlType());
                Assert.Equal("hello.asm", peer.GetName()); // was empty before TASK-069
                Assert.True(peer.IsKeyboardFocusable());
                Assert.False(string.IsNullOrEmpty(AutomationProperties.GetHelpText(editor.TextArea)));
                var text = (ITextProvider)peer.GetPattern(PatternInterface.Text);
                Assert.NotNull(text);

                editor.CaretOffset = editor.Document.GetLineByNumber(2).Offset + 4; // on "ax"
                ITextRangeProvider caret = text.GetSelection().Single();
                ITextRangeProvider line = caret.Clone();
                line.ExpandToEnclosingUnit(TextUnit.Line);
                ITextRangeProvider word = caret.Clone();
                word.ExpandToEnclosingUnit(TextUnit.Word);
                Assert.Equal("mov ax, 1", line.GetText(-1).TrimEnd('\r', '\n'));
                Assert.Equal("ax", word.GetText(-1));

                editor.Select(editor.Document.GetLineByNumber(2).Offset, 3);
                Assert.Equal("mov", text.GetSelection().Single().GetText(-1));
                Assert.Equal(SupportedTextSelection.Single, text.SupportedTextSelection);

                AutomationProperties.SetName(editor, "renamed.asm"); // follows the document's name
                Assert.Equal("renamed.asm", UIElementAutomationPeer.CreatePeerForElement(editor.TextArea).GetName());
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void A_screen_reader_outside_the_app_finds_the_editor_by_name_and_hears_selection_changes()
    {
        string file = Path.Combine(_folder, "probe.asm");
        File.WriteAllText(file, "org 100h\r\nmov ax, 1\r\nint 20h\r\n");
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", Path.Combine(_folder, "recovery"), "--log", Path.Combine(_folder, "logs"), file })
        {
            start.ArgumentList.Add(argument);
        }

        _process = Process.Start(start)!;
        AutomationElement textArea = Poll(() =>
            AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _process.Id))?
                .FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                    new PropertyCondition(AutomationElement.NameProperty, "probe.asm"),
                    new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true))));

        var text = (TextPattern)textArea.GetCurrentPattern(TextPattern.Pattern);
        Assert.Contains("mov ax, 1", text.DocumentRange.GetText(-1), StringComparison.Ordinal);

        using var changed = new ManualResetEventSlim();
        AutomationEventHandler handler = (_, _) => changed.Set();
        Automation.AddAutomationEventHandler(TextPattern.TextSelectionChangedEvent, textArea, TreeScope.Element, handler);
        try
        {
            TextPatternRange target = text.DocumentRange.FindText("mov", backward: false, ignoreCase: false);
            target.Select(); // as a screen reader's selection command would

            Assert.True(changed.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken), "No TextSelectionChanged event.");
            Assert.Equal("mov", text.GetSelection().Single().GetText(-1));
        }
        finally
        {
            Automation.RemoveAutomationEventHandler(TextPattern.TextSelectionChangedEvent, textArea, handler);
        }
    }

    private static AutomationElement Poll(Func<AutomationElement?> find)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (find() is { } found)
            {
                return found;
            }

            Assert.True(stopwatch.Elapsed < Timeout, "Timed out waiting for the editor.");
            Thread.Sleep(100);
        }
    }
}
