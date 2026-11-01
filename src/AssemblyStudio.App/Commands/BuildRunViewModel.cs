using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Threading;
using AssemblyStudio.App.Documents;
using AssemblyStudio.App.Editor.Adornments;
using AssemblyStudio.App.Panels.Problems;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Commands;

/// <summary>The states of ui-spec §4: document states plus the session states.</summary>
public enum RunState
{
    /// <summary>No document open.</summary>
    Empty,

    /// <summary>A document is open with no current build (never built, edited since, or the session stopped).</summary>
    Editing,

    /// <summary>A build is running (cancellable).</summary>
    Assembling,

    /// <summary>The last build failed: no image, no map.</summary>
    BuildFailed,

    /// <summary>Built and loaded, not started.</summary>
    Ready,

    /// <summary>Executing freely.</summary>
    Running,

    /// <summary>Stopped at a step, breakpoint, Pause or the instruction budget.</summary>
    Paused,

    /// <summary>One Step Into or Step Over is executing; commands wait for its snapshot.</summary>
    Stepping,

    /// <summary>The program waits for keyboard input.</summary>
    WaitingForInput,

    /// <summary>A runtime error, HLT or an engine fault stopped the program.</summary>
    HaltedError,

    /// <summary>An unsupported instruction or DOS/BIOS service stopped the program.</summary>
    HaltedUnsupported,

    /// <summary>The program ended normally.</summary>
    Terminated,
}

/// <summary>
/// The build and run commands (UI-002, TASK-080; FR-028, FR-035, FR-049, FR-050, FR-051, FR-067): Build and Cancel build,
/// Run/Continue (building first when needed), Pause, Stop, Reset, Step Into, Step Over and Ctrl-C to the program. Owns the
/// debug session of the built document and turns its snapshots into the state badge, the status bar text, the problems,
/// the editor's diagnostics, breakpoint binding and execution line. Commands never wait for execution: the session runs
/// on its worker thread and its snapshots are applied on the UI thread (ADR-0007 §3).
/// </summary>
public sealed partial class BuildRunViewModel : ObservableObject, IDisposable
{
    private readonly WorkspaceViewModel _workspace;
    private readonly Func<IAssembler> _assembler;
    private readonly Func<BuildResult, IDebugSession> _createSession;
    private readonly Dispatcher _dispatcher;
    private readonly StringBuilder _console = new();
    private CancellationTokenSource? _buildCancel;
    private IDebugSession? _session;
    private MachineSnapshot? _pendingSnapshot;
    private bool _snapshotQueued;
    private bool _syncingBreakpoints;
    private string? _builtText;

    /// <summary>Creates the commands over <paramref name="workspace"/>.</summary>
    /// <param name="workspace">The open documents; the active one is built and run.</param>
    /// <param name="assembler">The assembler for the next build (Settings may change it between builds).</param>
    /// <param name="createSession">Creates the debug session for a successful build.</param>
    /// <param name="dispatcher">The UI thread's dispatcher; by default the current thread's.</param>
    public BuildRunViewModel(WorkspaceViewModel workspace, Func<IAssembler> assembler, Func<BuildResult, IDebugSession> createSession, Dispatcher? dispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(assembler);
        ArgumentNullException.ThrowIfNull(createSession);
        _workspace = workspace;
        _assembler = assembler;
        _createSession = createSession;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
        _workspace.PropertyChanged += OnWorkspaceChanged;
        Watch(_workspace.ActiveDocument);
        State = _workspace.ActiveDocument is null ? RunState.Empty : RunState.Editing;
    }

    /// <summary>The Problems panel.</summary>
    public ProblemsViewModel Problems { get; } = new();

    /// <summary>The current state.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText), nameof(StatusText), nameof(HasSession))]
    public partial RunState State { get; private set; }

    /// <summary>The last build, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText), nameof(StatusText))]
    public partial BuildResult? LastBuild { get; private set; }

    /// <summary>The document the session runs (the last built one), or <see langword="null"/>.</summary>
    public DocumentViewModel? BuiltDocument { get; private set; }

    /// <summary>Instructions executed since load or reset, as of the latest snapshot.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial ulong InstructionCount { get; private set; }

    /// <summary>The source line of the current instruction while paused, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText), nameof(StatusText))]
    public partial int? CurrentLine { get; private set; }

    /// <summary>Why the session halted or ended, in words (status bar detail), or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial string? Detail { get; private set; }

    /// <summary>The program's exit code once it terminated, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText), nameof(StatusText))]
    public partial int? ExitCode { get; private set; }

    /// <summary>The console output since load or reset (shown by the console panel, TASK-084).</summary>
    public string ConsoleText => _console.ToString();

    /// <summary>Whether a session exists (Stop and Reset are available).</summary>
    public bool HasSession => _session is not null;

    /// <summary>The state badge (UI-002): a glyph and words, announced when it changes (live region).</summary>
    public string BadgeText => State switch
    {
        RunState.Empty => string.Empty,
        RunState.Editing => Strings.StateEditing,
        RunState.Assembling => "⟳ " + Strings.StateAssembling,
        RunState.BuildFailed => "⨯ " + Strings.StateBuildFailed,
        RunState.Ready => "● " + Strings.StateReady,
        RunState.Running => "▶ " + Strings.StateRunning,
        RunState.Paused => "❚❚ " + (CurrentLine is int line ? Format(Strings.StatePausedLine, line) : Strings.StatePaused),
        RunState.Stepping => "▶ " + Strings.StateStepping,
        RunState.WaitingForInput => "⌨ " + Strings.StateWaiting,
        RunState.HaltedError => "⨯ " + Strings.StateHaltedError,
        RunState.HaltedUnsupported => "⨯ " + Strings.StateHaltedUnsupported,
        _ => "■ " + (ExitCode is int code ? Format(Strings.StateEndedCode, code) : Strings.StateEnded),
    };

    /// <summary>The status bar's build and session text (UI-016).</summary>
    public string StatusText => State switch
    {
        RunState.Empty => string.Empty,
        RunState.Editing => LastBuild is null ? string.Empty : Strings.StatusBuildOutdated,
        RunState.Assembling => Strings.StatusBuilding,
        RunState.BuildFailed => BuildFailedText(),
        RunState.Ready => BuildOkText() + " · " + Strings.StateReady,
        RunState.Running => Format(Strings.StatusRunning, Instructions(InstructionCount)),
        RunState.Paused or RunState.Stepping => CurrentLine is int line
            ? Format(Strings.StatusPausedLine, line, Instructions(InstructionCount))
            : Format(Strings.StatusPaused, Instructions(InstructionCount)),
        RunState.WaitingForInput => Strings.StateWaiting,
        RunState.HaltedError or RunState.HaltedUnsupported => Detail ?? BadgeText,
        _ => (ExitCode is int code ? Format(Strings.StateEndedCode, code) : Strings.StateEnded) + " · " + Instructions(InstructionCount),
    };

    /// <summary>The assembler of the last build, for the status bar ("NASM 3.02").</summary>
    public AssemblerInfo? Assembler => LastBuild?.Assembler;

    /// <summary>"NASM 3.02", or the version and the problem with a ▲ when the assembler is older, unknown or unusable (UI-016, FR-024).</summary>
    public string AssemblerText => Assembler switch
    {
        null => string.Empty,
        { Status: AssemblerStatus.Ready } a => Format(Strings.StatusNasm, a.Version),
        var a => Format(Strings.StatusNasmWarning, a.Version ?? "?", a.Problem ?? a.Status.ToString()),
    };

    /// <summary>Ctrl+Shift+B: builds the active document and loads it (Ready).</summary>
    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync() => await BuildActiveAsync().ConfigureAwait(true);

    /// <summary>Esc while building: cancels the build; the assembler process is killed.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelBuild))]
    private void CancelBuild() => _buildCancel?.Cancel();

    /// <summary>F5: Run or Continue. Builds first when there is no current build; restarts an ended program.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (NeedsBuild() && !await BuildActiveAsync().ConfigureAwait(true))
        {
            return;
        }

        if (_session is null)
        {
            return;
        }

        if (State == RunState.Terminated)
        {
            ResetConsole();
            _session.Reset();
        }

        State = RunState.Running;
        UpdateExecutionMarker();
        _session.Run();
    }

    /// <summary>Ctrl+Alt+Break: pauses a run.</summary>
    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => _session?.Pause();

    /// <summary>Shift+F5: ends the session.</summary>
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        EndSession();
        State = _workspace.ActiveDocument is null ? RunState.Empty : RunState.Editing;
    }

    /// <summary>Ctrl+Shift+F5: reloads the program (initial registers, cleared console); rebuilds first if the source changed.</summary>
    [RelayCommand(CanExecute = nameof(CanReset))]
    private async Task ResetAsync()
    {
        if (BuiltDocument is { } document && !string.Equals(document.Text, _builtText, StringComparison.Ordinal))
        {
            await BuildDocumentAsync(document).ConfigureAwait(true);
            return;
        }

        ResetConsole();
        State = RunState.Stepping; // until the reloaded snapshot arrives
        _session?.Reset();
    }

    /// <summary>F11: executes one instruction.</summary>
    [RelayCommand(CanExecute = nameof(CanStep))]
    private void StepInto()
    {
        State = RunState.Stepping;
        _session?.StepInto();
    }

    /// <summary>F10: executes a CALL or INT and runs until it returns, or one instruction.</summary>
    [RelayCommand(CanExecute = nameof(CanStep))]
    private void StepOver()
    {
        State = RunState.Stepping;
        _session?.StepOver();
    }

    /// <summary>Ctrl+Break: delivers Ctrl-C to the running program (D-015).</summary>
    [RelayCommand(CanExecute = nameof(CanPause))]
    private void SendCtrlC() => _session?.SendCtrlC();

    /// <summary>Starts building the active document and returns whether it succeeded and loaded.</summary>
    public Task<bool> BuildActiveAsync() =>
        _workspace.ActiveDocument is { } document ? BuildDocumentAsync(document) : Task.FromResult(false);

    /// <inheritdoc />
    public void Dispose()
    {
        _workspace.PropertyChanged -= OnWorkspaceChanged;
        EndSession();
        _buildCancel?.Dispose();
        _buildCancel = null;
    }

    private async Task<bool> BuildDocumentAsync(DocumentViewModel document)
    {
        EndSession();
        string text = document.Text;
        var request = new BuildRequest(document.FilePath, document.IsDirty || document.FilePath is null ? text : null);
        using var cancel = new CancellationTokenSource();
        _buildCancel = cancel;
        Problems.BeginBuild();
        State = RunState.Assembling;
        BuildResult result;
        try
        {
            IAssembler assembler = _assembler();
            result = await Task.Run(() => assembler.BuildAsync(request, cancel.Token), CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            _buildCancel = null;
        }

        LastBuild = result;
        OnPropertyChanged(nameof(Assembler));
        OnPropertyChanged(nameof(AssemblerText));
        BuiltDocument = document;
        _builtText = text;
        Problems.Show(result, document);
        document.Diagnostics = [.. result.Diagnostics.Where(d => ProblemsViewModel.IsAbout(d, document) && d.Line is > 0)];
        if (result.Outcome != BuildOutcome.Succeeded)
        {
            State = RunState.BuildFailed;
            return false;
        }

        StartSession(result, document);
        return true;
    }

    private void StartSession(BuildResult build, DocumentViewModel document)
    {
        ResetConsole();
        IDebugSession session = _createSession(build);
        _session = session;
        foreach (EditorBreakpoint breakpoint in document.Breakpoints)
        {
            session.SetBreakpoint(document.FilePath, breakpoint.Line);
        }

        ShowBreakpointBinding(document);
        document.HasSession = true;
        State = RunState.Stepping; // until the loaded snapshot arrives (Ready)
        session.SnapshotPublished += OnSnapshotPublished;
        if (session.LatestSnapshot is { } first)
        {
            OnSnapshotPublished(session, first); // published before the handler was attached
        }
    }

    private void EndSession()
    {
        if (_session is { } session)
        {
            _session = null;
            session.SnapshotPublished -= OnSnapshotPublished;
            session.Dispose();
        }

        _pendingSnapshot = null;
        if (BuiltDocument is { } document)
        {
            document.HasSession = false;
            document.ExecutionLine = null;
            document.Breakpoints = [.. document.Breakpoints.Select(b => new EditorBreakpoint(b.Line))];
        }

        CurrentLine = null;
        ExitCode = null;
        Detail = null;
        OnPropertyChanged(nameof(HasSession));
    }

    // Worker thread: keep only the latest snapshot and apply it on the UI thread (at most one queued at a time).
    private void OnSnapshotPublished(object? sender, MachineSnapshot snapshot)
    {
        lock (_console)
        {
            _pendingSnapshot = snapshot;
            if (!string.IsNullOrEmpty(snapshot.ConsoleDelta))
            {
                _console.Append(snapshot.ConsoleDelta);
            }

            if (_snapshotQueued)
            {
                return;
            }

            _snapshotQueued = true;
        }

        _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ApplyPendingSnapshot));
    }

    private void ApplyPendingSnapshot()
    {
        MachineSnapshot? snapshot;
        lock (_console)
        {
            snapshot = _pendingSnapshot;
            _pendingSnapshot = null;
            _snapshotQueued = false;
        }

        OnPropertyChanged(nameof(ConsoleText));
        if (snapshot is not { } s || _session is not { } session)
        {
            return;
        }

        InstructionCount = session.InstructionsExecuted;
        CurrentLine = s.State is SessionState.Running ? null : s.CurrentInstruction?.SourceLine;
        ExitCode = s.ExitCode;
        (State, Detail) = s.State switch
        {
            SessionState.Running => (RunState.Running, null),
            SessionState.Paused when session.PauseReason == PauseReason.Loaded => (RunState.Ready, null),
            SessionState.Paused => (RunState.Paused, null),
            SessionState.WaitingForInput => (RunState.WaitingForInput, null),
            SessionState.Halted => HaltState(session.LastStep?.HaltReason ?? HaltReason.None),
            SessionState.Terminated => (RunState.Terminated, null),
            SessionState.Error => (RunState.HaltedError, Format(Strings.StatusSessionError, session.LastFault ?? string.Empty)),
            _ => (_workspace.ActiveDocument is null ? RunState.Empty : RunState.Editing, (string?)null),
        };
        UpdateExecutionMarker(s);
    }

    private static (RunState, string?) HaltState(HaltReason reason) => reason switch
    {
        HaltReason.UnsupportedInstruction => (RunState.HaltedUnsupported, Strings.StatusHaltUnsupportedInstruction),
        HaltReason.UnsupportedService => (RunState.HaltedUnsupported, Strings.StatusHaltUnsupportedService),
        HaltReason.RuntimeError => (RunState.HaltedError, Strings.StatusHaltRuntimeError),
        HaltReason.Halt => (RunState.HaltedError, Strings.StatusHaltHlt),
        _ => (RunState.HaltedError, null),
    };

    private void UpdateExecutionMarker(MachineSnapshot? snapshot = null)
    {
        if (BuiltDocument is not { } document)
        {
            return;
        }

        CurrentInstruction? current = snapshot?.CurrentInstruction;
        bool sameFile = current is { } c && (c.SourcePath is null ? document.FilePath is null
            : document.FilePath is { } path && string.Equals(Path.GetFullPath(c.SourcePath), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
        document.ExecutionLine = State is RunState.Running or RunState.Stepping || !sameFile ? null : current!.Value.SourceLine;
        document.ExecutionMarker = State switch
        {
            RunState.HaltedError or RunState.HaltedUnsupported => ExecutionMarkerKind.Fault,
            RunState.Terminated => ExecutionMarkerKind.Ended,
            _ => ExecutionMarkerKind.Current,
        };
    }

    // The editor's breakpoints become the session's (added or removed lines), then show the session's binding.
    private void OnDocumentChanged(object? sender, PropertyChangedEventArgs e)
    {
        var document = (DocumentViewModel)sender!;
        if (e.PropertyName == nameof(DocumentViewModel.Text))
        {
            if (document == BuiltDocument && _builtText is not null)
            {
                Problems.IsOutdated = !string.Equals(document.Text, _builtText, StringComparison.Ordinal);
                if (_session is null && State is RunState.BuildFailed)
                {
                    State = RunState.Editing;
                }
            }

            NotifyCommands();
        }
        else if (e.PropertyName == nameof(DocumentViewModel.Breakpoints) && !_syncingBreakpoints && document == BuiltDocument && _session is { } session)
        {
            var wanted = document.Breakpoints.Select(b => b.Line).ToHashSet();
            var current = session.Breakpoints.Where(b => SameFile(b.Location.File, document.FilePath)).Select(b => b.Location.Line).ToHashSet();
            foreach (int line in wanted.Except(current))
            {
                session.SetBreakpoint(document.FilePath, line);
            }

            foreach (int line in current.Except(wanted))
            {
                session.RemoveBreakpoint(document.FilePath, line);
            }

            ShowBreakpointBinding(document);
        }
    }

    private void ShowBreakpointBinding(DocumentViewModel document)
    {
        if (_session is not { } session)
        {
            return;
        }

        var bound = session.Breakpoints.Where(b => SameFile(b.Location.File, document.FilePath)).ToDictionary(b => b.Location.Line);
        _syncingBreakpoints = true;
        try
        {
            document.Breakpoints = [.. document.Breakpoints.Select(b => bound.TryGetValue(b.Line, out Breakpoint s)
                ? s.IsBound && s.Offset is ushort offset
                    ? new EditorBreakpoint(b.Line, BreakpointBinding.Bound, $"{SessionFactory.ProgramSegment:X4}:{offset:X4}")
                    : new EditorBreakpoint(b.Line, BreakpointBinding.Unbound)
                : new EditorBreakpoint(b.Line))];
        }
        finally
        {
            _syncingBreakpoints = false;
        }
    }

    private static bool SameFile(string? a, string? b) =>
        a is null || b is null ? a is null && b is null : string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private void OnWorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceViewModel.ActiveDocument))
        {
            return;
        }

        Watch(_workspace.ActiveDocument);
        if (_workspace.ActiveDocument is null && _session is null && State is not RunState.Assembling)
        {
            State = RunState.Empty;
        }
        else if (State == RunState.Empty)
        {
            State = RunState.Editing;
        }

        NotifyCommands();
    }

    private readonly HashSet<DocumentViewModel> _watched = [];

    private void Watch(DocumentViewModel? document)
    {
        if (document is not null && _watched.Add(document))
        {
            document.PropertyChanged += OnDocumentChanged;
        }
    }

    private bool NeedsBuild() =>
        _session is null || BuiltDocument != _workspace.ActiveDocument || !string.Equals(BuiltDocument?.Text, _builtText, StringComparison.Ordinal);

    private void ResetConsole()
    {
        lock (_console)
        {
            _console.Clear();
        }

        OnPropertyChanged(nameof(ConsoleText));
    }

    partial void OnStateChanged(RunState value) => NotifyCommands();

    private void NotifyCommands()
    {
        BuildCommand.NotifyCanExecuteChanged();
        CancelBuildCommand.NotifyCanExecuteChanged();
        RunCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        StepIntoCommand.NotifyCanExecuteChanged();
        StepOverCommand.NotifyCanExecuteChanged();
        SendCtrlCCommand.NotifyCanExecuteChanged();
    }

    private bool IsBusy => State is RunState.Assembling or RunState.Running or RunState.WaitingForInput or RunState.Stepping;

    private bool CanBuild() => _workspace.ActiveDocument is not null && !IsBusy;

    private bool CanCancelBuild() => State == RunState.Assembling;

    // AC-013: after a failed build Run stays disabled until the text changes (the state returns to Editing; Run then
    // rebuilds). The ui-spec §4 matrix lists Run for Build failed; the approved acceptance criterion decides.
    private bool CanRun() => _workspace.ActiveDocument is not null && !IsBusy
        && State is not (RunState.HaltedError or RunState.HaltedUnsupported or RunState.BuildFailed);

    private bool CanPause() => State is RunState.Running or RunState.WaitingForInput;

    private bool CanStop() => _session is not null && State != RunState.Assembling;

    private bool CanReset() => _session is not null && State is not (RunState.Assembling or RunState.Stepping);

    private bool CanStep() => _session is not null && State is RunState.Ready or RunState.Paused;

    private string BuildOkText() => LastBuild is { } b
        ? Format(Strings.StatusBuildOk, b.Image.Length, Count(b.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning), Strings.CountWarning, Strings.CountWarnings), (long)b.Duration.TotalMilliseconds)
        : string.Empty;

    private string BuildFailedText() => LastBuild is { } b
        ? b.Outcome switch
        {
            BuildOutcome.Cancelled => Strings.StatusBuildCancelled,
            BuildOutcome.AssemblerMissing => Strings.StatusAssemblerMissing,
            BuildOutcome.TimedOut => Strings.StatusBuildTimedOut,
            BuildOutcome.Crashed => Strings.StatusBuildCrashed,
            _ => Format(Strings.StatusBuildFailed, Count(b.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error), Strings.CountError, Strings.CountErrors)),
        }
        : Strings.StateBuildFailed;

    private static string Instructions(ulong count) => Count(count, Strings.CountInstruction, Strings.CountInstructions);

    private static string Count(ulong count, string one, string many) => Format(count == 1 ? one : many, count);

    private static string Count(int count, string one, string many) => Count((ulong)count, one, many);

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(format), args);
}
