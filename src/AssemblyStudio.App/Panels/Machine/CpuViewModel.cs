using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AssemblyStudio.App.Settings;
using AssemblyStudio.App.Shell;
using AssemblyStudio.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssemblyStudio.App.Panels.Machine;

/// <summary>What the machine panels show (ui-spec §4 state matrix).</summary>
public enum CpuPanelState
{
    /// <summary>No program loaded: "Build and run a program to see the CPU".</summary>
    Empty,

    /// <summary>The last build failed: "Fix build errors to inspect the CPU".</summary>
    BuildFailed,

    /// <summary>The values of the latest paused snapshot.</summary>
    Current,

    /// <summary>Running: the last values, dimmed, "Running — values not current" (FR-057).</summary>
    NotCurrent,
}

/// <summary>A register cell (ValueCell, UI-005).</summary>
public sealed partial class RegisterCell : ObservableObject
{
    internal RegisterCell(string name, int bits, Func<CpuState, ushort> read)
    {
        Name = name;
        Bits = bits;
        Read = read;
    }

    /// <summary>"AX", "AL", "FLAGS".</summary>
    public string Name { get; }

    /// <summary>8 or 16.</summary>
    public int Bits { get; }

    internal Func<CpuState, ushort> Read { get; }

    /// <summary>The value from the snapshot.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial ushort Value { get; internal set; }

    /// <summary>The value in the current format.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Text { get; internal set; } = string.Empty;

    /// <summary>The value differs from the previous pause (FR-056).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsChanged { get; internal set; }

    /// <summary>"AX 7F80, changed": the name, the value and the change state for screen readers.</summary>
    public string AccessibleName => IsChanged ? $"{Name} {Text}, {Strings.CellChanged}" : $"{Name} {Text}";
}

/// <summary>
/// One row of the registers panel (UI-005, D-017): a 16-bit register and, for AX–DX, its 8-bit halves on the same row
/// ("AX 000A  AH 00  AL 0A").
/// </summary>
public sealed partial class RegisterRow : ObservableObject
{
    internal RegisterRow(RegisterCell main, RegisterCell? high = null, RegisterCell? low = null)
    {
        Main = main;
        High = high;
        Low = low;
        foreach (RegisterCell? cell in new[] { main, high, low })
        {
            if (cell is not null)
            {
                cell.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(RegisterCell.AccessibleName))
                    {
                        OnPropertyChanged(nameof(AccessibleName));
                    }
                };
            }
        }
    }

    /// <summary>The 16-bit register.</summary>
    public RegisterCell Main { get; }

    /// <summary>The high half (AH … DH), or <see langword="null"/>.</summary>
    public RegisterCell? High { get; }

    /// <summary>The low half (AL … DL), or <see langword="null"/>.</summary>
    public RegisterCell? Low { get; }

    /// <summary>Whether the row shows halves.</summary>
    public bool HasHalves => High is not null;

    /// <summary>"AX 000A, changed; AH 00; AL 0A, changed" for screen readers.</summary>
    public string AccessibleName => string.Join("; ", new[] { Main, High, Low }.OfType<RegisterCell>().Select(c => c.AccessibleName));
}

/// <summary>A flag cell (FlagCell, UI-006).</summary>
public sealed partial class FlagCell : ObservableObject
{
    internal FlagCell(FlagBit bit, string letter, string name, string description)
    {
        Bit = bit;
        Letter = letter;
        Name = name;
        Description = description;
    }

    /// <summary>The flag's bit in FLAGS.</summary>
    public FlagBit Bit { get; }

    /// <summary>"O".</summary>
    public string Letter { get; }

    /// <summary>"Overflow flag (OF)".</summary>
    public string Name { get; }

    /// <summary>What the flag means.</summary>
    public string Description { get; }

    /// <summary>"0", "1" or "?" when the last instruction left it undefined (FR-048).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial string Text { get; internal set; } = string.Empty;

    /// <summary>The last instruction left the flag undefined.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tooltip), nameof(AccessibleName))]
    public partial bool IsUndefined { get; internal set; }

    /// <summary>The value (or its undefined state) differs from the previous pause (FR-056).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsChanged { get; internal set; }

    /// <summary>The instruction after which the flag is undefined, for the tooltip.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tooltip))]
    public partial string? UndefinedAfter { get; internal set; }

    /// <summary>Full name and meaning, plus "Undefined after `add al, bl`" when undefined.</summary>
    public string Tooltip => IsUndefined
        ? $"{Name}: {Description}{Environment.NewLine}{string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(Strings.FlagUndefinedAfter), UndefinedAfter ?? Strings.FlagUndefinedLastInstruction)}"
        : $"{Name}: {Description}";

    /// <summary>"Overflow flag (OF) 1, changed" or "… undefined".</summary>
    public string AccessibleName =>
        $"{Name} {(IsUndefined ? Strings.FlagUndefined : Text)}{(IsChanged ? ", " + Strings.CellChanged : string.Empty)}";
}

/// <summary>
/// The registers and flags panels (UI-005, UI-006; FR-046, FR-047, FR-048, FR-056, FR-057, NFR-018): every value comes from
/// the latest paused snapshot, in the chosen format, with the values that changed since the previous pause marked. While
/// the program runs the last values stay, marked not current. No value is ever made up: before a program is loaded the
/// panels show their empty state.
/// </summary>
public sealed partial class CpuViewModel : ObservableObject
{
    private CpuState? _previous;

    /// <summary>Creates empty panels.</summary>
    /// <param name="format">The number format (Settings, FR-047).</param>
    public CpuViewModel(NumberFormat format = NumberFormat.Hexadecimal)
    {
        Format = format;
        Registers =
        [
            new("AX", 16, c => c.AX), new("BX", 16, c => c.BX), new("CX", 16, c => c.CX), new("DX", 16, c => c.DX),
            new("SI", 16, c => c.SI), new("DI", 16, c => c.DI), new("BP", 16, c => c.BP), new("SP", 16, c => c.SP),
            new("CS", 16, c => c.CS), new("DS", 16, c => c.DS), new("ES", 16, c => c.ES), new("SS", 16, c => c.SS),
            new("IP", 16, c => c.IP), new("FLAGS", 16, c => c.Flags),
        ];
        Halves =
        [
            new("AH", 8, c => (ushort)(c.AX >> 8)), new("AL", 8, c => (ushort)(c.AX & 0xFF)),
            new("BH", 8, c => (ushort)(c.BX >> 8)), new("BL", 8, c => (ushort)(c.BX & 0xFF)),
            new("CH", 8, c => (ushort)(c.CX >> 8)), new("CL", 8, c => (ushort)(c.CX & 0xFF)),
            new("DH", 8, c => (ushort)(c.DX >> 8)), new("DL", 8, c => (ushort)(c.DX & 0xFF)),
        ];
        Flags =
        [
            new(FlagBit.OF, "O", Strings.FlagOF, Strings.FlagOFText), new(FlagBit.DF, "D", Strings.FlagDF, Strings.FlagDFText),
            new(FlagBit.IF, "I", Strings.FlagIF, Strings.FlagIFText), new(FlagBit.TF, "T", Strings.FlagTF, Strings.FlagTFText),
            new(FlagBit.SF, "S", Strings.FlagSF, Strings.FlagSFText), new(FlagBit.ZF, "Z", Strings.FlagZF, Strings.FlagZFText),
            new(FlagBit.AF, "A", Strings.FlagAF, Strings.FlagAFText), new(FlagBit.PF, "P", Strings.FlagPF, Strings.FlagPFText),
            new(FlagBit.CF, "C", Strings.FlagCF, Strings.FlagCFText),
        ];
        Rows = [.. Registers.Select(r => r.Bits == 16 && r.Name is "AX" or "BX" or "CX" or "DX"
            ? new RegisterRow(r, Halves.Single(h => h.Name == $"{r.Name[0]}H"), Halves.Single(h => h.Name == $"{r.Name[0]}L"))
            : new RegisterRow(r))];
    }

    /// <summary>The panel's rows: AX–DX with their halves inline, then SI … FLAGS (D-017).</summary>
    public IReadOnlyList<RegisterRow> Rows { get; }

    /// <summary>AX … FLAGS (FR-046).</summary>
    public ObservableCollection<RegisterCell> Registers { get; }

    /// <summary>AH … DL (shown on the rows of AX … DX).</summary>
    public ObservableCollection<RegisterCell> Halves { get; }

    /// <summary>O D I T S Z A P C (FR-048).</summary>
    public ObservableCollection<FlagCell> Flags { get; }

    /// <summary>The panels' state.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValues), nameof(IsNotCurrent), nameof(EmptyText))]
    public partial CpuPanelState State { get; private set; }

    /// <summary>The number format of the values (FR-047).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatIndex))]
    public partial NumberFormat Format { get; set; }

    /// <summary>The format as an index for the format list (hex, unsigned, signed, binary).</summary>
    public int FormatIndex
    {
        get => (int)Format;
        set => Format = (NumberFormat)value;
    }

    /// <summary>Values are shown (current or not current).</summary>
    public bool HasValues => State is CpuPanelState.Current or CpuPanelState.NotCurrent;

    /// <summary>Running: the values are the last paused ones (FR-057).</summary>
    public bool IsNotCurrent => State == CpuPanelState.NotCurrent;

    /// <summary>The empty-state text.</summary>
    public string EmptyText => State == CpuPanelState.BuildFailed ? Strings.EmptyMachineBuildFailed : Strings.EmptyMachine;

    /// <summary>The names of the registers and flags marked changed, for the step announcement (TASK-085).</summary>
    public IReadOnlyList<string> ChangedNames =>
        [.. Registers.Concat(Halves).Where(r => r.IsChanged).Select(r => r.Name), .. Flags.Where(f => f.IsChanged).Select(f => f.Bit.ToString())];

    /// <summary>Ctrl+Alt+N (View ▸ Cycle Number Format): the next format.</summary>
    [RelayCommand]
    private void CycleFormat() => Format = ValueFormat.Next(Format);

    /// <summary>Shows a paused snapshot's registers and flags.</summary>
    /// <param name="cpu">The CPU state of the snapshot.</param>
    /// <param name="undefined">The flags the last instruction left undefined.</param>
    /// <param name="executed">The last executed instruction's text, for the undefined-flag tooltip.</param>
    /// <param name="baseline">The program was just loaded or reset: nothing is marked changed.</param>
    public void Show(CpuState cpu, FlagBit undefined, string? executed, bool baseline)
    {
        CpuState? previous = baseline ? null : _previous;
        foreach (RegisterCell cell in Registers.Concat(Halves))
        {
            ushort value = cell.Read(cpu);
            cell.Value = value;
            cell.Text = ValueFormat.Format(value, cell.Bits, Format);
            cell.IsChanged = previous is { } p && cell.Read(p) != value;
        }

        foreach (FlagCell flag in Flags)
        {
            bool set = (cpu.Flags & (ushort)flag.Bit) != 0;
            bool isUndefined = (undefined & flag.Bit) != 0;
            bool wasSet = previous is { } p && (p.Flags & (ushort)flag.Bit) != 0;
            flag.Text = isUndefined ? "?" : set ? "1" : "0";
            flag.IsChanged = previous is not null && (isUndefined != flag.IsUndefined || (!isUndefined && set != wasSet));
            flag.IsUndefined = isUndefined;
            flag.UndefinedAfter = isUndefined ? executed : null;
        }

        _previous = cpu;
        State = CpuPanelState.Current;
        OnPropertyChanged(nameof(ChangedNames));
    }

    /// <summary>The program runs: keep the last values, marked not current, with no change marking (ui-spec §4).</summary>
    public void MarkRunning()
    {
        if (State == CpuPanelState.Current)
        {
            State = CpuPanelState.NotCurrent;
        }

        foreach (RegisterCell cell in Registers.Concat(Halves))
        {
            cell.IsChanged = false;
        }

        foreach (FlagCell flag in Flags)
        {
            flag.IsChanged = false;
        }
    }

    /// <summary>No program: the empty state (or the build-failed state).</summary>
    public void Clear(bool buildFailed = false)
    {
        _previous = null;
        State = buildFailed ? CpuPanelState.BuildFailed : CpuPanelState.Empty;
    }

    partial void OnFormatChanged(NumberFormat value)
    {
        foreach (RegisterCell cell in Registers.Concat(Halves))
        {
            cell.Text = ValueFormat.Format(cell.Value, cell.Bits, value);
        }
    }
}
