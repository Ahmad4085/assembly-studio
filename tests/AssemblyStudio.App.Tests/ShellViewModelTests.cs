using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// Shell layout rules (TASK-060; UI-001, ui-spec §2 and §6): F6 region order with hidden regions
/// skipped, the compact layout below 1280 px, View ▸ Go to, Reset Layout and persistence.
/// </summary>
public class ShellViewModelTests
{
    internal sealed class MemoryLayoutStore(LayoutState? saved = null) : ILayoutStore
    {
        public LayoutState? Saved { get; private set; } = saved;

        public LayoutState Load() => Saved ?? LayoutState.Default;

        public void Save(LayoutState layout) => Saved = layout;
    }

    private static ShellViewModel NewViewModel(LayoutState? saved = null) => new(new MemoryLayoutStore(saved), "1.2.3");

    [Fact]
    public void Starts_from_the_default_layout_with_learn_collapsed()
    {
        var vm = NewViewModel();

        Assert.False(vm.IsLearnShown);
        Assert.True(vm.IsMachineVisible);
        Assert.True(vm.IsBottomVisible);
        Assert.Equal(BottomTab.Console, vm.BottomTab);
        Assert.Equal("Assembly Studio 1.2.3", vm.StatusText);
    }

    [Theory]
    [InlineData(ShellRegion.Editor, ShellRegion.Machine)]
    [InlineData(ShellRegion.Machine, ShellRegion.Bottom)]
    [InlineData(ShellRegion.Bottom, ShellRegion.Editor)] // Learn is collapsed: skipped
    public void F6_cycles_editor_machine_bottom_when_learn_is_collapsed(ShellRegion from, ShellRegion expected) =>
        Assert.Equal(expected, NewViewModel().NextRegion(from, backwards: false));

    [Fact]
    public void F6_includes_learn_when_shown_and_shift_f6_goes_backwards()
    {
        var vm = NewViewModel(LayoutState.Default with { IsLearnVisible = true });

        Assert.Equal(ShellRegion.Learn, vm.NextRegion(ShellRegion.Bottom, backwards: false));
        Assert.Equal(ShellRegion.Editor, vm.NextRegion(ShellRegion.Learn, backwards: false));
        Assert.Equal(ShellRegion.Learn, vm.NextRegion(ShellRegion.Editor, backwards: true));
        Assert.Equal(ShellRegion.Machine, vm.NextRegion(ShellRegion.Bottom, backwards: true));
    }

    [Fact]
    public void F6_skips_hidden_machine_and_bottom_regions()
    {
        var vm = NewViewModel(LayoutState.Default with { IsMachineVisible = false, IsBottomVisible = false });

        Assert.Equal(ShellRegion.Editor, vm.NextRegion(ShellRegion.Editor, backwards: false));
        Assert.Equal(ShellRegion.Editor, vm.NextRegion(ShellRegion.Editor, backwards: true));
    }

    [Fact]
    public void Below_1280_px_learn_collapses_and_memory_stack_join_the_bottom_tabs()
    {
        var vm = NewViewModel(LayoutState.Default with { IsLearnVisible = true });

        vm.UpdateWindowWidth(1279);

        Assert.True(vm.IsCompact);
        Assert.True(vm.IsLearnVisible); // the user's choice is kept for wider windows
        Assert.False(vm.IsLearnShown);
        Assert.False(vm.IsMemoryStackDocked);
        Assert.Equal(ShellRegion.Editor, vm.NextRegion(ShellRegion.Bottom, backwards: false));

        vm.UpdateWindowWidth(1280);

        Assert.False(vm.IsCompact);
        Assert.True(vm.IsLearnShown);
        Assert.True(vm.IsMemoryStackDocked);
    }

    [Fact]
    public void Widening_past_1280_px_moves_a_selected_memory_tab_back_to_console()
    {
        var vm = NewViewModel();
        vm.UpdateWindowWidth(1100);
        vm.GoToCommand.Execute(ShellPanel.Memory);
        Assert.Equal(BottomTab.Memory, vm.BottomTab);

        vm.UpdateWindowWidth(1440);

        Assert.Equal(BottomTab.Console, vm.BottomTab);
    }

    [Theory]
    [InlineData(ShellPanel.Console, BottomTab.Console)]
    [InlineData(ShellPanel.Problems, BottomTab.Problems)]
    [InlineData(ShellPanel.Runtime, BottomTab.Runtime)]
    [InlineData(ShellPanel.History, BottomTab.History)]
    [InlineData(ShellPanel.Breakpoints, BottomTab.Breakpoints)]
    public void Go_to_a_bottom_panel_shows_the_area_selects_the_tab_and_requests_focus(ShellPanel panel, BottomTab tab)
    {
        var vm = NewViewModel(LayoutState.Default with { IsBottomVisible = false, BottomTab = BottomTab.History });
        ShellPanel? activated = null;
        vm.PanelActivated += (_, p) => activated = p;

        vm.GoToCommand.Execute(panel);

        Assert.True(vm.IsBottomVisible);
        Assert.Equal(tab, vm.BottomTab);
        Assert.Equal(panel, activated);
    }

    [Fact]
    public void Go_to_registers_shows_a_hidden_machine_column()
    {
        var vm = NewViewModel(LayoutState.Default with { IsMachineVisible = false });
        ShellPanel? activated = null;
        vm.PanelActivated += (_, p) => activated = p;

        vm.GoToCommand.Execute(ShellPanel.Registers);

        Assert.True(vm.IsMachineVisible);
        Assert.Equal(ShellPanel.Registers, activated);
    }

    [Fact]
    public void Go_to_learn_in_the_compact_layout_keeps_the_request_but_moves_no_focus()
    {
        var vm = NewViewModel();
        vm.UpdateWindowWidth(1100);
        ShellPanel? activated = null;
        vm.PanelActivated += (_, p) => activated = p;

        vm.GoToCommand.Execute(ShellPanel.Learn);

        Assert.True(vm.IsLearnVisible);
        Assert.Null(activated);
    }

    [Fact]
    public void Reset_layout_restores_the_defaults_and_notifies_the_view()
    {
        var vm = NewViewModel(LayoutState.Default with
        {
            LearnWidth = 333, MachineWidth = 444, BottomHeight = 300, IsLearnVisible = true, IsMachineVisible = false,
            BottomTab = BottomTab.History, WindowWidth = 1100,
        });
        bool reset = false;
        vm.LayoutReset += (_, _) => reset = true;

        vm.ResetLayoutCommand.Execute(null);

        Assert.True(reset);
        Assert.Equal(LayoutState.Default, vm.CaptureLayout(LayoutState.Default.WindowWidth, LayoutState.Default.WindowHeight, false));
    }

    [Fact]
    public void Save_persists_sizes_visibility_tab_and_window_bounds()
    {
        var store = new MemoryLayoutStore();
        var vm = new ShellViewModel(store, "1.0.0")
        {
            LearnWidth = 240.4, IsLearnVisible = true, IsBottomVisible = false, BottomTab = BottomTab.Problems,
        };

        vm.Save(1300, 820, isMaximized: true);

        Assert.Equal(
            LayoutState.Default with
            {
                LearnWidth = 240, IsLearnVisible = true, IsBottomVisible = false, BottomTab = BottomTab.Problems,
                WindowWidth = 1300, WindowHeight = 820, IsMaximized = true,
            },
            store.Saved);

        var restored = new ShellViewModel(store, "1.0.0");
        Assert.Equal(store.Saved, restored.CaptureLayout(1300, 820, true));
    }
}
