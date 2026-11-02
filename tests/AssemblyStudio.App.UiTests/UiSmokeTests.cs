using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AssemblyStudio.App.UiTests;

/// <summary>Runs the UI tests one at a time: they start the real application and the keyboard test needs the foreground.</summary>
[CollectionDefinition(nameof(UiTestGroup), DisableParallelization = true)]
public sealed class UiTestGroup;

/// <summary>
/// TASK-085 UI smoke tests with FlaUI.UIA3 (ADR-0008) against the real application. The structure test reads only UI
/// Automation and always runs. The keyboard-only workflow (AC-043) presses real keys, so it is opt-in (the user's decision,
/// 2026-11-02): set <c>ASSEMBLY_STUDIO_UI_INPUT=1</c>, click the Assembly Studio window when it appears if Windows keeps another
/// window in front, and leave the PC alone while it runs; every key is sent only while the application has the keyboard focus.
/// </summary>
[Collection(nameof(UiTestGroup))]
public sealed class UiSmokeTests
{
    private const string InputVariable = "ASSEMBLY_STUDIO_UI_INPUT";

    private static AutomationElement Find(AutomationElement parent, Func<FlaUI.Core.Conditions.ConditionFactory, FlaUI.Core.Conditions.ConditionBase> condition, string what) =>
        parent.FindFirstDescendant(condition) ?? throw new Xunit.Sdk.XunitException($"Not found: {what}");
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

    [Fact]
    public void The_main_window_exposes_named_menus_commands_panels_and_the_editor()
    {
        using var app = new AppUnderTest();
        Window window = app.Window;

        Assert.Equal("Assembly Studio", window.Title.Split(" — ")[0].Split(" - ")[0].Trim());
        AutomationElement menuBar = Find(window, cf => cf.ByAutomationId("MainMenu"), "the main menu");
        string[] menus = [.. menuBar.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem)).Select(m => m.Name)];
        Assert.Equal(["File", "Edit", "View", "Build", "Debug", "Learn", "Help"], menus);

        foreach (string id in new[] { "BarBuild", "BarRun", "BarPause", "BarStop", "BarReset", "BarStepInto", "BarStepOver" })
        {
            AutomationElement button = Find(window, cf => cf.ByAutomationId(id), id);
            Assert.False(string.IsNullOrWhiteSpace(button.Name), id);
        }

        Assert.NotNull(window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document).And(cf.ByName("Untitled"))));
        foreach (string tab in new[] { "Console", "Problems", "Runtime", "History", "Breakpoints" })
        {
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName(tab))));
        }

        foreach (string id in new[] { "RegistersPanel", "FlagsPanel", "MemoryPanel", "StackView", "AnnouncementRegion", "StateBadge" })
        {
            Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId(id)));
        }
    }

    /// <summary>AC-043 (FR-091, FR-092): open a sample, build, set a breakpoint, run, step and read the registers with the
    /// keyboard only; after every step the focused element is in the application, has keyboard focus and is on screen.</summary>
    [Fact]
    public void AC_043_the_core_workflow_works_by_keyboard_alone()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable(InputVariable) == "1",
            $"Presses real keys: set {InputVariable}=1 and leave the PC alone to run it (TASK-085, opt-in).");
        using var app = new AppUnderTest();
        Window window = app.Window;
        AutomationElement badge = Find(window, cf => cf.ByAutomationId("StateBadge"), "the state badge");

        void Keys(params VirtualKeyShort[] keys)
        {
            app.EnsureForeground();
            if (keys.Length == 1)
            {
                Keyboard.Type(keys[0]);
            }
            else
            {
                Keyboard.TypeSimultaneously(keys);
            }

            Thread.Sleep(150);
        }

        // The element with keyboard focus, read without judging it (focus moves while menus close).
        AutomationElement Current() => app.Automation.FocusedElement();

        // FR-092 at a settled point: the focus is in the application, on an element that has it and is on screen.
        AutomationElement Focused()
        {
            AutomationElement focused = Current();
            TestContext.Current.TestOutputHelper?.WriteLine($"focus: {focused.ControlType} '{focused.Name}'");
            Assert.Equal(app.ProcessId, focused.Properties.ProcessId.Value);
            Assert.True(focused.Properties.HasKeyboardFocus.ValueOrDefault, $"'{focused.Name}' has no keyboard focus");
            Assert.False(focused.Properties.IsOffscreen.ValueOrDefault, $"'{focused.Name}' is off screen");
            Assert.True(focused.BoundingRectangle.Width > 0 && focused.BoundingRectangle.Height > 0, $"'{focused.Name}' has no size");
            return focused;
        }

        string CurrentName()
        {
            try
            {
                return Current().Name;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return string.Empty;
            }
        }

        void Until(Func<bool> condition, string what) => Assert.True(Retry.WhileFalse(condition, Wait, TimeSpan.FromMilliseconds(100)).Result, $"Timed out: {what}");

        // 1. Open the Hello sample: Learn ▸ Samples (Alt+L, S), Down to the first sample, Enter opens a copy.
        app.EnsureForeground();
        Keys(VirtualKeyShort.ALT, VirtualKeyShort.KEY_L);
        Keys(VirtualKeyShort.KEY_S);
        Until(() => CurrentName() == "Samples", "the Samples section has focus");
        Focused();
        Keys(VirtualKeyShort.DOWN);
        Until(() => CurrentName() == "Hello, World", "the Hello sample has focus");
        Focused();
        Keys(VirtualKeyShort.ENTER);
        Until(() => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document).And(cf.ByName("hello.asm (copy)"))) is not null, "the copy opens");

        // 2. Move to the editor with F6 (next pane) until the document has focus.
        Thread.Sleep(500);
        for (int i = 0; i < 6 && CurrentName() != "hello.asm (copy)"; i++)
        {
            Keys(VirtualKeyShort.F6);
            Thread.Sleep(300);
        }

        Assert.Equal("hello.asm (copy)", Focused().Name);

        // 3. Build (Ctrl+Shift+B).
        Keys(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_B);
        Until(() => badge.Name.Contains("Ready", StringComparison.Ordinal), "Ready after the build");
        Focused();

        // 4. Put the caret on the first "int 21h" line and set a breakpoint (F9).
        Keys(VirtualKeyShort.CONTROL, VirtualKeyShort.HOME);
        int line = 1;
        for (; line < 40; line++)
        {
            var range = Focused().Patterns.Text.Pattern!.GetSelection()[0];
            range.ExpandToEnclosingUnit(TextUnit.Line);
            if (range.GetText(-1).Trim().StartsWith("int 21h", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            Keys(VirtualKeyShort.DOWN);
        }

        Keys(VirtualKeyShort.F9);

        // 5. Run (F5): stops at the breakpoint.
        Keys(VirtualKeyShort.F5);
        Until(() => badge.Name.Contains($"Paused · line {line}", StringComparison.Ordinal), $"paused at the breakpoint on line {line}");
        Focused();

        // 6. Step Over the INT (F10) and Step Into (F11).
        Keys(VirtualKeyShort.F10);
        Until(() => badge.Name.Contains("Paused · line", StringComparison.Ordinal) && !badge.Name.EndsWith($"line {line}", StringComparison.Ordinal), "a step");
        string announcement = Find(window, cf => cf.ByAutomationId("AnnouncementRegion"), "the announcement region").Name;
        Assert.StartsWith("Paused at line", announcement, StringComparison.Ordinal);

        // 7. Read the registers: Ctrl+Alt+R puts the focus on the register list; the focused row is named "AX …".
        Keys(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.KEY_R);
        Until(() => CurrentName().StartsWith("AX", StringComparison.Ordinal) || CurrentName() == "Registers", "the registers have focus");
        if (CurrentName() == "Registers")
        {
            Keys(VirtualKeyShort.DOWN); // into the list
        }

        Assert.StartsWith("AX ", Focused().Name, StringComparison.Ordinal);

        // 8. Stop (Shift+F5).
        Keys(VirtualKeyShort.SHIFT, VirtualKeyShort.F5);
        Until(() => badge.Name.Contains("Not built", StringComparison.Ordinal) || badge.Name.Length == 0, "stopped");
    }
}
