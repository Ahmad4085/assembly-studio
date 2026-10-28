using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssemblyStudio.App.Shell;
using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// UI smoke tests for the main window (TASK-060 test requirement: the window opens, F6 cycles the
/// regions). They run the real XAML on an STA thread. The window may not be the active one while
/// tests run, so focus is checked through the window's logical focus.
/// </summary>
public class MainWindowSmokeTests
{
    private static readonly string[] AlwaysShownRegions = ["EditorPanel", "RegistersPanel", "FlagsPanel", "BottomTabs"];

    private static void WithWindow(Action<MainWindow, ShellViewModel> body, LayoutState? layout = null)
    {
        StaTestThread.Run(() =>
        {
            var vm = new ShellViewModel(new ShellViewModelTests.MemoryLayoutStore(layout), "test");
            var window = new MainWindow(vm, persistLayout: false);
            try
            {
                window.Show();
                window.UpdateLayout();
                body(window, vm);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static T Element<T>(MainWindow window, string name) where T : class =>
        Assert.IsAssignableFrom<T>(window.FindName(name));

    private static void PressF6(MainWindow window)
    {
        var target = FocusManager.GetFocusedElement(window) ?? window;
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, 0, Key.F6)
        {
            RoutedEvent = Keyboard.KeyDownEvent,
        };
        target.RaiseEvent(args);
    }

    [Fact]
    public void Window_opens_with_every_region_and_the_editor_focused()
    {
        WithWindow((window, vm) =>
        {
            Assert.True(window.IsVisible);
            Assert.Equal(ShellRegion.Editor, window.CurrentRegion);
            foreach (string name in AlwaysShownRegions)
            {
                Assert.True(Element<UIElement>(window, name).IsVisible, name);
            }

            Assert.False(Element<UIElement>(window, "LearnPanel").IsVisible); // collapsed by default
            Assert.Equal(vm.IsMemoryStackDocked, Element<UIElement>(window, "MemoryPanel").IsVisible);
            Assert.Equal(!vm.IsMemoryStackDocked, Element<UIElement>(window, "MemoryTab").IsVisible);
            Assert.True(window.ActualWidth >= window.MinWidth);
        });
    }

    [Fact]
    public void F6_cycles_editor_machine_bottom_and_back()
    {
        WithWindow((window, _) =>
        {
            PressF6(window);
            Assert.Equal(ShellRegion.Machine, window.CurrentRegion);
            PressF6(window);
            Assert.Equal(ShellRegion.Bottom, window.CurrentRegion);
            PressF6(window);
            Assert.Equal(ShellRegion.Editor, window.CurrentRegion);
        });
    }

    [Fact]
    public void Shift_f6_cycles_backwards_through_a_shown_learn_sidebar()
    {
        WithWindow((window, vm) =>
        {
            vm.IsLearnVisible = true;
            window.UpdateLayout();
            ShellRegion expected = vm.IsLearnShown ? ShellRegion.Learn : ShellRegion.Bottom;

            window.CyclePane(backwards: true);

            Assert.Equal(expected, window.CurrentRegion);
        });
    }

    [Fact]
    public void Go_to_console_selects_and_focuses_its_tab()
    {
        WithWindow((window, vm) =>
        {
            vm.GoToCommand.Execute(ShellPanel.Problems);

            var tabs = Element<TabControl>(window, "BottomTabs");
            Assert.Equal((int)BottomTab.Problems, tabs.SelectedIndex);
            Assert.Same(tabs.SelectedItem, FocusManager.GetFocusedElement(window));
            Assert.Equal(ShellRegion.Bottom, window.CurrentRegion);
        });
    }

    [Fact]
    public void Hiding_the_focused_region_returns_focus_to_the_editor()
    {
        WithWindow((window, vm) =>
        {
            vm.GoToCommand.Execute(ShellPanel.Registers);
            Assert.Equal(ShellRegion.Machine, window.CurrentRegion);

            vm.IsMachineVisible = false;

            Assert.Equal(ShellRegion.Editor, window.CurrentRegion);
            Assert.False(Element<UIElement>(window, "MachineRegion").IsVisible);
        });
    }

    [Fact]
    public void View_menu_opens_with_shortcuts_and_reset_layout()
    {
        WithWindow((window, _) =>
        {
            var menu = FindChild<Menu>(window);
            var view = Assert.IsType<MenuItem>(menu.Items[2]);
            Assert.Equal(Strings.MenuView, view.Header);

            view.IsSubmenuOpen = true;
            window.UpdateLayout();

            Assert.True(view.IsSubmenuOpen);
            var next = Assert.IsType<MenuItem>(view.Items[0]);
            Assert.Equal("F6", next.InputGestureText);
            Assert.True(next.IsEnabled);
            Assert.Contains(view.Items.OfType<MenuItem>(), item => Equals(item.Header, Strings.CmdResetLayout) && item.IsEnabled);
            view.IsSubmenuOpen = false;
        });
    }

    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            if (FindChildOrNull<T>(child) is { } nested)
            {
                return nested;
            }
        }

        throw new InvalidOperationException($"No {typeof(T).Name} in the visual tree.");
    }

    private static T? FindChildOrNull<T>(DependencyObject parent) where T : DependencyObject
    {
        try
        {
            return FindChild<T>(parent);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Renders the empty shell to <c>artifacts/ui/shell-empty-dark.png</c> for visual review
    /// (in-process rendering of this window only; nothing else on the screen is captured).</summary>
    [Fact]
    public void Renders_the_empty_shell_for_review()
    {
        WithWindow((window, _) => Assert.True(Render(window, "shell-empty-dark.png") > 0));
    }

    /// <summary>Windows text size at 200 % with the window at its 1024 × 768 minimum (NFR-026):
    /// renders <c>artifacts/ui/shell-text200-min.png</c> for the clipping review.</summary>
    [Fact]
    public void Renders_200_percent_text_at_the_minimum_window_size_for_review()
    {
        StaTestThread.Run(() =>
        {
            ResourceDictionary resources = Application.Current.Resources;
            TextScale.Apply(resources, 2.0);
            try
            {
                WithWindow((window, _) =>
                {
                    window.MinWidth = window.MinHeight = 0;
                    window.Width = LayoutState.MinWindowWidth;
                    window.Height = LayoutState.MinWindowHeight;
                    window.UpdateLayout();
                    Assert.True(Render(window, "shell-text200-min.png") > 0);
                });
            }
            finally
            {
                foreach (string key in TextScale.FontSizeKeys)
                {
                    resources.Remove(key); // back to the theme dictionary values
                }
            }
        });
    }

    private static long Render(MainWindow window, string fileName)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        string dir = Path.Combine(RepoRoot(), "artifacts", "ui");
        Directory.CreateDirectory(dir);
        using FileStream file = File.Create(Path.Combine(dir, fileName));
        encoder.Save(file);
        return file.Length;
    }

    internal static string RepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
        {
            dir = dir.Parent;
        }

        return Assert.IsType<DirectoryInfo>(dir).FullName;
    }
}
