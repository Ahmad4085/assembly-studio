using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Commands;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Shell;
using AssemblyStudio.App.Tests.Documents;
using Xunit;

namespace AssemblyStudio.App.Tests.Commands;

/// <summary>
/// TASK-080, FR-091: every command of ui-spec §6 is reachable by keyboard (a key binding, the editor's standard gestures,
/// or the menu's access keys), shows its shortcut in its menu item and its command-bar tooltip, and the window-level
/// flow (Problems → line, state badge, status bar) works in the real main window.
/// </summary>
// These tests pump the dispatcher while they wait for snapshots, which would let other UI tests run inside them: they run
// in the non-parallel collection.
[Collection(nameof(AppProcessTests))]
public sealed class CommandTableTests
{
    private static readonly string[] SpecShortcuts =
    [
        "Ctrl+N", "Ctrl+O", "Ctrl+S", "Ctrl+Shift+S", "Ctrl+W", "Ctrl+F4", "Alt+F4", "Ctrl+Z", "Ctrl+Y", "Ctrl+X", "Ctrl+C",
        "Ctrl+V", "Ctrl+A", "Ctrl+F", "F3", "Shift+F3", "Ctrl+H", "Ctrl+G", "Ctrl+M", "Ctrl+Plus", "Ctrl+Minus", "Ctrl+0", "F6", "Shift+F6",
        "Ctrl+Alt+R", "Ctrl+Alt+F", "Ctrl+Alt+M", "Ctrl+Alt+K", "Ctrl+Alt+O", "Ctrl+Alt+P", "Ctrl+Alt+U", "Ctrl+Alt+H", "Ctrl+Alt+B",
        "Ctrl+Alt+L", "Ctrl+Alt+N", "Ctrl+Shift+B", "Esc", "F5", "Ctrl+Alt+Break", "Shift+F5", "Ctrl+Shift+F5", "F11", "F10", "F9",
        "Ctrl+F10", "Ctrl+Break", "Ctrl+Shift+C", "Ctrl+,", "F1",
    ];

    private static void WithWindow(Action<MainWindow, ShellViewModel, WorkspaceViewModel> body)
    {
        StaTestThread.Run(() =>
        {
            WorkspaceViewModel workspace = BuildRunTests.Workspace();
            var shell = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(null), "test", workspace, buildRun: BuildRunTests.Create(workspace));
            var window = new MainWindow(shell, persistLayout: false) { Width = 1400, Height = 900 };
            try
            {
                window.Show();
                window.UpdateLayout();
                body(window, shell, workspace);
            }
            finally
            {
                foreach (DocumentViewModel document in workspace.Documents)
                {
                    document.MarkSaved(document.FilePath ?? "x.asm");
                }

                window.Close();
            }
        });
    }

    private static IEnumerable<MenuItem> MenuItems(ItemsControl parent)
    {
        foreach (object item in parent.Items)
        {
            if (item is MenuItem menuItem)
            {
                yield return menuItem;
                foreach (MenuItem child in MenuItems(menuItem))
                {
                    yield return child;
                }
            }
        }
    }

    [Fact]
    public void The_table_covers_every_command_of_ui_spec_section_6()
    {
        string spec = File.ReadAllText(Path.Combine(MainWindowSmokeTests.RepoRoot(), "docs", "04-design", "ui-spec.md"));
        string section = spec[spec.IndexOf("## 6. Commands and keyboard", StringComparison.Ordinal)..spec.IndexOf("## 7.", StringComparison.Ordinal)];
        string[] shortcuts = [.. section.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal) && !l.StartsWith("| Command", StringComparison.Ordinal) && !l.StartsWith("|---", StringComparison.Ordinal))
            .Select(l => l.Split('|')[2].Trim())];
        Assert.Equal(33, shortcuts.Length); // ui-spec §6 rows

        // Every key the specification names is in the table.
        Assert.All(SpecShortcuts, token => Assert.Contains(token, section, StringComparison.Ordinal));
        string[] missing = [.. SpecShortcuts.Where(token => !CommandTable.All.Any(e =>
            (e.Gesture is { } g && Normalize(g).Split(", ").Contains(token))
            || e.Keys.Any(k => new KeyGesture(k.Key, k.Modifiers).GetDisplayStringForCulture(System.Globalization.CultureInfo.InvariantCulture) == token)))];
        Assert.Empty(missing);
    }

    private static string Normalize(string gesture) => gesture.Replace("Ctrl++", "Ctrl+Plus", StringComparison.Ordinal)
        .Replace("Ctrl+-", "Ctrl+Minus", StringComparison.Ordinal).Replace(" / ", ", ", StringComparison.Ordinal);

    [Fact]
    public void FR_091_every_available_command_is_reachable_by_keyboard()
    {
        WithWindow((window, _, _) =>
        {
            var problems = new List<string>();
            foreach (CommandEntry entry in CommandTable.All.Where(e => e.DeferredTo is null))
            {
                switch (entry.Scope)
                {
                    case ShortcutScope.Window:
                        problems.AddRange(entry.Keys.Where(k => window.ResolveShortcut(k.Key, k.Modifiers) is null).Select(k => $"{entry.Name}: no key binding for {k}"));
                        break;
                    case ShortcutScope.Editor when entry.Name == "Copy in the console":
                        var console = (AssemblyStudio.App.Panels.Console.ConsoleView)window.FindName("ConsoleOutput");
                        problems.AddRange(entry.Keys.Where(k => !console.InputBindings.OfType<KeyBinding>().Any(b => b.Key == k.Key && b.Modifiers == k.Modifiers))
                            .Select(k => $"{entry.Name}: the console has no binding for {k}"));
                        break;
                    case ShortcutScope.Editor when entry.Name is "Find" or "Find next" or "Find previous" or "Replace" or "Go to Line":
                        var editorPanel = (FrameworkElement)window.FindName("EditorPanel"); // TASK-065: the editor and the find bar
                        problems.AddRange(entry.Keys.Where(k => !editorPanel.InputBindings.OfType<KeyBinding>().Any(b => b.Key == k.Key && b.Modifiers == k.Modifiers && b.Command is not null))
                            .Select(k => $"{entry.Name}: the editor area has no binding for {k}"));
                        break;
                    case ShortcutScope.Editor:
                        RoutedUICommand command = entry.Name switch
                        {
                            "Undo" => ApplicationCommands.Undo,
                            "Redo" => ApplicationCommands.Redo,
                            "Cut" => ApplicationCommands.Cut,
                            "Copy" => ApplicationCommands.Copy,
                            "Paste" => ApplicationCommands.Paste,
                            _ => ApplicationCommands.SelectAll,
                        };
                        problems.AddRange(entry.Keys.Where(k => !command.InputGestures.OfType<KeyGesture>().Any(g => g.Key == k.Key && g.Modifiers == k.Modifiers))
                            .Select(k => $"{entry.Name}: {k} is not a gesture of {command.Name}"));
                        break;
                    case ShortcutScope.MachinePanel:
                        var machine = (FrameworkElement)window.FindName("MachineRegion");
                        problems.AddRange(entry.Keys.Where(k => !machine.InputBindings.OfType<KeyBinding>().Any(b => b.Key == k.Key && b.Modifiers == k.Modifiers && b.Command is not null))
                            .Select(k => $"{entry.Name}: the machine panels have no binding for {k}"));
                        break;
                    case ShortcutScope.MenuAccess or ShortcutScope.System:
                        break; // checked through the menu below
                }
            }

            Assert.Empty(problems);
        });
    }

    [Fact]
    public void FR_091_every_menu_command_has_an_access_key_and_shows_its_shortcut()
    {
        WithWindow((window, _, _) =>
        {
            var menu = (Menu)window.FindName("MainMenu");
            List<MenuItem> items = [.. MenuItems(menu)];
            Assert.All(menu.Items.OfType<MenuItem>(), top => Assert.Contains('_', (string)top.Header));
            var problems = new List<string>();
            foreach (CommandEntry entry in CommandTable.All.Where(e => e.MenuHeader is not null))
            {
                MenuItem? item = items.FirstOrDefault(i => i.Items.Count == 0 && i.Header as string == entry.MenuHeader); // a command, not a menu
                if (item is null)
                {
                    problems.Add($"{entry.Name}: no menu item");
                    continue;
                }

                if (!entry.MenuHeader!.Contains('_', StringComparison.Ordinal))
                {
                    problems.Add($"{entry.Name}: no access key in '{entry.MenuHeader}'");
                }

                if (entry.Gesture is not null && entry.Scope != ShortcutScope.MenuAccess && item.InputGestureText != entry.Gesture)
                {
                    problems.Add($"{entry.Name}: menu shows '{item.InputGestureText}', expected '{entry.Gesture}'");
                }

                if (entry.DeferredTo is not null && item.IsEnabled)
                {
                    problems.Add($"{entry.Name}: enabled although it arrives with {entry.DeferredTo}");
                }
            }

            Assert.Empty(problems);
        });
    }

    [Fact]
    public void The_command_bar_buttons_run_the_commands_and_their_tooltips_show_the_shortcuts()
    {
        WithWindow((window, shell, _) =>
        {
            (string Button, System.Windows.Input.ICommand Command, string Gesture)[] bar =
            [
                ("BarBuild", shell.BuildRun.BuildCommand, Strings.KeyBuild),
                ("BarRun", shell.BuildRun.RunCommand, Strings.KeyRun),
                ("BarPause", shell.BuildRun.PauseCommand, Strings.KeyPause),
                ("BarStop", shell.BuildRun.StopCommand, Strings.KeyStop),
                ("BarReset", shell.BuildRun.ResetCommand, Strings.KeyReset),
                ("BarStepInto", shell.BuildRun.StepIntoCommand, Strings.KeyStepInto),
                ("BarStepOver", shell.BuildRun.StepOverCommand, Strings.KeyStepOver),
            ];
            foreach ((string name, System.Windows.Input.ICommand command, string gesture) in bar)
            {
                var button = (Button)window.FindName(name);
                Assert.Same(command, button.Command);
                Assert.Contains($"({gesture})", (string)button.ToolTip, StringComparison.Ordinal);
            }
        });
    }

    [Fact]
    public void AC_013_and_AC_017_by_keyboard_in_the_main_window_problems_go_to_the_line_and_the_badge_is_a_live_region()
    {
        WithWindow((window, shell, workspace) =>
        {
            BuildRunViewModel run = shell.BuildRun;
            var badge = (TextBlock)window.FindName("StateBadge");
            Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting((TextBlock)window.FindName("AnnouncementRegion"))); // TASK-085

            DocumentViewModel document = workspace.New();
            document.Text = BuildRunTests.WarningAndError;
            window.UpdateLayout();
            Assert.True(window.TryRunShortcut(Key.B, ModifierKeys.Control | ModifierKeys.Shift)); // Ctrl+Shift+B
            BuildRunTests.WaitUntil(() => run.State == RunState.BuildFailed, "Build failed");
            window.UpdateLayout();
            Assert.Equal("⨯ Build failed", badge.Text);
            Assert.False(window.TryRunShortcut(Key.F5, ModifierKeys.None)); // AC-013: Run not enabled

            var list = (ListBox)window.FindName("ProblemsList");
            Assert.Equal(2, list.Items.Count);
            run.Problems.ActivateCommand.Execute(list.Items[1]); // the warning on line 3
            AssemblyStudio.App.Editor.CodeEditor editor = window.ActiveEditor()!;
            Assert.Equal(3, editor.TextArea.Caret.Line);
            Assert.Equal(3, editor.CaretLine); // the editor's property
            BuildRunTests.WaitUntil(() => document.StatusCaret == "Ln 3, Col 1", "the caret position in the status bar (UI-016)", seconds: 5);

            document.Text = BuildRunTests.Hello;
            Assert.True(window.TryRunShortcut(Key.F5, ModifierKeys.None)); // F5 rebuilds and runs
            BuildRunTests.WaitUntil(() => run.State == RunState.Terminated, "the program to end");
            window.UpdateLayout();
            Assert.Equal("■ Ended · exit 0", badge.Text);
            Assert.StartsWith("Ended · exit 0", ((TextBlock)window.FindName("StatusText")).Text, StringComparison.Ordinal);

            Assert.True(window.TryRunShortcut(Key.F5, ModifierKeys.Control | ModifierKeys.Shift)); // Ctrl+Shift+F5: Reset
            BuildRunTests.WaitUntil(() => run.State == RunState.Ready, "Ready after Reset");
            Assert.True(window.TryRunShortcut(Key.F10, ModifierKeys.None)); // F10 (a system key in WPF; routed by the window)
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "Paused after Step Over");
            Assert.True(window.TryRunShortcut(Key.F9, ModifierKeys.None)); // F9 on the caret line
            Assert.Single(document.Breakpoints);
            Assert.True(window.TryRunShortcut(Key.F5, ModifierKeys.Shift)); // Shift+F5: Stop
            Assert.Equal(RunState.Editing, run.State);
        });
    }

    [Fact]
    public void Renders_the_failed_build_and_the_paused_session_for_review()
    {
        WithWindow((window, shell, workspace) =>
        {
            BuildRunViewModel run = shell.BuildRun;
            DocumentViewModel document = workspace.New();
            document.Text = BuildRunTests.WarningAndError;
            shell.GoToCommand.Execute(ShellPanel.Problems);
            run.BuildCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.BuildFailed, "Build failed");
            window.UpdateLayout();
            Render(window, "build-failed.png");

            document.Text = BuildRunTests.Loop;
            document.ToggleBreakpointCommand.Execute(5);
            run.RunCommand.Execute(null);
            BuildRunTests.WaitUntil(() => run.State == RunState.Paused, "the breakpoint");
            window.UpdateLayout();
            Render(window, "session-paused.png");
        });
    }

    private static void Render(Window window, string fileName)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string dir = Path.Combine(MainWindowSmokeTests.RepoRoot(), "artifacts", "ui");
        Directory.CreateDirectory(dir);
        using FileStream file = File.Create(Path.Combine(dir, fileName));
        encoder.Save(file);
    }
}
