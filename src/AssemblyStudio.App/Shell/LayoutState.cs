namespace AssemblyStudio.App.Shell;

/// <summary>
/// Persisted shell layout (UI-001, ui-spec §2 "Persistence"): region sizes in device-independent
/// pixels, region visibility, the selected bottom tab and the window bounds. Immutable; the shell
/// view model owns the current value.
/// </summary>
/// <param name="LearnWidth">Learn sidebar width.</param>
/// <param name="MachineWidth">Machine column width (registers, flags).</param>
/// <param name="BottomHeight">Bottom area height (tab group, memory, stack).</param>
/// <param name="MemoryStackWidth">Width of the memory/stack group on the bottom right.</param>
/// <param name="IsLearnVisible">Learn sidebar shown (default collapsed until the user opens it).</param>
/// <param name="IsMachineVisible">Machine column shown.</param>
/// <param name="IsBottomVisible">Bottom area shown.</param>
/// <param name="BottomTab">Selected bottom tab.</param>
/// <param name="WindowWidth">Restored window width.</param>
/// <param name="WindowHeight">Restored window height.</param>
/// <param name="IsMaximized">Restored maximized state.</param>
public sealed record LayoutState(
    double LearnWidth,
    double MachineWidth,
    double BottomHeight,
    double MemoryStackWidth,
    bool IsLearnVisible,
    bool IsMachineVisible,
    bool IsBottomVisible,
    BottomTab BottomTab,
    double WindowWidth,
    double WindowHeight,
    bool IsMaximized)
{
    /// <summary>Minimum window size (NFR-026, design tokens <c>Size.MinWindow</c>).</summary>
    public const double MinWindowWidth = 1024;

    /// <inheritdoc cref="MinWindowWidth"/>
    public const double MinWindowHeight = 768;

    /// <summary>Below this window width the Learn sidebar collapses and Memory/Stack join the
    /// bottom tab group (ui-spec §2).</summary>
    public const double CompactBelowWidth = 1280;

    private const double MinRegion = 120;

    /// <summary>The default layout, also used by View ▸ Reset Layout.</summary>
    public static LayoutState Default { get; } = new(
        LearnWidth: 200, MachineWidth: 280, BottomHeight: 220, MemoryStackWidth: 520,
        IsLearnVisible: false, IsMachineVisible: true, IsBottomVisible: true,
        BottomTab: BottomTab.Console,
        WindowWidth: 1440, WindowHeight: 900, IsMaximized: false);

    /// <summary>Replaces out-of-range or non-finite values (e.g. from a hand-edited settings file)
    /// with safe ones, so a bad file can never produce an unusable window.</summary>
    public LayoutState Sanitized() => this with
    {
        LearnWidth = Clamp(LearnWidth, MinRegion, 600, Default.LearnWidth),
        MachineWidth = Clamp(MachineWidth, MinRegion, 800, Default.MachineWidth),
        BottomHeight = Clamp(BottomHeight, MinRegion, 800, Default.BottomHeight),
        MemoryStackWidth = Clamp(MemoryStackWidth, 240, 1600, Default.MemoryStackWidth),
        BottomTab = Enum.IsDefined(BottomTab) ? BottomTab : BottomTab.Console,
        WindowWidth = Clamp(WindowWidth, MinWindowWidth, 10000, Default.WindowWidth),
        WindowHeight = Clamp(WindowHeight, MinWindowHeight, 10000, Default.WindowHeight),
    };

    private static double Clamp(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}

/// <summary>Tabs of the bottom tab group (ui-spec §2). Memory and Stack are tabs only in the
/// compact layout; otherwise they sit beside the group.</summary>
public enum BottomTab
{
    /// <summary>Program console (UI-010).</summary>
    Console,

    /// <summary>Build problems (UI-011).</summary>
    Problems,

    /// <summary>Runtime messages (UI-012).</summary>
    Runtime,

    /// <summary>Execution history (UI-013).</summary>
    History,

    /// <summary>Breakpoints (UI-014).</summary>
    Breakpoints,

    /// <summary>Memory (UI-008), compact layout only.</summary>
    Memory,

    /// <summary>Stack (UI-009), compact layout only.</summary>
    Stack,
}
