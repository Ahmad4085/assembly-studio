using System.Globalization;
using System.Text;
using AssemblyStudio.App.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Shell;

/// <summary>Focus regions of the main window, in F6 order (ui-spec §2 "Focus targets").</summary>
public enum ShellRegion
{
    /// <summary>Editor (initial focus).</summary>
    Editor,

    /// <summary>Machine column: registers and flags.</summary>
    Machine,

    /// <summary>Bottom area: tab group plus memory and stack.</summary>
    Bottom,

    /// <summary>Learn sidebar.</summary>
    Learn,
}

/// <summary>Panels reachable through the View ▸ Go to commands (ui-spec §6).</summary>
public enum ShellPanel
{
    /// <summary>Registers (UI-005).</summary>
    Registers,

    /// <summary>Flags (UI-006).</summary>
    Flags,

    /// <summary>Memory (UI-008).</summary>
    Memory,

    /// <summary>Stack (UI-009).</summary>
    Stack,

    /// <summary>Console (UI-010).</summary>
    Console,

    /// <summary>Problems (UI-011).</summary>
    Problems,

    /// <summary>Runtime messages (UI-012).</summary>
    Runtime,

    /// <summary>Execution history (UI-013).</summary>
    History,

    /// <summary>Breakpoints (UI-014).</summary>
    Breakpoints,

    /// <summary>Learn sidebar (UI-015).</summary>
    Learn,
}

/// <summary>
/// Main window shell (UI-001, UI-002 skeleton, UI-016): region sizes and visibility, the compact
/// layout below 1280 px, the F6 region order, the View ▸ Go to panel commands, Reset Layout and
/// persistence. Panel contents arrive with their own tasks; until then every panel shows its
/// empty state (NFR-018: no sample machine data). No WPF types: the view applies sizes and focus.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private static readonly ShellRegion[] RegionOrder =
        [ShellRegion.Editor, ShellRegion.Machine, ShellRegion.Bottom, ShellRegion.Learn];

    private readonly ILayoutStore _store;

    /// <summary>Creates the shell state from the saved layout.</summary>
    /// <param name="store">Layout persistence.</param>
    /// <param name="appVersion">Version shown in the status bar while no document is open.</param>
    /// <param name="workspace">The open documents and file workflow (TASK-062); by default one with the WPF dialogs
    /// and an in-memory recent-files list.</param>
    /// <param name="openSettings">Opens the Settings dialog (TASK-067); the command is disabled without it.</param>
    /// <param name="openAbout">Opens the About window on a page (TASK-068); the commands are disabled without it.</param>
    /// <param name="learn">The Learn sidebar (TASK-090); by default one over the built-in content catalog.</param>
    /// <param name="buildRun">The build and run commands (TASK-080); by default the bundled NASM and the 8086 session.</param>
    public ShellViewModel(
        ILayoutStore store,
        string appVersion,
        WorkspaceViewModel? workspace = null,
        Action? openSettings = null,
        Action<About.AboutPage>? openAbout = null,
        Learn.LearnViewModel? learn = null,
        Commands.BuildRunViewModel? buildRun = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        OpenSettingsCommand = new RelayCommand(() => openSettings?.Invoke(), () => openSettings is not null);
        OpenAboutCommand = new RelayCommand<About.AboutPage>(page => openAbout?.Invoke(page), _ => openAbout is not null);
        Workspace = workspace ?? new WorkspaceViewModel(new WpfDialogService(), new MemoryRecentFilesStore());
        Learn = learn ?? new Learn.LearnViewModel(Content.EmbeddedContentCatalog.BuiltIn(), Workspace);
        BuildRun = buildRun ?? new Commands.BuildRunViewModel(
            Workspace,
            () => new Toolchain.Nasm.NasmAssembler(new Toolchain.Nasm.NasmOptions { BundledPath = Commands.SessionFactory.BundledAssemblerPath }),
            build => Commands.SessionFactory.Create(build, Settings.AppSettings.Default.InstructionBudget));
        StatusText = string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.StatusAppVersion), appVersion);
        BuildRun.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Commands.BuildRunViewModel.State))
            {
                OnRunStateChanged(BuildRun.State);
            }
        };
        LayoutState saved = store.Load();
        Apply(saved);
        ZoomPercent = saved.ZoomPercent;
    }

    /// <summary>Editor and debug-panel zoom in percent (FR-022); the application scales the code fonts by it.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ZoomInCommand), nameof(ZoomOutCommand), nameof(ZoomResetCommand))]
    public partial int ZoomPercent { get; private set; }

    /// <summary>Ctrl+M (Edit ▸ Tab Moves Focus): Tab and Shift+Tab move focus out of the editor instead of indenting.</summary>
    [ObservableProperty]
    public partial bool TabMovesFocus { get; set; }

    /// <summary>Ctrl+Plus and Ctrl+wheel up: the next larger zoom step.</summary>
    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private void ZoomIn() => ZoomPercent = LayoutState.ZoomSteps.First(z => z > ZoomPercent);

    /// <summary>Ctrl+Minus and Ctrl+wheel down: the next smaller zoom step.</summary>
    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private void ZoomOut() => ZoomPercent = LayoutState.ZoomSteps.Last(z => z < ZoomPercent);

    /// <summary>Ctrl+0: 100 %.</summary>
    [RelayCommand(CanExecute = nameof(CanZoomReset))]
    private void ZoomReset() => ZoomPercent = 100;

    [RelayCommand]
    private void ToggleTabMovesFocus() => TabMovesFocus = !TabMovesFocus;

    private bool CanZoomIn() => ZoomPercent < LayoutState.ZoomSteps[^1];

    private bool CanZoomOut() => ZoomPercent > LayoutState.ZoomSteps[0];

    private bool CanZoomReset() => ZoomPercent != 100;

    /// <summary>Build, Run, Step and the other session commands, the Problems panel and the state badge (TASK-080).</summary>
    public Commands.BuildRunViewModel BuildRun { get; }

    // TASK-084: the bottom tab follows the session: Problems after a failed build, Console while a program runs, and the
    // console takes the focus when the program waits for input (ui-spec §4: "input line focused").
    private void OnRunStateChanged(Commands.RunState state)
    {
        switch (state)
        {
            case Commands.RunState.BuildFailed:
                IsBottomVisible = true;
                BottomTab = BottomTab.Problems;
                break;
            case Commands.RunState.Running:
                IsBottomVisible = true;
                BottomTab = BottomTab.Console;
                break;
            case Commands.RunState.WaitingForInput:
                GoTo(ShellPanel.Console);
                break;
            case Commands.RunState.HaltedError or Commands.RunState.HaltedUnsupported:
                IsBottomVisible = true;
                BottomTab = BottomTab.Runtime; // the halt's what, where and next step (UI-012)
                break;
        }
    }

    /// <summary>The Learn sidebar (UI-015).</summary>
    public Learn.LearnViewModel Learn { get; }

    /// <summary>File ▸ Settings (Ctrl+,).</summary>
    public IRelayCommand OpenSettingsCommand { get; }

    /// <summary>Help ▸ About / Release Notes / Third-Party Notices; the parameter is the page.</summary>
    public IRelayCommand<About.AboutPage> OpenAboutCommand { get; }

    /// <summary>Raised after a Go to command made a panel visible; the view moves focus to it.</summary>
    public event EventHandler<ShellPanel>? PanelActivated;

    /// <summary>Raised after Reset Layout; the view re-applies sizes and the window bounds.</summary>
    public event EventHandler? LayoutReset;

    [ObservableProperty]
    public partial double LearnWidth { get; set; }

    [ObservableProperty]
    public partial double MachineWidth { get; set; }

    [ObservableProperty]
    public partial double BottomHeight { get; set; }

    [ObservableProperty]
    public partial double MemoryStackWidth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLearnShown))]
    public partial bool IsLearnVisible { get; set; }

    [ObservableProperty]
    public partial bool IsMachineVisible { get; set; }

    [ObservableProperty]
    public partial bool IsBottomVisible { get; set; }

    [ObservableProperty]
    public partial BottomTab BottomTab { get; set; }

    /// <summary>Window narrower than 1280 px: Learn collapses, Memory/Stack become bottom tabs.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLearnShown), nameof(IsMemoryStackDocked))]
    public partial bool IsCompact { get; private set; }

    /// <summary>Saved window size and state, applied by the view at startup and on reset.</summary>
    public double WindowWidth { get; private set; }

    /// <inheritdoc cref="WindowWidth"/>
    public double WindowHeight { get; private set; }

    /// <inheritdoc cref="WindowWidth"/>
    public bool IsMaximized { get; private set; }

    /// <summary>The Learn sidebar is actually on screen.</summary>
    public bool IsLearnShown => IsLearnVisible && !IsCompact;

    /// <summary>Memory and Stack sit beside the bottom tab group (not compact).</summary>
    public bool IsMemoryStackDocked => !IsCompact;

    /// <summary>The open documents and the file workflow (UI-003, UI-019, UI-020).</summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>Status bar text (UI-016, "Empty" state: the app version).</summary>
    public string StatusText { get; }

    /// <summary>Updates the compact layout from the window width (ui-spec §2).</summary>
    public void UpdateWindowWidth(double width)
    {
        IsCompact = width < LayoutState.CompactBelowWidth;
        if (!IsCompact && BottomTab is BottomTab.Memory or BottomTab.Stack)
        {
            BottomTab = BottomTab.Console;
        }
    }

    /// <summary>The region F6 (or Shift+F6 when <paramref name="backwards"/>) moves to from
    /// <paramref name="current"/>, skipping hidden regions. The editor is always available.</summary>
    public ShellRegion NextRegion(ShellRegion current, bool backwards)
    {
        int index = Array.IndexOf(RegionOrder, current);
        for (int i = 1; i <= RegionOrder.Length; i++)
        {
            int step = backwards ? -i : i;
            ShellRegion candidate = RegionOrder[((index + step) % RegionOrder.Length + RegionOrder.Length) % RegionOrder.Length];
            if (IsRegionShown(candidate))
            {
                return candidate;
            }
        }

        return ShellRegion.Editor;
    }

    /// <summary>Whether a region is on screen and can take focus.</summary>
    public bool IsRegionShown(ShellRegion region) => region switch
    {
        ShellRegion.Machine => IsMachineVisible,
        ShellRegion.Bottom => IsBottomVisible,
        ShellRegion.Learn => IsLearnShown,
        _ => true,
    };

    /// <summary>The region that hosts a panel in the current layout.</summary>
    public static ShellRegion RegionOf(ShellPanel panel) => panel switch
    {
        ShellPanel.Registers or ShellPanel.Flags => ShellRegion.Machine,
        ShellPanel.Learn => ShellRegion.Learn,
        _ => ShellRegion.Bottom,
    };

    /// <summary>Snapshot of the current layout with the given window bounds.</summary>
    public LayoutState CaptureLayout(double windowWidth, double windowHeight, bool isMaximized) => new LayoutState(
        Math.Round(LearnWidth), Math.Round(MachineWidth), Math.Round(BottomHeight), Math.Round(MemoryStackWidth),
        IsLearnVisible, IsMachineVisible, IsBottomVisible,
        BottomTab is BottomTab.Memory or BottomTab.Stack ? BottomTab.Console : BottomTab,
        Math.Round(windowWidth), Math.Round(windowHeight), isMaximized) { ZoomPercent = ZoomPercent }.Sanitized();

    /// <summary>Saves the layout (called when the window closes).</summary>
    public void Save(double windowWidth, double windowHeight, bool isMaximized) =>
        _store.Save(CaptureLayout(windowWidth, windowHeight, isMaximized));

    /// <summary>View ▸ Go to &lt;panel&gt;: shows the panel's region, selects its tab and asks the
    /// view to focus it. In the compact layout Learn can't be shown, so going there expands the
    /// window's sidebar setting but focus stays where it is.</summary>
    [RelayCommand]
    private void GoTo(ShellPanel panel)
    {
        switch (RegionOf(panel))
        {
            case ShellRegion.Machine:
                IsMachineVisible = true;
                break;
            case ShellRegion.Learn:
                IsLearnVisible = true;
                break;
            default:
                IsBottomVisible = true;
                BottomTab? tab = panel switch
                {
                    ShellPanel.Console => BottomTab.Console,
                    ShellPanel.Problems => BottomTab.Problems,
                    ShellPanel.Runtime => BottomTab.Runtime,
                    ShellPanel.History => BottomTab.History,
                    ShellPanel.Breakpoints => BottomTab.Breakpoints,
                    ShellPanel.Memory when IsCompact => BottomTab.Memory,
                    ShellPanel.Stack when IsCompact => BottomTab.Stack,
                    _ => null,
                };
                if (tab is not null)
                {
                    BottomTab = tab.Value;
                }

                break;
        }

        if (IsRegionShown(RegionOf(panel)))
        {
            PanelActivated?.Invoke(this, panel);
        }
    }

    /// <summary>Learn ▸ Samples / Tutorials / Reference and the welcome page's "Open a sample": shows the Learn sidebar
    /// with that section selected and moves focus there.</summary>
    [RelayCommand]
    private void ShowLearnSection(Content.ContentKind kind)
    {
        Learn.ShowSection(kind);
        GoTo(ShellPanel.Learn);
    }

    [RelayCommand]
    private void ToggleLearn() => IsLearnVisible = !IsLearnVisible;

    [RelayCommand]
    private void ToggleMachine() => IsMachineVisible = !IsMachineVisible;

    [RelayCommand]
    private void ToggleBottom() => IsBottomVisible = !IsBottomVisible;

    /// <summary>View ▸ Reset Layout: restores the default sizes, visibility and window size.</summary>
    [RelayCommand]
    private void ResetLayout()
    {
        Apply(LayoutState.Default);
        UpdateWindowWidth(LayoutState.Default.WindowWidth);
        LayoutReset?.Invoke(this, EventArgs.Empty);
    }

    private void Apply(LayoutState layout)
    {
        // Visibility first: the view captures the on-screen sizes when a region hides, so sizes
        // set afterwards are the ones that stick.
        IsLearnVisible = layout.IsLearnVisible;
        IsMachineVisible = layout.IsMachineVisible;
        IsBottomVisible = layout.IsBottomVisible;
        LearnWidth = layout.LearnWidth;
        MachineWidth = layout.MachineWidth;
        BottomHeight = layout.BottomHeight;
        MemoryStackWidth = layout.MemoryStackWidth;
        BottomTab = layout.BottomTab;
        WindowWidth = layout.WindowWidth;
        WindowHeight = layout.WindowHeight;
        IsMaximized = layout.IsMaximized;
    }
}
