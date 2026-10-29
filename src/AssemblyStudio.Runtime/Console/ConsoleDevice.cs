using System.Text;
using AssemblyStudio.Core;

namespace AssemblyStudio.Runtime.Console;

/// <summary>
/// The emulated console device (C05, ADR-0004 §6):
/// <list type="bullet">
///   <item><description>Bounded output buffer capped at 1 MB with oldest content trimmed (NFR-015, AC-045).</description></item>
///   <item><description>Snapshot delta API (<see cref="TakeOutput"/>) for UI updates (ADR-0007 §2).</description></item>
///   <item><description>DOS column counter tracking TAB (modulo 8), CR (reset to 0), BS (decrement), and printable characters.</description></item>
///   <item><description>BEL (<c>0x07</c>) tracking for visual/audible notifications.</description></item>
///   <item><description>Input queue with instruction-stamped event recording and deterministic replay (NFR-016, AC-047).</description></item>
///   <item><description>IBM PC Code Page 437 character decoding.</description></item>
/// </list>
/// </summary>
public sealed class ConsoleDevice : IConsoleDevice
{
    /// <summary>
    /// Default output buffer capacity: 1 MiB (1,048,576 bytes; NFR-015, AC-045).
    /// </summary>
    public const int DefaultMaxCapacity = 1024 * 1024;

    private static readonly Encoding Cp437Encoding = InitializeEncoding();

    private readonly byte[] _buffer;
    private readonly List<byte> _deltaBuffer = [];
    private readonly Queue<byte> _inputQueue = [];
    private readonly List<InputEvent> _recordedInputs = [];
    private readonly Queue<InputEvent> _replayQueue = [];

    private int _head;
    private int _count;

    /// <summary>
    /// Maximum capacity of the output buffer in bytes (default 1 MB).
    /// </summary>
    public int MaxCapacity => _buffer.Length;

    /// <summary>
    /// Number of bytes currently held in the output buffer.
    /// </summary>
    public int BufferLength => _count;

    /// <summary>
    /// Cumulative count of all bytes written since creation or reset.
    /// </summary>
    public long TotalBytesWritten { get; private set; }

    /// <summary>
    /// Cumulative count of all bytes trimmed due to exceeding <see cref="MaxCapacity"/>.
    /// </summary>
    public long TotalBytesTrimmed { get; private set; }

    /// <summary>
    /// Whether any output bytes have been trimmed.
    /// </summary>
    public bool HasTrimmed => TotalBytesTrimmed > 0;

    /// <summary>
    /// Current DOS column position (0-based) for TAB expansion (ADR-0004 §6).
    /// </summary>
    public int Column { get; private set; }

    /// <summary>
    /// Number of BEL (<c>0x07</c>) characters received.
    /// </summary>
    public int BelCount { get; private set; }

    /// <summary>
    /// Whether at least one BEL character has occurred.
    /// </summary>
    public bool HasBelOccurred => BelCount > 0;

    /// <summary>
    /// Current execution instruction count, used to stamp recorded input events when no provider is configured.
    /// </summary>
    public ulong CurrentInstructionCount { get; set; }

    /// <summary>
    /// Optional delegate querying the active instruction count from the CPU or session worker.
    /// </summary>
    public Func<ulong>? InstructionCountProvider { get; set; }

    /// <summary>
    /// Whether the console is replaying a previously recorded input sequence.
    /// </summary>
    public bool ReplayMode { get; set; }

    /// <summary>
    /// The chronologically recorded input events stamped with instruction counts (AC-047, NFR-016).
    /// </summary>
    public IReadOnlyList<InputEvent> RecordedInputs => _recordedInputs;

    /// <summary>
    /// Number of bytes currently waiting in the live input queue.
    /// </summary>
    public int PendingInputCount => ReplayMode ? _replayQueue.Count : _inputQueue.Count;

    /// <summary>
    /// Creates a new <see cref="ConsoleDevice"/> with the specified maximum output capacity.
    /// </summary>
    /// <param name="maxCapacity">Maximum buffer capacity in bytes (must be > 0, defaults to 1 MiB).</param>
    public ConsoleDevice(int maxCapacity = DefaultMaxCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCapacity, 1);
        _buffer = new byte[maxCapacity];
    }

    private static Encoding InitializeEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(437);
        }
        catch
        {
            return Encoding.Latin1;
        }
    }

    private ulong GetEffectiveInstructionCount() => InstructionCountProvider?.Invoke() ?? CurrentInstructionCount;

    /// <inheritdoc />
    public void WriteOutput(byte value)
    {
        TotalBytesWritten++;

        // 1. Column counter and BEL arithmetic
        switch (value)
        {
            case 0x0D: // CR (Carriage Return)
                Column = 0;
                break;

            case 0x08: // BS (Backspace - non-destructive column decrement)
                if (Column > 0)
                {
                    Column--;
                }
                break;

            case 0x09: // TAB (advance to next multiple of 8)
                Column = (Column + 8) & ~7;
                break;

            case 0x07: // BEL
                BelCount++;
                break;

            default:
                if (value >= 0x20)
                {
                    // Printable character advances column by 1
                    Column++;
                }
                // Control characters < 0x20 (other than CR/BS/TAB/BEL) do not advance the column in DOS
                break;
        }

        // 2. Circular buffer append with oldest content trimmed at cap (NFR-015, AC-045)
        if (_count < _buffer.Length)
        {
            int index = (_head + _count) % _buffer.Length;
            _buffer[index] = value;
            _count++;
        }
        else
        {
            // Buffer is full: overwrite the oldest element at _head, advance _head, and increment trim counter
            _buffer[_head] = value;
            _head = (_head + 1) % _buffer.Length;
            TotalBytesTrimmed++;
        }

        // 3. Snapshot delta buffer
        _deltaBuffer.Add(value);
        if (_deltaBuffer.Count > _buffer.Length)
        {
            // Bounded delta buffer during extreme floods
            _deltaBuffer.RemoveAt(0);
        }
    }

    /// <summary>
    /// Writes multiple output bytes to the console.
    /// </summary>
    /// <param name="bytes">Span of bytes to output.</param>
    public void WriteOutput(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            WriteOutput(b);
        }
    }

    /// <summary>
    /// Writes a string to the console, encoding it using CP437.
    /// </summary>
    /// <param name="text">Text to output.</param>
    public void WriteString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] encoded = Cp437Encoding.GetBytes(text);
        WriteOutput(encoded);
    }

    /// <summary>
    /// Emits a BEL (<c>0x07</c>) character to the console.
    /// </summary>
    public void RingBell() => WriteOutput(0x07);

    /// <summary>
    /// Resets the <see cref="BelCount"/> to zero.
    /// </summary>
    public void ResetBel() => BelCount = 0;

    /// <inheritdoc />
    public string TakeOutput()
    {
        if (_deltaBuffer.Count == 0)
        {
            return string.Empty;
        }

        string deltaText = Cp437Encoding.GetString(_deltaBuffer.ToArray());
        _deltaBuffer.Clear();
        return deltaText;
    }

    /// <summary>
    /// Returns the entire active contents of the output buffer as a chronological byte array.
    /// </summary>
    /// <returns>A new byte array containing the active output bytes.</returns>
    public byte[] ToByteArray()
    {
        if (_count == 0)
        {
            return [];
        }

        byte[] result = new byte[_count];
        int firstChunk = Math.Min(_count, _buffer.Length - _head);
        Array.Copy(_buffer, _head, result, 0, firstChunk);

        int secondChunk = _count - firstChunk;
        if (secondChunk > 0)
        {
            Array.Copy(_buffer, 0, result, firstChunk, secondChunk);
        }

        return result;
    }

    /// <summary>
    /// Returns the entire active contents of the output buffer decoded as a CP437 string.
    /// </summary>
    public string GetAllText()
    {
        byte[] bytes = ToByteArray();
        return bytes.Length == 0 ? string.Empty : Cp437Encoding.GetString(bytes);
    }

    // ---- Input Queue, Recording, and Replay ---------------------------------------------

    /// <summary>
    /// Enqueues one byte into the console's live input queue.
    /// </summary>
    /// <param name="value">Byte value to enqueue.</param>
    public void EnqueueInput(byte value) => _inputQueue.Enqueue(value);

    /// <summary>
    /// Enqueues multiple bytes into the console's live input queue.
    /// </summary>
    /// <param name="bytes">Bytes to enqueue.</param>
    public void EnqueueInput(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            _inputQueue.Enqueue(b);
        }
    }

    /// <summary>
    /// Enqueues a string into the console's live input queue, encoded using CP437.
    /// </summary>
    /// <param name="text">Text to enqueue.</param>
    public void EnqueueInputString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] bytes = Cp437Encoding.GetBytes(text);
        EnqueueInput(bytes);
    }

    /// <summary>
    /// Configures the console for deterministic replay from a recorded input sequence (AC-047).
    /// </summary>
    /// <param name="events">The recorded input events to replay.</param>
    public void SetReplayInputs(IEnumerable<InputEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _replayQueue.Clear();
        foreach (var ev in events)
        {
            _replayQueue.Enqueue(ev);
        }
        ReplayMode = true;
    }

    /// <summary>
    /// Clears the recorded input events history.
    /// </summary>
    public void ClearRecording() => _recordedInputs.Clear();

    /// <inheritdoc />
    public bool TryReadInput(out byte value)
    {
        ulong currentStep = GetEffectiveInstructionCount();

        if (ReplayMode)
        {
            if (_replayQueue.Count > 0)
            {
                var next = _replayQueue.Peek();
                if (next.InstructionCount <= currentStep)
                {
                    _replayQueue.Dequeue();
                    value = next.Value;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        if (_inputQueue.TryDequeue(out value))
        {
            _recordedInputs.Add(new InputEvent(currentStep, value));
            return true;
        }

        value = 0;
        return false;
    }

    /// <inheritdoc />
    public bool TryPeekInput(out byte value)
    {
        if (ReplayMode)
        {
            if (_replayQueue.TryPeek(out InputEvent next) && next.InstructionCount <= GetEffectiveInstructionCount())
            {
                value = next.Value;
                return true;
            }

            value = 0;
            return false;
        }

        return _inputQueue.TryPeek(out value);
    }

    /// <summary>
    /// Clears the output buffer, pending delta, column counter, BEL counter, and live input queue (FR-063).
    /// Does not clear the recorded input log unless <see cref="ClearRecording"/> is called.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
        _deltaBuffer.Clear();
        _inputQueue.Clear();
        _replayQueue.Clear();
        Column = 0;
        BelCount = 0;
        TotalBytesWritten = 0;
        TotalBytesTrimmed = 0;
        ReplayMode = false;
    }
}
