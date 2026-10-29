using AssemblyStudio.Core;

namespace AssemblyStudio.Debugging.Snapshots;

/// <summary>
/// The execution history ring buffer (FR-058, ADR-0007 v1.1): the last 1,000 executed instructions with their
/// bytes as executed. Recording is allocation-free (it runs once per instruction); disassembly happens only when
/// entries are read for a snapshot. Used by the worker thread only.
/// </summary>
internal sealed class ExecutionHistory
{
    /// <summary>History length (FR-058: last 1,000).</summary>
    public const int Capacity = 1_000;

    private const int MaxInstructionLength = 6;

    private readonly Recorded[] _records = new Recorded[Capacity];
    private long _total;
    private long _publishedUpTo;

    /// <summary>Records the instruction at CS:IP before it executes (its bytes as they are in memory now).</summary>
    public void Record(IMemory memory, ushort cs, ushort ip)
    {
        ulong bytes = 0;
        for (int i = 0; i < MaxInstructionLength; i++)
        {
            bytes |= (ulong)memory.Read8(cs, (ushort)(ip + i)) << (8 * i);
        }

        _records[_total % Capacity] = new Recorded(cs, ip, bytes);
        _total++;
    }

    /// <summary>Forgets the last recorded instruction (it didn't execute: the step halted, terminated or waited).</summary>
    public void Unrecord()
    {
        if (_total > _publishedUpTo)
        {
            _total--;
        }
    }

    /// <summary>Forgets everything (load or reset).</summary>
    public void Clear()
    {
        _total = 0;
        _publishedUpTo = 0;
    }

    /// <summary>The entries recorded since the previous call (at most <see cref="Capacity"/>), oldest first.</summary>
    public IReadOnlyList<HistoryEntry> TakeDelta(IDisassembler disassembler)
    {
        long from = Math.Max(_publishedUpTo, _total - Capacity);
        _publishedUpTo = _total;
        return Entries(from, disassembler);
    }

    private HistoryEntry[] Entries(long from, IDisassembler disassembler)
    {
        var entries = new HistoryEntry[_total - from];
        Span<byte> buffer = stackalloc byte[MaxInstructionLength];
        for (long n = from; n < _total; n++)
        {
            Recorded record = _records[n % Capacity];
            for (int i = 0; i < MaxInstructionLength; i++)
            {
                buffer[i] = (byte)(record.Bytes >> (8 * i));
            }

            DecodedInstruction? decoded = disassembler.Decode(buffer, record.Ip);
            int length = decoded?.Length ?? 1;
            entries[n - from] = new HistoryEntry(record.Cs, record.Ip, buffer[..length].ToArray(), decoded?.Text);
        }

        return entries;
    }

    private readonly record struct Recorded(ushort Cs, ushort Ip, ulong Bytes);
}
