using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Memory;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

public class RealModeMemoryTests
{
    [Fact]
    public void Memory_is_exactly_one_megabyte_and_initialized_to_zero()
    {
        var mem = new RealModeMemory();

        Assert.Equal(1024 * 1024, RealModeMemory.MemorySize);
        Assert.Equal(0xFFFFF, RealModeMemory.AddressMask);

        // Spot check boundaries and random addresses
        Assert.Equal(0, mem.Read8(0x0000, 0x0000));
        Assert.Equal(0, mem.Read8(0x1000, 0x0000));
        Assert.Equal(0, mem.Read8(0xFFFF, 0x000F));
        Assert.Equal(0, mem.ReadPhysical(0xFFFFF));
        Assert.Empty(mem.CurrentJournal);
    }

    [Theory]
    [InlineData(0x0000, 0x0000, 0x00000)]
    [InlineData(0x0000, 0x0100, 0x00100)]
    [InlineData(0x0010, 0x0000, 0x00100)]
    [InlineData(0x0800, 0x0100, 0x08100)]
    [InlineData(0x1234, 0x5678, 0x179B8)]
    [InlineData(0xF000, 0xFFFF, 0xFFFFF)]
    [InlineData(0xFFFF, 0x0010, 0x00000)] // 0xFFFF0 + 0x10 = 0x100000 -> 0x00000 (20-bit wrap)
    [InlineData(0xFFFF, 0xFFFF, 0x0FFEF)] // 0xFFFF0 + 0xFFFF = 0x10FFEF -> 0x0FFEF
    [InlineData(0xFFFF, 0x0011, 0x00001)] // 0xFFFF0 + 0x11 = 0x100001 -> 0x00001
    public void ToPhysicalAddress_computes_correct_20bit_wrapped_address(ushort seg, ushort off, int expectedPhysical)
    {
        int actual = RealModeMemory.ToPhysicalAddress(seg, off);
        Assert.Equal(expectedPhysical, actual);
    }

    [Fact]
    public void Read8_and_Write8_respect_physical_aliasing()
    {
        var mem = new RealModeMemory();

        // 0x0010:0x0000 and 0x0000:0x0100 alias to physical address 0x00100
        mem.Write8(0x0010, 0x0000, 0x42);

        Assert.Equal(0x42, mem.Read8(0x0000, 0x0100));
        Assert.Equal(0x42, mem.ReadPhysical(0x00100));
    }

    [Fact]
    public void Physical_address_wraps_at_one_megabyte_boundary()
    {
        var mem = new RealModeMemory();

        // Write to (0xFFFF, 0x0010) which wraps to physical address 0x00000
        mem.Write8(0xFFFF, 0x0010, 0xAA);

        Assert.Equal(0xAA, mem.ReadPhysical(0x00000));
        Assert.Equal(0xAA, mem.Read8(0x0000, 0x0000));

        // Write to (0xFFFF, 0xFFFF) which wraps to physical address 0x0FFEF
        mem.Write8(0xFFFF, 0xFFFF, 0xBB);
        Assert.Equal(0xBB, mem.ReadPhysical(0x0FFEF));
        Assert.Equal(0xBB, mem.Read8(0x0000, 0x0FFEF));
        Assert.Equal(0xBB, mem.Read8(0x0FFE, 0x000F));
    }

    [Fact]
    public void Read16_and_Write16_are_little_endian()
    {
        var mem = new RealModeMemory();

        mem.Write16(0x1000, 0x0050, 0x1234);

        // Low byte at offset, high byte at offset+1
        Assert.Equal(0x34, mem.Read8(0x1000, 0x0050));
        Assert.Equal(0x12, mem.Read8(0x1000, 0x0051));
        Assert.Equal(0x1234, mem.Read16(0x1000, 0x0050));
    }

    [Fact]
    public void Word_access_wraps_at_segment_boundary_offset_0xFFFF()
    {
        var mem = new RealModeMemory();

        // Segment 0x2000, offset 0xFFFF
        // Low byte goes to 0x2000:0xFFFF -> physical 0x2FFFF
        // High byte goes to 0x2000:0x0000 -> physical 0x20000 (wraps within 16-bit segment!)
        mem.Write16(0x2000, 0xFFFF, 0xA55A);

        Assert.Equal(0x5A, mem.Read8(0x2000, 0xFFFF));
        Assert.Equal(0x5A, mem.ReadPhysical(0x2FFFF));

        Assert.Equal(0xA5, mem.Read8(0x2000, 0x0000));
        Assert.Equal(0xA5, mem.ReadPhysical(0x20000));

        // Read16 must also wrap within segment
        Assert.Equal(0xA55A, mem.Read16(0x2000, 0xFFFF));
    }

    [Fact]
    public void Word_access_at_0xFFFF_offset_also_respects_20bit_wrap()
    {
        var mem = new RealModeMemory();

        // Segment 0xFFFF, offset 0xFFFF
        // Low byte at 0xFFFF:0xFFFF -> (0xFFFF0 + 0xFFFF) & 0xFFFFF = 0x0FFEF
        // High byte at 0xFFFF:0x0000 -> (0xFFFF0 + 0x0000) & 0xFFFFF = 0x0FFFF0
        mem.Write16(0xFFFF, 0xFFFF, 0xBEEF);

        Assert.Equal(0xEF, mem.ReadPhysical(0x0FFEF));
        Assert.Equal(0xBE, mem.ReadPhysical(0x0FFFF0));
        Assert.Equal(0xBEEF, mem.Read16(0xFFFF, 0xFFFF));
    }

    [Fact]
    public void Journal_records_byte_writes_and_clears_on_TakeJournal()
    {
        var mem = new RealModeMemory();

        mem.Write8(0x0000, 0x0010, 0x77);
        mem.Write8(0x0000, 0x0020, 0x88);

        Assert.Equal(2, mem.CurrentJournal.Count);
        Assert.Equal(new MemoryChange(0x00010, 0x00), mem.CurrentJournal[0]);
        Assert.Equal(new MemoryChange(0x00020, 0x00), mem.CurrentJournal[1]);

        var snapshot = mem.TakeJournal();
        Assert.Equal(2, snapshot.Count);
        Assert.Empty(mem.CurrentJournal);

        // Next call returns empty until new writes
        Assert.Empty(mem.TakeJournal());

        // Subsequent write creates new journal
        mem.Write8(0x0000, 0x0010, 0x99);
        Assert.Single(mem.CurrentJournal);
        Assert.Equal(new MemoryChange(0x00010, 0x77), mem.CurrentJournal[0]);
    }

    [Fact]
    public void Journal_records_word_writes_as_two_ordered_byte_changes()
    {
        var mem = new RealModeMemory();

        mem.Write16(0x0100, 0x0004, 0xCAFE);

        var journal = mem.TakeJournal();
        Assert.Equal(2, journal.Count);

        int lowAddr = RealModeMemory.ToPhysicalAddress(0x0100, 0x0004);
        int highAddr = RealModeMemory.ToPhysicalAddress(0x0100, 0x0005);

        Assert.Equal(new MemoryChange(lowAddr, 0x00), journal[0]);
        Assert.Equal(new MemoryChange(highAddr, 0x00), journal[1]);
    }

    [Fact]
    public void ExtractChangedRanges_merges_contiguous_writes_and_deduplicates()
    {
        var changes = new List<MemoryChange>
        {
            new(0x1000, 0x00),
            new(0x1001, 0x00),
            new(0x1002, 0x00),
            new(0x1001, 0xAA), // Duplicate address 0x1001
            new(0x2000, 0x00),
            new(0x2001, 0x00),
            new(0x3000, 0x00)
        };

        var ranges = RealModeMemory.ExtractChangedRanges(changes);

        Assert.Equal(3, ranges.Count);
        Assert.Equal(new AddressRange(0x1000, 3), ranges[0]);
        Assert.Equal(new AddressRange(0x2000, 2), ranges[1]);
        Assert.Equal(new AddressRange(0x3000, 1), ranges[2]);

        Assert.True(ranges[0].Contains(0x1000));
        Assert.True(ranges[0].Contains(0x1002));
        Assert.False(ranges[0].Contains(0x1003));
    }

    [Fact]
    public void ExtractChangedRanges_handles_empty_and_single_entry()
    {
        Assert.Empty(RealModeMemory.ExtractChangedRanges(Array.Empty<MemoryChange>()));

        var single = RealModeMemory.ExtractChangedRanges([new MemoryChange(0x500, 0x11)]);
        Assert.Single(single);
        Assert.Equal(new AddressRange(0x500, 1), single[0]);
    }

    [Fact]
    public void Rollback_restores_memory_state_in_reverse_chronological_order()
    {
        var mem = new RealModeMemory();

        mem.Write8(0x0000, 0x0100, 0xAA);
        mem.TakeJournal(); // Commit initial setup

        // Now write sequence: 0xAA -> 0xBB -> 0xCC at the same address
        mem.Write8(0x0000, 0x0100, 0xBB);
        mem.Write8(0x0000, 0x0100, 0xCC);

        var changes = mem.TakeJournal();
        Assert.Equal(0xCC, mem.Read8(0x0000, 0x0100));

        // Rolling back the changes must restore the pre-sequence value 0xAA
        mem.Rollback(changes);
        Assert.Equal(0xAA, mem.Read8(0x0000, 0x0100));
    }

    [Fact]
    public void RollbackJournal_reverts_uncommitted_journal_and_clears_it()
    {
        var mem = new RealModeMemory();

        mem.Write8(0x0000, 0x0200, 0x55);
        mem.TakeJournal(); // Baseline

        mem.Write8(0x0000, 0x0200, 0x66);
        mem.Write16(0x0000, 0x0300, 0x1234);

        Assert.Equal(0x66, mem.Read8(0x0000, 0x0200));
        Assert.Equal(0x1234, mem.Read16(0x0000, 0x0300));
        Assert.Equal(3, mem.CurrentJournal.Count);

        mem.RollbackJournal();

        Assert.Equal(0x55, mem.Read8(0x0000, 0x0200));
        Assert.Equal(0x0000, mem.Read16(0x0000, 0x0300));
        Assert.Empty(mem.CurrentJournal);
    }

    [Fact]
    public void LoadBytes_writes_without_affecting_journal()
    {
        var mem = new RealModeMemory();
        byte[] payload = [0x90, 0x90, 0xB8, 0x00, 0x4C, 0xCD, 0x21];

        mem.LoadBytes(0x08100, payload);

        Assert.Empty(mem.CurrentJournal);
        Assert.Equal(0x90, mem.ReadPhysical(0x08100));
        Assert.Equal(0x21, mem.ReadPhysical(0x08106));
    }

    [Fact]
    public void CopyTo_extracts_memory_span_correctly()
    {
        var mem = new RealModeMemory();
        mem.LoadBytes(0x00100, new byte[] { 0x10, 0x20, 0x30, 0x40 });

        Span<byte> dest = stackalloc byte[4];
        mem.CopyTo(0x00100, dest);

        Assert.Equal(0x10, dest[0]);
        Assert.Equal(0x20, dest[1]);
        Assert.Equal(0x30, dest[2]);
        Assert.Equal(0x40, dest[3]);
    }

    [Fact]
    public void Reset_clears_memory_and_journal()
    {
        var mem = new RealModeMemory();
        mem.Write8(0x1000, 0x0000, 0xFF);

        Assert.Single(mem.CurrentJournal);
        Assert.Equal(0xFF, mem.Read8(0x1000, 0x0000));

        mem.Reset();

        Assert.Empty(mem.CurrentJournal);
        Assert.Equal(0, mem.Read8(0x1000, 0x0000));
    }
}
