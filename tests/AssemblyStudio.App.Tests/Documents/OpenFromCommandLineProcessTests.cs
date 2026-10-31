using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using Xunit;

namespace AssemblyStudio.App.Tests.Documents;

/// <summary>FR-084 (TASK-101): the .asm file association starts the application with the file's path; the real
/// application opens it in the editor instead of an empty document.</summary>
[Collection(nameof(AppProcessTests))]
public sealed class OpenFromCommandLineProcessTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly string _folder = Directory.CreateTempSubdirectory("asmstudio-open-arg-").FullName;
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
    public void A_file_passed_on_the_command_line_opens_in_the_editor()
    {
        string file = Path.Combine(_folder, "my program.asm");
        File.WriteAllText(file, "; opened from the command line\r\norg 100h\r\n");
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", Path.Combine(_folder, "recovery"), "--log", Path.Combine(_folder, "logs"), file })
        {
            start.ArgumentList.Add(argument);
        }

        _process = Process.Start(start)!;
        AutomationElement editor = Poll(() =>
            AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _process.Id))?
                .FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                    new PropertyCondition(AutomationElement.NameProperty, "my program.asm"))));

        Assert.StartsWith("; opened from the command line", ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value, StringComparison.Ordinal);
        Assert.Null(AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _process.Id))
            .FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Untitled"))); // no extra empty document
    }

    [Fact]
    public void FR_001_without_a_file_the_app_starts_with_an_editable_untitled_document()
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "AssemblyStudio.App.exe")) { UseShellExecute = false };
        foreach (string argument in new[] { "--settings", Path.Combine(_folder, "settings.json"), "--recovery", Path.Combine(_folder, "recovery"), "--log", Path.Combine(_folder, "logs") })
        {
            start.ArgumentList.Add(argument);
        }

        _process = Process.Start(start)!;
        AutomationElement editor = Poll(() =>
            AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _process.Id))?
                .FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                    new PropertyCondition(AutomationElement.NameProperty, "Untitled"))));
        var value = (ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern);

        Assert.False(value.Current.IsReadOnly);
        value.SetValue("mov ax, 1");
        Assert.Equal("mov ax, 1", value.Current.Value);
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

            Assert.True(stopwatch.Elapsed < Timeout, "Timed out waiting for the opened file.");
            Thread.Sleep(100);
        }
    }
}
