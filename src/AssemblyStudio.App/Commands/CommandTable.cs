using System.Windows.Input;
using AssemblyStudio.App.Shell;

namespace AssemblyStudio.App.Commands;

/// <summary>Where a command's shortcut is handled.</summary>
public enum ShortcutScope
{
    /// <summary>A key binding of the main window: works wherever focus is in the window.</summary>
    Window,

    /// <summary>Handled by the focused editor (the standard editing commands, F9).</summary>
    Editor,

    /// <summary>Reached through the menu's access keys only (for example Alt+F, R).</summary>
    MenuAccess,

    /// <summary>Handled by Windows (Alt+F4).</summary>
    System,

    /// <summary>A key binding of the machine panels (registers, flags, memory, stack): works when one of them has focus.</summary>
    MachinePanel,
}

/// <summary>A key and its modifiers.</summary>
/// <param name="Key">The key.</param>
/// <param name="Modifiers">The modifiers.</param>
public readonly record struct Shortcut(Key Key, ModifierKeys Modifiers = ModifierKeys.None);

/// <summary>One command of ui-spec §6.</summary>
/// <param name="Name">The command, as ui-spec §6 names it.</param>
/// <param name="MenuHeader">Its menu item's header (with the access key), or <see langword="null"/> when it has no menu item of its own.</param>
/// <param name="Gesture">The shortcut as menus and tooltips show it, or <see langword="null"/>.</param>
/// <param name="Scope">Where the shortcut is handled.</param>
/// <param name="Keys">The key presses that run it (alternatives).</param>
/// <param name="DeferredTo">The task that delivers it, when it isn't available yet; <see langword="null"/> when it is.</param>
public sealed record CommandEntry(string Name, string? MenuHeader, string? Gesture, ShortcutScope Scope, IReadOnlyList<Shortcut> Keys, string? DeferredTo = null);

/// <summary>
/// The command table of ui-spec §6 (FR-091): every command, its menu item, shortcut and where the shortcut is handled. The
/// keyboard tests go over it, so a command added to the specification is added here and then checked. Commands whose
/// feature belongs to a later task name that task in <see cref="CommandEntry.DeferredTo"/>.
/// </summary>
public static class CommandTable
{
    private const ModifierKeys Ctrl = ModifierKeys.Control;
    private const ModifierKeys Shift = ModifierKeys.Shift;
    private const ModifierKeys Alt = ModifierKeys.Alt;

    /// <summary>All commands, in ui-spec §6 order.</summary>
    public static IReadOnlyList<CommandEntry> All { get; } =
    [
        new("New", Strings.CmdNew, Strings.KeyNew, ShortcutScope.Window, [new(Key.N, Ctrl)]),
        new("Open", Strings.CmdOpen, Strings.KeyOpen, ShortcutScope.Window, [new(Key.O, Ctrl)]),
        new("Save", Strings.CmdSave, Strings.KeySave, ShortcutScope.Window, [new(Key.S, Ctrl)]),
        new("Save As", Strings.CmdSaveAs, Strings.KeySaveAs, ShortcutScope.Window, [new(Key.S, Ctrl | Shift)]),
        new("Close tab", Strings.CmdCloseTab, Strings.KeyCloseTab, ShortcutScope.Window, [new(Key.W, Ctrl), new(Key.F4, Ctrl)]),
        new("Recent files", Strings.CmdRecent, null, ShortcutScope.MenuAccess, []),
        new("Exit", Strings.CmdExit, Strings.KeyExit, ShortcutScope.System, [new(Key.F4, Alt)]),
        new("Undo", Strings.CmdUndo, Strings.KeyUndo, ShortcutScope.Editor, [new(Key.Z, Ctrl)]),
        new("Redo", Strings.CmdRedo, Strings.KeyRedo, ShortcutScope.Editor, [new(Key.Y, Ctrl)]),
        new("Cut", Strings.CmdCut, Strings.KeyCut, ShortcutScope.Editor, [new(Key.X, Ctrl)]),
        new("Copy", Strings.CmdCopy, Strings.KeyCopy, ShortcutScope.Editor, [new(Key.C, Ctrl)]),
        new("Paste", Strings.CmdPaste, Strings.KeyPaste, ShortcutScope.Editor, [new(Key.V, Ctrl)]),
        new("Select all", Strings.CmdSelectAll, Strings.KeySelectAll, ShortcutScope.Editor, [new(Key.A, Ctrl)]),
        new("Find", Strings.CmdFind, Strings.KeyFind, ShortcutScope.Editor, [new(Key.F, Ctrl)]),
        new("Find next", Strings.CmdFindNext, Strings.KeyFindNext, ShortcutScope.Editor, [new(Key.F3)]),
        new("Find previous", Strings.CmdFindPrevious, Strings.KeyFindPrevious, ShortcutScope.Editor, [new(Key.F3, Shift)]),
        new("Replace", Strings.CmdReplace, Strings.KeyReplace, ShortcutScope.Editor, [new(Key.H, Ctrl)]),
        new("Go to Line", Strings.CmdGoToLine, Strings.KeyGoToLine, ShortcutScope.Editor, [new(Key.G, Ctrl)]),
        new("Toggle Tab moves focus", Strings.CmdTabMovesFocus, Strings.KeyTabMovesFocus, ShortcutScope.Window, [new(Key.M, Ctrl)]),
        new("Zoom in", Strings.CmdZoomIn, Strings.KeyZoomIn, ShortcutScope.Window, [new(Key.OemPlus, Ctrl), new(Key.Add, Ctrl)]),
        new("Zoom out", Strings.CmdZoomOut, Strings.KeyZoomOut, ShortcutScope.Window, [new(Key.OemMinus, Ctrl), new(Key.Subtract, Ctrl)]),
        new("Zoom reset", Strings.CmdZoomReset, Strings.KeyZoomReset, ShortcutScope.Window, [new(Key.D0, Ctrl), new(Key.NumPad0, Ctrl)]),
        new("Next pane", Strings.CmdNextPane, Strings.KeyNextPane, ShortcutScope.Window, [new(Key.F6)]),
        new("Previous pane", Strings.CmdPreviousPane, Strings.KeyPreviousPane, ShortcutScope.Window, [new(Key.F6, Shift)]),
        new("Go to Registers", Strings.GoRegisters, Strings.KeyGoRegisters, ShortcutScope.Window, [new(Key.R, Ctrl | Alt)]),
        new("Go to Flags", Strings.GoFlags, Strings.KeyGoFlags, ShortcutScope.Window, [new(Key.F, Ctrl | Alt)]),
        new("Go to Memory", Strings.GoMemory, Strings.KeyGoMemory, ShortcutScope.Window, [new(Key.M, Ctrl | Alt)]),
        new("Go to Stack", Strings.GoStack, Strings.KeyGoStack, ShortcutScope.Window, [new(Key.K, Ctrl | Alt)]),
        new("Go to Console", Strings.GoConsole, Strings.KeyGoConsole, ShortcutScope.Window, [new(Key.O, Ctrl | Alt)]),
        new("Go to Problems", Strings.GoProblems, Strings.KeyGoProblems, ShortcutScope.Window, [new(Key.P, Ctrl | Alt)]),
        new("Go to Runtime", Strings.GoRuntime, Strings.KeyGoRuntime, ShortcutScope.Window, [new(Key.U, Ctrl | Alt)]),
        new("Go to History", Strings.GoHistory, Strings.KeyGoHistory, ShortcutScope.Window, [new(Key.H, Ctrl | Alt)]),
        new("Go to Breakpoints", Strings.GoBreakpoints, Strings.KeyGoBreakpoints, ShortcutScope.Window, [new(Key.B, Ctrl | Alt)]),
        new("Go to Learn", Strings.GoLearn, Strings.KeyGoLearn, ShortcutScope.Window, [new(Key.L, Ctrl | Alt)]),
        new("Cycle number format", Strings.CmdCycleFormat, Strings.KeyCycleFormat, ShortcutScope.MachinePanel, [new(Key.N, Ctrl | Alt)]),
        new("Reset layout", Strings.CmdResetLayout, null, ShortcutScope.MenuAccess, []),
        new("Build", Strings.CmdBuild, Strings.KeyBuild, ShortcutScope.Window, [new(Key.B, Ctrl | Shift)]),
        new("Cancel build", Strings.CmdCancelBuild, Strings.KeyCancelBuild, ShortcutScope.Window, [new(Key.Escape)]),
        new("Run / Continue", Strings.CmdRun, Strings.KeyRun, ShortcutScope.Window, [new(Key.F5)]),
        new("Pause", Strings.CmdPause, Strings.KeyPause, ShortcutScope.Window, [new(Key.Cancel, Ctrl | Alt), new(Key.Pause, Ctrl | Alt)]),
        new("Stop", Strings.CmdStop, Strings.KeyStop, ShortcutScope.Window, [new(Key.F5, Shift)]),
        new("Reset", Strings.CmdReset, Strings.KeyReset, ShortcutScope.Window, [new(Key.F5, Ctrl | Shift)]),
        new("Step Into", Strings.CmdStepInto, Strings.KeyStepInto, ShortcutScope.Window, [new(Key.F11)]),
        new("Step Over", Strings.CmdStepOver, Strings.KeyStepOver, ShortcutScope.Window, [new(Key.F10)]),
        new("Toggle breakpoint", Strings.CmdToggleBreakpoint, Strings.KeyToggleBreakpoint, ShortcutScope.Window, [new(Key.F9)]),
        new("Run to Cursor", null, "Ctrl+F10", ShortcutScope.Window, [new(Key.F10, Ctrl)], "P2 (ui-spec §6), not planned for this release"),
        new("Send Ctrl-C to the program", Strings.CmdSendCtrlC, Strings.KeySendCtrlC, ShortcutScope.Window, [new(Key.Cancel, Ctrl)]),
        new("Copy in the console", null, Strings.KeyConsoleCopy, ShortcutScope.Editor, [new(Key.C, Ctrl | Shift)]), // the console's binding and context menu
        new("Samples", Strings.CmdSamples, null, ShortcutScope.MenuAccess, []),
        new("Tutorials", Strings.CmdTutorials, null, ShortcutScope.MenuAccess, []),
        new("Reference", Strings.CmdReference, null, ShortcutScope.MenuAccess, []),
        new("Settings", Strings.CmdSettings, Strings.KeySettings, ShortcutScope.Window, [new(Key.OemComma, Ctrl)]),
        new("Help", Strings.CmdHelp, Strings.KeyHelp, ShortcutScope.Window, [new(Key.F1)]),
        new("About", Strings.CmdAbout, null, ShortcutScope.MenuAccess, []),
    ];
}
