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

/// <summary>One data label of the program and its current value (UI-023); updated in place.</summary>
public sealed partial class VariableRow : ObservableObject
{
    internal VariableRow(DataSymbol symbol, ushort segment)
    {
        Symbol = symbol;
        Address = MemoryAddress.Format(segment, symbol.Offset);
        Value = Strings.VariableNotRead;
    }

    /// <summary>The label, its address and size from the build.</summary>
    public DataSymbol Symbol { get; }

    /// <summary>"lhs".</summary>
    public string Name => Symbol.Name;

    /// <summary>"0800:0130".</summary>
    public string Address { get; }

    /// <summary>"\"1234\"", "04D2h = 1234", "42 43 00": read from the session's memory, never invented.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Value { get; internal set; }

    /// <summary>"text", "byte", "word" or "bytes" for screen readers; empty while there is no value.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Kind { get; internal set; } = string.Empty;

    /// <summary>Differs from the previous pause (FR-056).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsChanged { get; internal set; }

    /// <summary>"lhs at 0800:0130, text 1234, changed".</summary>
    public string AccessibleName
    {
        get
        {
            string text = string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.VariableAccessible), Name, Address, Kind, Value.Trim('"')).Replace(",  ", ", ", StringComparison.Ordinal);
            return IsChanged ? $"{text}, {Strings.CellChanged}" : text;
        }
    }

    internal byte[]? Bytes { get; set; }
}

/// <summary>
/// The Variables panel (UI-023, FR-097): every data label of the build with its address and current value. The values come
/// from memory watches on the session (<see cref="IDebugSession.WatchMemory"/>) covering the labels, so they are the
/// emulated memory's bytes; a label outside the watched regions says so instead of showing a value (NFR-018). Values that
/// differ from the previous pause are marked; opening a row shows its address in the memory view.
/// </summary>
public sealed partial class VariablesViewModel : ObservableObject
{
    /// <summary>The most bytes shown per variable (FR-097).</summary>
    public const int MaxShownBytes = 64;

    /// <summary>The watch regions the panel may use (the session has 4; the memory view uses one).</summary>
    public const int MaxWindows = 2;

    /// <summary>The largest watch region (ADR-0007 v1.1).</summary>
    public const int MaxWindowLength = 4096;

    private readonly CpuViewModel _cpu;
    private readonly MemoryViewModel _memory;
    private readonly ushort _segment;
    private readonly List<(ushort Offset, int Length)> _windows = [];
    private readonly Dictionary<VariableRow, int> _windowOf = [];

    /// <summary>Creates an empty panel.</summary>
    /// <param name="cpu">The registers panel, whose number format the values follow (FR-047).</param>
    /// <param name="memory">The memory view, where a variable is shown on request.</param>
    /// <param name="segment">The program segment.</param>
    public VariablesViewModel(CpuViewModel cpu, MemoryViewModel memory, ushort segment)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        ArgumentNullException.ThrowIfNull(memory);
        _cpu = cpu;
        _memory = memory;
        _segment = segment;
        cpu.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CpuViewModel.Format))
            {
                foreach (VariableRow row in Rows)
                {
                    Describe(row);
                }
            }
        };
    }

    /// <summary>A row was opened: its address is now in the memory view (the shell then shows that view).</summary>
    public event EventHandler<VariableRow>? ShownInMemory;

    /// <summary>The data labels, in address order.</summary>
    public ObservableCollection<VariableRow> Rows { get; } = [];

    /// <summary>The panel's state (as the registers panel's).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValues), nameof(IsNotCurrent), nameof(EmptyText))]
    public partial CpuPanelState State { get; private set; }

    /// <summary>Rows are shown.</summary>
    public bool HasValues => State is CpuPanelState.Current or CpuPanelState.NotCurrent && Rows.Count > 0;

    /// <summary>Running: the last values (FR-057).</summary>
    public bool IsNotCurrent => State == CpuPanelState.NotCurrent;

    /// <summary>The empty-state text.</summary>
    public string EmptyText => State switch
    {
        CpuPanelState.BuildFailed => Strings.EmptyMachineBuildFailed,
        CpuPanelState.Current or CpuPanelState.NotCurrent => Strings.EmptyVariablesNone,
        _ => Strings.EmptyVariables,
    };

    /// <summary>A session was loaded from a build: list its data labels and watch their memory.</summary>
    /// <param name="session">The session.</param>
    /// <param name="symbols">The build's data labels (<see cref="ISourceMap.DataSymbols"/>).</param>
    public void Attach(IDebugSession session, IReadOnlyList<DataSymbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(symbols);
        Clear();
        foreach (DataSymbol symbol in symbols.OrderBy(s => s.Offset))
        {
            var row = new VariableRow(symbol, _segment);
            int shown = Shown(symbol);
            if (_windows.Count > 0 && symbol.Offset + shown - _windows[^1].Offset <= MaxWindowLength && symbol.Offset >= _windows[^1].Offset)
            {
                (ushort start, int length) = _windows[^1];
                _windows[^1] = (start, Math.Max(length, symbol.Offset + shown - start));
                _windowOf[row] = _windows.Count - 1;
            }
            else if (_windows.Count < MaxWindows)
            {
                _windows.Add((symbol.Offset, shown));
                _windowOf[row] = _windows.Count - 1;
            }
            else
            {
                row.Value = Strings.VariableNotWatched; // no region left: said, not guessed
            }

            Rows.Add(row);
        }

        foreach ((ushort offset, int length) in _windows)
        {
            session.WatchMemory(_segment, offset, length);
        }

        OnPropertyChanged(nameof(HasValues));
    }

    /// <summary>The session ended: the empty state (or build failed). Its watches end with it.</summary>
    public void Clear(bool buildFailed = false)
    {
        _windows.Clear();
        _windowOf.Clear();
        Rows.Clear();
        State = buildFailed ? CpuPanelState.BuildFailed : CpuPanelState.Empty;
        OnPropertyChanged(nameof(HasValues));
    }

    /// <summary>Shows a paused snapshot's values.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="newPause">An instruction ran since the last shown pause: changes are counted from that pause. A refresh
    /// (a new watch, the same pause) keeps the comparison.</param>
    /// <param name="baseline">Just loaded or reset: nothing is marked.</param>
    public void Show(MachineSnapshot snapshot, bool newPause, bool baseline)
    {
        foreach (VariableRow row in Rows)
        {
            if (!_windowOf.TryGetValue(row, out int index))
            {
                continue;
            }

            (ushort start, int length) = _windows[index];
            if (MemoryViewModel.FindWindow(snapshot, _segment, start, length) is not { } w)
            {
                continue; // not in this snapshot yet: the row keeps what it showed
            }

            int from = row.Symbol.Offset - start;
            byte[] bytes = [.. w.Bytes.Skip(from).Take(Shown(row.Symbol))];
            byte[]? previous = row.Bytes;
            row.Bytes = bytes;
            if (baseline)
            {
                row.IsChanged = false;
            }
            else if (newPause)
            {
                row.IsChanged = previous is not null && !previous.AsSpan().SequenceEqual(bytes);
            }

            Describe(row);
        }

        State = CpuPanelState.Current;
        OnPropertyChanged(nameof(HasValues));
    }

    /// <summary>The program runs: the last values, not current, nothing marked.</summary>
    public void MarkRunning()
    {
        if (State == CpuPanelState.Current)
        {
            State = CpuPanelState.NotCurrent;
        }

        foreach (VariableRow row in Rows)
        {
            row.IsChanged = false;
        }
    }

    /// <summary>Enter or double-click on a row: its address in the memory view.</summary>
    [RelayCommand]
    private void ShowInMemory(VariableRow? row)
    {
        if (row is null)
        {
            return;
        }

        _memory.ShowAddress(_segment, row.Symbol.Offset);
        ShownInMemory?.Invoke(this, row);
    }

    private static int Shown(DataSymbol symbol) => Math.Min(Math.Min(symbol.Length, MaxShownBytes), 0x10000 - symbol.Offset);

    private void Describe(VariableRow row)
    {
        if (row.Bytes is not { } bytes)
        {
            return;
        }

        (row.Kind, row.Value) = Describe(bytes, row.Symbol.Length, _cpu.Format);
    }

    /// <summary>A variable's value as the panel shows it (FR-097): a 1-byte variable as a byte, a 2-byte one as a word
    /// (little-endian), printable text (trailing zero bytes ignored) in quotes, anything else as hex bytes.</summary>
    /// <param name="bytes">The bytes read (at most <see cref="MaxShownBytes"/>).</param>
    /// <param name="length">The variable's full size.</param>
    /// <param name="format">The number format.</param>
    internal static (string Kind, string Text) Describe(IReadOnlyList<byte> bytes, int length, NumberFormat format)
    {
        if (length == 1 && bytes.Count == 1)
        {
            return (Strings.VariableKindByte, Number(bytes[0], 8, format));
        }

        if (length == 2 && bytes.Count == 2)
        {
            return (Strings.VariableKindWord, Number((ushort)(bytes[0] | (bytes[1] << 8)), 16, format));
        }

        int end = bytes.Count;
        while (end > 0 && bytes[end - 1] == 0)
        {
            end--;
        }

        bool more = length > bytes.Count;
        if (end > 0 && !more && Enumerable.Range(0, end).All(i => bytes[i] is >= 0x20 and < 0x7F))
        {
            var text = new StringBuilder(end + 2).Append('"');
            for (int i = 0; i < end; i++)
            {
                text.Append((char)bytes[i]);
            }

            return (Strings.VariableKindText, text.Append('"').ToString());
        }

        string hex = string.Join(" ", bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        return (Strings.VariableKindBytes, more ? hex + " …" : hex);
    }

    // "04D2h = 1234" in hex, "0000 0100 1101 0010 = 1234" in binary, the decimal value alone in the decimal formats.
    private static string Number(ushort value, int bits, NumberFormat format) => format switch
    {
        NumberFormat.Hexadecimal => $"{ValueFormat.Format(value, bits, format)}h = {value.ToString(CultureInfo.InvariantCulture)}",
        NumberFormat.Binary => $"{ValueFormat.Format(value, bits, format)} = {value.ToString(CultureInfo.InvariantCulture)}",
        _ => ValueFormat.Format(value, bits, format),
    };
}
