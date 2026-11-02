using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AssemblyStudio.App.Panels.Machine;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Panels.Memory;

/// <summary>One byte of the memory view; updated in place (TASK-086: only changed text re-renders).</summary>
public sealed partial class ByteCell : ObservableObject
{
    /// <summary>"34".</summary>
    [ObservableProperty]
    public partial string Text { get; internal set; } = string.Empty;

    /// <summary>Differs from the previous pause (FR-056).</summary>
    [ObservableProperty]
    public partial bool IsChanged { get; internal set; }
}

/// <summary>One row of 16 bytes (HexByteGrid, UI-008); updated in place.</summary>
public sealed partial class MemoryRow : ObservableObject
{
    internal MemoryRow() => Bytes = [.. Enumerable.Range(0, 16).Select(_ => new ByteCell())];

    /// <summary>"0800:0100".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Address { get; internal set; } = string.Empty;

    /// <summary>The 16 bytes.</summary>
    public IReadOnlyList<ByteCell> Bytes { get; }

    /// <summary>The bytes as ASCII, "." for the others.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Ascii { get; internal set; } = string.Empty;

    /// <summary>"0800:0100: B4 09 BA 0D 01 …; changed +0 34" for screen readers.</summary>
    public string AccessibleName
    {
        get
        {
            string text = $"{Address}: {string.Join(" ", Bytes.Select(b => b.Text))}";
            string[] changed = [.. Bytes.Select((b, i) => (b, i)).Where(x => x.b.IsChanged).Select(x => $"+{x.i:X} {x.b.Text}")];
            return changed.Length == 0 ? text : $"{text}; {Strings.CellChanged}: {string.Join(", ", changed)}";
        }
    }

    internal void Refresh() => OnPropertyChanged(nameof(AccessibleName));
}

/// <summary>One word of the stack view (UI-009); updated in place.</summary>
public sealed partial class StackRow : ObservableObject
{
    /// <summary>"FFFC" (the offset in SS).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Offset { get; internal set; } = string.Empty;

    /// <summary>The word in the current number format.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Value { get; internal set; } = string.Empty;

    /// <summary>The word at SS:SP (the SP marker).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsTop { get; internal set; }

    /// <summary>Differs from the previous pause at the same address (FR-056).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsChanged { get; internal set; }

    /// <summary>"SS:FFFC 1234, top of stack (SP), changed".</summary>
    public string AccessibleName =>
        $"SS:{Offset} {Value}{(IsTop ? ", " + Strings.StackTop : string.Empty)}{(IsChanged ? ", " + Strings.CellChanged : string.Empty)}";
}

/// <summary>
/// The memory and stack panels (UI-008, UI-009; FR-054, FR-055, FR-056, NFR-018). The memory view shows 256 bytes at a
/// user-entered address (default: the program image), read through <see cref="IDebugSession.WatchMemory"/>; the stack view
/// shows the snapshot's 16 words from SS:SP. Bytes and words that differ from the previous pause at the same address are
/// marked. Only data the snapshot carries is shown: no row is invented (an address without a window shows nothing).
/// </summary>
public sealed partial class MemoryViewModel : ObservableObject
{
    /// <summary>Bytes in the memory view (16 rows of 16).</summary>
    public const int WindowLength = 256;

    private readonly CpuViewModel _cpu;
    private IDebugSession? _session;
    private int? _watchId;
    private ushort _segment;
    private ushort _offset;
    private byte[]? _shown;
    private byte[]? _previousPause;
    private Dictionary<int, ushort> _shownStack = [];
    private Dictionary<int, ushort> _previousStack = [];
    private StackWindow? _stack;
    private ushort _stackSegment;
    private ushort _stackPointer;
    private CpuState? _lastCpu;
    private IReadOnlyDictionary<int, byte>? _writes;

    /// <summary>Creates empty panels.</summary>
    /// <param name="cpu">The registers panel, whose number format the stack words follow.</param>
    /// <param name="segment">The default segment (the program's).</param>
    /// <param name="offset">The default offset (the program start).</param>
    public MemoryViewModel(CpuViewModel cpu, ushort segment, ushort offset)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        _cpu = cpu;
        _segment = segment;
        _offset = offset;
        AddressText = MemoryAddress.Format(segment, offset);
        cpu.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CpuViewModel.Format))
            {
                ShowStack();
            }
        };
    }

    /// <summary>Ctrl+Alt+N in the memory and stack panels: the number format (shared with the registers panel).</summary>
    public IRelayCommand CycleFormatCommand => _cpu.CycleFormatCommand;

    /// <summary>The memory rows.</summary>
    public ObservableCollection<MemoryRow> Rows { get; } = [];

    /// <summary>The stack rows (SS:SP first).</summary>
    public ObservableCollection<StackRow> StackRows { get; } = [];

    /// <summary>The address field (FR-054).</summary>
    [ObservableProperty]
    public partial string AddressText { get; set; }

    /// <summary>The address shown, resolved ("0800:0100").</summary>
    [ObservableProperty]
    public partial string ShownAddress { get; private set; } = string.Empty;

    /// <summary>Why the address field couldn't be read, or <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? AddressError { get; private set; }

    /// <summary>The panels' state (as the registers panel's).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValues), nameof(IsNotCurrent), nameof(EmptyText))]
    public partial CpuPanelState State { get; private set; }

    /// <summary>Values are shown.</summary>
    public bool HasValues => State is CpuPanelState.Current or CpuPanelState.NotCurrent;

    /// <summary>Running: the last values (FR-057).</summary>
    public bool IsNotCurrent => State == CpuPanelState.NotCurrent;

    /// <summary>The empty-state text.</summary>
    public string EmptyText => State == CpuPanelState.BuildFailed ? Strings.EmptyMachineBuildFailed : Strings.EmptyMachine;

    /// <summary>A session was loaded: watch the current address in it.</summary>
    public void Attach(IDebugSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _watchId = session.WatchMemory(_segment, _offset, WindowLength);
        _shown = null;
        _previousPause = null;
        _shownStack = [];
        _previousStack = [];
    }

    /// <summary>The session ended: the empty state (or build failed).</summary>
    public void Clear(bool buildFailed = false)
    {
        _session = null;
        _watchId = null;
        _shown = null;
        _previousPause = null;
        _shownStack = [];
        _previousStack = [];
        _stack = null;
        Rows.Clear();
        StackRows.Clear();
        State = buildFailed ? CpuPanelState.BuildFailed : CpuPanelState.Empty;
    }

    /// <summary>Shows a paused snapshot's memory window and stack.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="writesSincePause">Every address written since the previous pause with its value at that pause (from the
    /// snapshots' write journals), or <see langword="null"/> when too many were written to track: then the previous pause's
    /// shown values are compared instead.</param>
    /// <param name="newPause">An instruction ran since the last shown pause: the changes are counted from that pause. A
    /// refresh (a new watch, the same pause) keeps the comparison.</param>
    /// <param name="baseline">Just loaded or reset: nothing is marked.</param>
    public void Show(MachineSnapshot snapshot, IReadOnlyDictionary<int, byte>? writesSincePause, bool newPause, bool baseline)
    {
        _lastCpu = snapshot.Cpu;
        if (baseline)
        {
            _previousPause = null;
            _previousStack = [];
            _writes = new Dictionary<int, byte>();
        }
        else if (newPause)
        {
            _previousPause = _shown;
            _previousStack = _shownStack;
            _writes = writesSincePause;
        }

        MemoryWindow? window = snapshot.MemoryWindows?.FirstOrDefault(w => w.Segment == _segment && w.Offset == _offset && w.Bytes.Count > 0);
        if (window is { } w)
        {
            byte[] bytes = [.. w.Bytes];
            _shown = bytes;
            int rows = (bytes.Length + 15) / 16;
            ResizeRows(rows);
            for (int row = 0; row < rows; row++)
            {
                int start = row * 16;
                int count = Math.Min(16, bytes.Length - start);
                MemoryRow target = Rows[row];
                var ascii = new StringBuilder(count);
                for (int i = 0; i < 16; i++)
                {
                    ByteCell cell = target.Bytes[i];
                    if (i >= count)
                    {
                        cell.Text = string.Empty; // past the window: nothing invented
                        cell.IsChanged = false;
                        continue;
                    }

                    byte b = bytes[start + i];
                    cell.Text = HexBytes[b];
                    cell.IsChanged = IsChanged(Linear(_segment, (ushort)(_offset + start + i)), b, _previousPause is { } p && p.Length > start + i ? p[start + i] : null);
                    ascii.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
                }

                target.Address = MemoryAddress.Format(_segment, (ushort)(_offset + start));
                target.Ascii = ascii.ToString();
                target.Refresh();
            }

            ShownAddress = MemoryAddress.Format(_segment, _offset);
        }
        else
        {
            Rows.Clear(); // no window for this address (yet): nothing is shown
        }

        _stack = snapshot.Stack;
        _stackSegment = snapshot.Cpu.SS;
        _stackPointer = snapshot.Cpu.SP;
        _shownStack = [];
        if (_stack is { } s)
        {
            for (int i = 0; i < s.Words.Count; i++)
            {
                _shownStack[Linear(_stackSegment, (ushort)(_stackPointer + (2 * i)))] = s.Words[i];
            }
        }

        ShowStack();
        State = CpuPanelState.Current;
    }

    /// <summary>The program runs: the last values, not current, nothing marked.</summary>
    public void MarkRunning()
    {
        if (State == CpuPanelState.Current)
        {
            State = CpuPanelState.NotCurrent;
        }

        foreach (MemoryRow row in Rows)
        {
            foreach (ByteCell cell in row.Bytes)
            {
                cell.IsChanged = false; // no notification when it already is
            }
        }

        foreach (StackRow row in StackRows)
        {
            row.IsChanged = false;
        }
    }

    /// <summary>Enter in the address field (or Go): watch the typed address; the bytes come with the next snapshot.</summary>
    [RelayCommand]
    private void Go()
    {
        if (!MemoryAddress.TryParse(AddressText, _lastCpu, out ushort segment, out ushort offset))
        {
            AddressError = Strings.MemoryAddressInvalid;
            return;
        }

        AddressError = null;
        if (segment == _segment && offset == _offset && _watchId is not null)
        {
            return;
        }

        _segment = segment;
        _offset = offset;
        _shown = null;
        _previousPause = null; // a new address has nothing to compare with
        Rows.Clear();
        if (_session is { } session)
        {
            if (_watchId is int id)
            {
                session.UnwatchMemory(id);
            }

            _watchId = session.WatchMemory(segment, offset, WindowLength);
        }
    }

    private void ShowStack()
    {
        if (_stack is not { } s)
        {
            StackRows.Clear();
            return;
        }

        while (StackRows.Count > s.Words.Count)
        {
            StackRows.RemoveAt(StackRows.Count - 1);
        }

        while (StackRows.Count < s.Words.Count)
        {
            StackRows.Add(new StackRow());
        }

        for (int i = 0; i < s.Words.Count; i++)
        {
            var offset = (ushort)(_stackPointer + (2 * i));
            ushort value = s.Words[i];
            int low = Linear(_stackSegment, offset);
            int high = Linear(_stackSegment, (ushort)(offset + 1));
            bool changed = _writes is not null
                ? IsChanged(low, (byte)value, null) || IsChanged(high, (byte)(value >> 8), null)
                : _previousStack.TryGetValue(low, out ushort before) && before != value;
            StackRow row = StackRows[i];
            row.Offset = offset.ToString("X4", CultureInfo.InvariantCulture);
            row.Value = ValueFormat.Format(value, 16, _cpu.Format);
            row.IsTop = i == 0;
            row.IsChanged = changed;
        }
    }

    // Changed since the previous pause: from the write journal when it is complete, else against the previous pause's value.
    private bool IsChanged(int physical, byte value, byte? previous) => _writes is { } writes
        ? writes.TryGetValue(physical, out byte atPause) && atPause != value
        : previous is byte p && p != value;

    private static int Linear(ushort segment, ushort offset) => ((segment << 4) + offset) & 0xFFFFF;

    private static readonly string[] HexBytes = [.. Enumerable.Range(0, 256).Select(b => b.ToString("X2", CultureInfo.InvariantCulture))];

    private void ResizeRows(int count)
    {
        while (Rows.Count > count)
        {
            Rows.RemoveAt(Rows.Count - 1);
        }

        while (Rows.Count < count)
        {
            Rows.Add(new MemoryRow());
        }
    }
}
