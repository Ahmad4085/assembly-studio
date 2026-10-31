using System.Windows;
using ICSharpCode.AvalonEdit;
using Xunit;

namespace AssemblyStudio.App.Tests.Editor;

/// <summary>TASK-064 step 1 (ADR-0006 Unverified → Verified): the net8.0-windows build of AvalonEdit 6.3.1.120 runs on
/// net10.0-windows: it loads, renders, edits and undoes.</summary>
public sealed class AvalonEditPlatformTests
{
    [Fact]
    public void AvalonEdit_6_3_1_runs_on_net10()
    {
        StaTestThread.Run(() =>
        {
            var editor = new TextEditor { Text = "mov ax, 1\n", ShowLineNumbers = true };
            var window = new Window { Content = editor, Width = 400, Height = 200 };
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.TextArea.Caret.Offset = editor.Text.Length;
                editor.TextArea.PerformTextInput("int 21h");
                window.UpdateLayout();

                Assert.Equal("mov ax, 1\nint 21h", editor.Text);
                Assert.True(editor.TextArea.TextView.VisualLinesValid);
                Assert.True(editor.Undo());
                Assert.Equal("mov ax, 1\n", editor.Text);
                Assert.Equal(new Version(6, 3, 1, 120), typeof(TextEditor).Assembly.GetName().Version);
                Assert.StartsWith(".NET 10", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
