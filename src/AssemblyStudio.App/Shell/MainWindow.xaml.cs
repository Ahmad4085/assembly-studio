using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using AssemblyStudio.App.Documents;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Shell;

/// <summary>
/// Main window view (UI-001). View concerns only: applying the view model's region sizes and
/// visibility to the grid, reading splitter sizes back, and moving keyboard focus for F6/Shift+F6
/// and the View ▸ Go to commands. The layout rules live in <see cref="ShellViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>F6: next region (ui-spec section 6).</summary>
    public static readonly RoutedUICommand NextPaneCommand = new(Strings.CmdNextPane, nameof(NextPaneCommand), typeof(MainWindow));

    /// <summary>Shift+F6: previous region.</summary>
    public static readonly RoutedUICommand PreviousPaneCommand = new(Strings.CmdPreviousPane, nameof(PreviousPaneCommand), typeof(MainWindow));

    /// <summary>F9 (Debug ▸ Toggle Breakpoint): a breakpoint on the caret line of the active editor (FR-052).</summary>
    public static readonly RoutedUICommand ToggleBreakpointCommand = new(Strings.CmdToggleBreakpoint, nameof(ToggleBreakpointCommand), typeof(MainWindow));

    private readonly ShellViewModel _viewModel;
    private readonly Logging.ILog _log;
    private readonly bool _persistLayout;

    /// <summary>Creates the window over its view model.</summary>
    /// <param name="viewModel">Shell state.</param>
    /// <param name="persistLayout">Save the layout when the window closes.</param>
    public MainWindow(ShellViewModel viewModel, bool persistLayout = true, Logging.ILog? log = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        _log = log ?? Logging.NullLog.Instance;
        _persistLayout = persistLayout;
        DataContext = viewModel;
        InitializeComponent();

        ApplyWindowBounds();
        ApplyLayout();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.PanelActivated += (_, panel) => FocusPanel(panel);
        viewModel.LayoutReset += (_, _) =>
        {
            ApplyWindowBounds();
            ApplyLayout();
        };
        SizeChanged += (_, e) => viewModel.UpdateWindowWidth(e.NewSize.Width);
        Closing += OnClosing;
        PreviewMouseWheel += OnPreviewMouseWheel;

        // TASK-080: F10 and Alt combinations arrive as system keys, which key bindings don't see; route them to the bindings.
        PreviewKeyDown += OnPreviewSystemKey;
        viewModel.BuildRun.Problems.GoToRequested += (_, item) => GoToLine(item.Document!, item.Diagnostic.Line!.Value);
        viewModel.BuildRun.History.GoToLineRequested += (_, line) =>
        {
            if (viewModel.BuildRun.BuiltDocument is { } built)
            {
                GoToLine(built, line, select: true); // UI-013: selects the line (read only)
            }
        };
        viewModel.BuildRun.Breakpoints.GoToRequested += (_, item) => GoToLine(item.Document, item.Breakpoint.Line);
        viewModel.BuildRun.Announced += (_, _) => Announce();

        // TASK-065: the find bar works on the active editor; Go to Line asks through its dialog.
        viewModel.ActiveSearchTarget = ActiveEditor;
        viewModel.Find.AskLine = (lineCount, currentLine) => Editor.Search.GoToLineDialog.Ask(this, lineCount, currentLine);
        viewModel.Find.FocusRequested += (_, replace) =>
        {
            UpdateLayout(); // the bar was just shown
            System.Windows.Controls.TextBox box = replace ? ReplaceBox : FindBox;
            box.Focus();
            box.SelectAll();
        };
        Closed += (_, _) => viewModel.BuildRun.Dispose();
    }

    /// <summary>The command and parameter a key press runs through the window's key bindings, or <see langword="null"/>.</summary>
    internal (ICommand Command, object? Parameter)? ResolveShortcut(Key key, ModifierKeys modifiers)
    {
        foreach (KeyBinding binding in InputBindings.OfType<KeyBinding>())
        {
            if (binding.Key == key && binding.Modifiers == modifiers && binding.Command is { } command)
            {
                return (command, binding.CommandParameter);
            }
        }

        return null;
    }

    /// <summary>Runs a key press's command when it can run (the keyboard path of the key bindings).</summary>
    internal bool TryRunShortcut(Key key, ModifierKeys modifiers)
    {
        if (ResolveShortcut(key, modifiers) is not { } shortcut)
        {
            return false;
        }

        if (shortcut.Command is RoutedCommand routed)
        {
            if (!routed.CanExecute(shortcut.Parameter, this))
            {
                return false;
            }

            routed.Execute(shortcut.Parameter, this);
            return true;
        }

        if (!shortcut.Command.CanExecute(shortcut.Parameter))
        {
            return false;
        }

        shortcut.Command.Execute(shortcut.Parameter);
        return true;
    }

    private void OnPreviewSystemKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey is Key.F10 or Key.Cancel or Key.Pause && TryRunShortcut(e.SystemKey, Keyboard.Modifiers))
        {
            e.Handled = true;
        }
    }

    private void OnToggleBreakpoint(object sender, ExecutedRoutedEventArgs e)
    {
        if (ActiveEditor() is { } editor)
        {
            editor.RequestToggleBreakpoint(editor.TextArea.Caret.Line);
        }
    }

    private void OnCanToggleBreakpoint(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = _viewModel.Workspace.ActiveDocument is not null;

    /// <summary>Activates <paramref name="document"/> and puts the caret at the start of <paramref name="line"/> (Problems, AC-013).</summary>
    internal void GoToLine(DocumentViewModel document, int line, bool select = false)
    {
        _viewModel.Workspace.ActiveDocument = document;
        UpdateLayout();
        if (ActiveEditor() is not { } editor)
        {
            return;
        }

        line = Math.Clamp(line, 1, editor.Document.LineCount);
        editor.TextArea.Caret.Location = new ICSharpCode.AvalonEdit.Document.TextLocation(line, 1);
        if (select)
        {
            ICSharpCode.AvalonEdit.Document.DocumentLine documentLine = editor.Document.GetLineByNumber(line);
            editor.Select(documentLine.Offset, documentLine.Length);
        }

        editor.ScrollToLine(line);
        editor.TextArea.Focus();
    }

    private void OnHistoryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Panels.Session.HistoryRow row })
        {
            _viewModel.BuildRun.History.ActivateCommand.Execute(row);
            e.Handled = true;
        }
    }

    private void OnVariableDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Panels.Memory.VariableRow row })
        {
            _viewModel.BuildRun.Variables.ShowInMemoryCommand.Execute(row);
            e.Handled = true;
        }
    }

    private void OnBreakpointDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Panels.Session.BreakpointItem item })
        {
            _viewModel.BuildRun.Breakpoints.ActivateCommand.Execute(item);
            e.Handled = true;
        }
    }

    private void OnProblemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Panels.Problems.ProblemItem item })
        {
            _viewModel.BuildRun.Problems.ActivateCommand.Execute(item);
            e.Handled = true;
        }
    }

    // FR-093: the announcement region is a polite live region; Narrator reads its text when it is raised.
    private void Announce()
    {
        if (AnnouncementRegion is not null)
        {
            AnnouncementRegion.GetBindingExpression(TextBlock.TextProperty)?.UpdateTarget();
            if ((UIElementAutomationPeer.FromElement(AnnouncementRegion) ?? UIElementAutomationPeer.CreatePeerForElement(AnnouncementRegion)) is { } peer)
            {
                peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        }
    }

    /// <summary>The region that currently holds keyboard (or, in an inactive window, logical)
    /// focus; the editor when focus is outside every region (menu, command bar).</summary>
    internal ShellRegion CurrentRegion
    {
        get
        {
            var focused = FocusManager.GetFocusedElement(this) as DependencyObject;
            if (focused is null)
            {
                return ShellRegion.Editor;
            }

            foreach (ShellRegion region in Enum.GetValues<ShellRegion>())
            {
                FrameworkElement container = RegionContainer(region);
                if (container == focused || container.IsAncestorOf(focused))
                {
                    return region;
                }
            }

            return ShellRegion.Editor;
        }
    }

    /// <summary>Moves focus to the next (or previous) shown region: F6 / Shift+F6.</summary>
    internal void CyclePane(bool backwards) => FocusRegion(_viewModel.NextRegion(CurrentRegion, backwards));

    private void OnNextPane(object sender, ExecutedRoutedEventArgs e) => CyclePane(backwards: false);

    private void OnPreviousPane(object sender, ExecutedRoutedEventArgs e) => CyclePane(backwards: true);

    private void OnExit(object sender, RoutedEventArgs e) => Close();

    /// <summary>Drag-and-drop onto the window opens the files (FR-003).</summary>
    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            _viewModel.Workspace.OpenFiles(files);
            e.Handled = true;
        }
    }

    /// <summary>FR-022: Ctrl+mouse wheel zooms the editor and the code-font panels.</summary>
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        IRelayCommand command = e.Delta > 0 ? _viewModel.ZoomInCommand : _viewModel.ZoomOutCommand;
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }

        e.Handled = true;
    }

    /// <summary>The code editor of the active document, or <see langword="null"/> when none is open.</summary>
    internal Editor.CodeEditor? ActiveEditor()
    {
        DocumentViewModel? active = _viewModel.Workspace.ActiveDocument;
        if (active is null || DocumentEditors.ItemContainerGenerator.ContainerFromItem(active) is not DependencyObject container)
        {
            return null;
        }

        return FindDescendant<Editor.CodeEditor>(container);
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private FrameworkElement RegionContainer(ShellRegion region) => region switch
    {
        ShellRegion.Machine => MachineRegion,
        ShellRegion.Bottom => BottomRegion,
        ShellRegion.Learn => LearnPanel,
        _ => EditorPanel,
    };

    private void FocusRegion(ShellRegion region)
    {
        UIElement target = region switch
        {
            ShellRegion.Machine => RegistersPanel,
            ShellRegion.Bottom => SelectedBottomTab(),
            ShellRegion.Learn => LearnTree,
            _ => EditorFocusTarget(),
        };
        MoveFocus(target);
    }

    /// <summary>The editor region's focus target: the active document's text, else the welcome page's first action.</summary>
    private UIElement EditorFocusTarget()
    {
        UpdateLayout();
        return ActiveEditor() ?? (UIElement)(WelcomeNew.IsVisible ? WelcomeNew : EditorPanel);
    }

    private void FocusPanel(ShellPanel panel)
    {
        UIElement target = panel switch
        {
            ShellPanel.Registers => RegisterList.IsVisible ? RegisterList : RegistersPanel,
            ShellPanel.Flags => FlagList.IsVisible ? FlagList : FlagsPanel,
            ShellPanel.Learn => LearnTree,
            ShellPanel.Memory when _viewModel.IsMemoryStackDocked => MemoryPanel,
            ShellPanel.Stack when _viewModel.IsMemoryStackDocked => StackView,
            ShellPanel.Console => ConsoleOutput,
            _ => SelectedBottomTab(),
        };
        MoveFocus(target);
    }

    /// <summary>Sets the window's logical focus and, when the window is active, keyboard focus.
    /// Logical focus keeps F6 order correct while another window is in front.</summary>
    private void MoveFocus(UIElement target)
    {
        UpdateLayout();
        FocusManager.SetFocusedElement(this, target);
        target.Focus();
    }

    /// <summary>The tree's selection (a section heading or an entry) goes to the Learn view model.</summary>
    private void OnLearnSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => _viewModel.Learn.Select(e.NewValue);

    private UIElement SelectedBottomTab() => BottomTabs.SelectedItem as TabItem ?? (UIElement)BottomTabs;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.IsLearnShown):
            case nameof(ShellViewModel.IsMachineVisible):
            case nameof(ShellViewModel.IsBottomVisible):
            case nameof(ShellViewModel.IsMemoryStackDocked):
                CaptureSizes();
                ApplyLayout();
                MoveFocusOutOfHiddenRegion();
                break;
        }
    }

    /// <summary>Applies the view model's sizes and visibility to the region grid.</summary>
    private void ApplyLayout()
    {
        ShellViewModel vm = _viewModel;
        SetColumn(LearnColumn, LearnPanel, LearnSplitter, vm.IsLearnShown, vm.LearnWidth);
        SetColumn(MachineColumn, MachineRegion, MachineSplitter, vm.IsMachineVisible, vm.MachineWidth);
        SetColumn(MemoryStackColumn, MemoryStackGroup, MemoryStackSplitter, vm.IsMemoryStackDocked, vm.MemoryStackWidth);

        BottomRow.MinHeight = vm.IsBottomVisible ? 120 : 0;
        BottomRow.Height = new GridLength(vm.IsBottomVisible ? vm.BottomHeight : 0);
        BottomRegion.Visibility = vm.IsBottomVisible ? Visibility.Visible : Visibility.Collapsed;
        BottomSplitter.Visibility = BottomRegion.Visibility;

        Visibility tabs = vm.IsMemoryStackDocked ? Visibility.Collapsed : Visibility.Visible;
        MemoryTab.Visibility = tabs;
        StackTab.Visibility = tabs;
    }

    private static void SetColumn(ColumnDefinition column, UIElement content, UIElement splitter, bool shown, double width)
    {
        column.MinWidth = shown ? 120 : 0;
        column.Width = new GridLength(shown ? width : 0);
        content.Visibility = shown ? Visibility.Visible : Visibility.Collapsed;
        splitter.Visibility = content.Visibility;
    }

    /// <summary>Reads splitter-adjusted sizes of the regions still on screen back into the view model.</summary>
    private void CaptureSizes()
    {
        if (LearnPanel.IsVisible && LearnColumn.ActualWidth > 0)
        {
            _viewModel.LearnWidth = LearnColumn.ActualWidth;
        }

        if (MachineRegion.IsVisible && MachineColumn.ActualWidth > 0)
        {
            _viewModel.MachineWidth = MachineColumn.ActualWidth;
        }

        if (BottomRegion.IsVisible && BottomRow.ActualHeight > 0)
        {
            _viewModel.BottomHeight = BottomRow.ActualHeight;
        }

        if (MemoryStackGroup.IsVisible && MemoryStackColumn.ActualWidth > 0)
        {
            _viewModel.MemoryStackWidth = MemoryStackColumn.ActualWidth;
        }
    }

    private void MoveFocusOutOfHiddenRegion()
    {
        if (FocusManager.GetFocusedElement(this) is UIElement { IsVisible: false })
        {
            FocusRegion(ShellRegion.Editor);
        }
    }

    private void ApplyWindowBounds()
    {
        // Fit the saved size to the work area (smaller screens, other monitors, high text scale).
        // On a screen smaller than the 1024 × 768 minimum the minimum gives way to the screen.
        Rect work = SystemParameters.WorkArea;
        MinWidth = Math.Min(LayoutState.MinWindowWidth, work.Width);
        MinHeight = Math.Min(LayoutState.MinWindowHeight, work.Height);
        Width = Math.Min(_viewModel.WindowWidth, work.Width);
        Height = Math.Min(_viewModel.WindowHeight, work.Height);
        if (!IsLoaded)
        {
            Left = work.Left + ((work.Width - Width) / 2);
            Top = work.Top + ((work.Height - Height) / 2);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        WindowState = _viewModel.IsMaximized ? WindowState.Maximized : WindowState.Normal;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // FR-005: unsaved documents are asked about first; Cancel keeps the window open.
        if (!_viewModel.Workspace.ConfirmCloseAll())
        {
            e.Cancel = true;
            return;
        }

        if (!_persistLayout)
        {
            return;
        }

        CaptureSizes();
        Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
        try
        {
            _viewModel.Save(bounds.Width, bounds.Height, WindowState == WindowState.Maximized);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing the layout must never block closing.
            _log.Write(Logging.LogLevel.Warning, "The window layout could not be saved.", ex);
        }
    }
}
