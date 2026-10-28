using AssemblyStudio.Core;
using AssemblyStudio.Runtime.Console;
using Xunit;

namespace AssemblyStudio.Runtime.Tests;

public class ConsoleDeviceTests
{
    // ---- Output Buffer, TakeOutput, and GetAllText --------------------------------------

    [Fact]
    public void WriteOutput_appends_and_TakeOutput_returns_delta_and_clears()
    {
        var console = new ConsoleDevice();

        console.WriteString("Hello");
        Assert.Equal("Hello", console.TakeOutput());

        // Delta is cleared after TakeOutput
        Assert.Equal(string.Empty, console.TakeOutput());

        console.WriteString(" World");
        Assert.Equal(" World", console.TakeOutput());
        Assert.Equal(string.Empty, console.TakeOutput());

        // GetAllText retains full chronological output
        Assert.Equal("Hello World", console.GetAllText());
    }

    [Fact]
    public void WriteOutput_span_and_byte_methods_work_identically()
    {
        var console = new ConsoleDevice();

        console.WriteOutput((byte)'A');
        console.WriteOutput([(byte)'B', (byte)'C', (byte)'D']);

        Assert.Equal("ABCD", console.TakeOutput());
        Assert.Equal("ABCD", console.GetAllText());
        Assert.Equal(4, console.BufferLength);
        Assert.Equal(4L, console.TotalBytesWritten);
    }

    // ---- Column Counter (ADR-0004 §6, R-DOS §2) -----------------------------------------

    [Fact]
    public void Column_counter_tracks_printable_characters()
    {
        var console = new ConsoleDevice();
        Assert.Equal(0, console.Column);

        console.WriteOutput((byte)'A');
        Assert.Equal(1, console.Column);

        console.WriteString("12345");
        Assert.Equal(6, console.Column);
    }

    [Fact]
    public void Column_counter_resets_to_zero_on_CR()
    {
        var console = new ConsoleDevice();
        console.WriteString("Testing123");
        Assert.Equal(10, console.Column);

        console.WriteOutput(0x0D); // CR
        Assert.Equal(0, console.Column);
    }

    [Fact]
    public void Column_counter_decrements_on_BS_without_underflowing()
    {
        var console = new ConsoleDevice();
        console.WriteString("ABC");
        Assert.Equal(3, console.Column);

        console.WriteOutput(0x08); // BS
        Assert.Equal(2, console.Column);

        console.WriteOutput(0x08);
        console.WriteOutput(0x08);
        Assert.Equal(0, console.Column);

        // Does not decrement below zero
        console.WriteOutput(0x08);
        Assert.Equal(0, console.Column);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 8)]
    [InlineData(7, 8)]
    [InlineData(8, 16)]
    [InlineData(9, 16)]
    [InlineData(15, 16)]
    [InlineData(16, 24)]
    [InlineData(22, 24)]
    public void Column_counter_advances_to_next_multiple_of_8_on_TAB(int initialColumn, int expectedColumn)
    {
        var console = new ConsoleDevice();

        // Advance column to initialColumn
        for (int i = 0; i < initialColumn; i++)
        {
            console.WriteOutput((byte)'X');
        }
        Assert.Equal(initialColumn, console.Column);

        // Write TAB (0x09)
        console.WriteOutput(0x09);
        Assert.Equal(expectedColumn, console.Column);
    }

    [Fact]
    public void LF_and_raw_control_bytes_do_not_advance_column()
    {
        var console = new ConsoleDevice();
        console.WriteOutput((byte)'Z');
        Assert.Equal(1, console.Column);

        // LF (0x0A) does not change column
        console.WriteOutput(0x0A);
        Assert.Equal(1, console.Column);

        // NUL (0x00) and other control bytes < 0x20
        console.WriteOutput(0x00);
        console.WriteOutput(0x01);
        Assert.Equal(1, console.Column);
    }

    // ---- BEL (07h) Tracking ------------------------------------------------------------

    [Fact]
    public void BEL_character_increments_BelCount_and_does_not_advance_column()
    {
        var console = new ConsoleDevice();
        console.WriteString("Test");
        Assert.Equal(4, console.Column);
        Assert.Equal(0, console.BelCount);
        Assert.False(console.HasBelOccurred);

        console.WriteOutput(0x07); // BEL
        Assert.Equal(4, console.Column);
        Assert.Equal(1, console.BelCount);
        Assert.True(console.HasBelOccurred);

        console.RingBell();
        Assert.Equal(2, console.BelCount);

        console.ResetBel();
        Assert.Equal(0, console.BelCount);
        Assert.False(console.HasBelOccurred);
    }

    // ---- AC-045: Output Cap and Oldest Content Trimming --------------------------------

    [Fact]
    public void AC_045_Cap_and_trim_limits_buffer_and_trims_oldest_content()
    {
        // AC-045: Buffer stays within cap with oldest output trimmed (NFR-015).
        const int capacity = 10;
        var console = new ConsoleDevice(capacity);

        // Write 7 bytes: "ABCDEFG"
        console.WriteString("ABCDEFG");
        Assert.Equal(7, console.BufferLength);
        Assert.Equal(0L, console.TotalBytesTrimmed);
        Assert.False(console.HasTrimmed);

        // Write 5 more bytes: "12345" (total 12 bytes written, capacity 10 -> oldest 2 bytes 'A','B' trimmed)
        console.WriteString("12345");
        Assert.Equal(10, console.BufferLength);
        Assert.Equal(12L, console.TotalBytesWritten);
        Assert.Equal(2L, console.TotalBytesTrimmed);
        Assert.True(console.HasTrimmed);

        // Buffer contents should be "CDEFG12345"
        Assert.Equal("CDEFG12345", console.GetAllText());
    }

    [Fact]
    public void Output_flood_under_default_1MB_cap_trims_safely()
    {
        // Default cap: 1,048,576 bytes
        var console = new ConsoleDevice();
        Assert.Equal(ConsoleDevice.DefaultMaxCapacity, console.MaxCapacity);

        // Write 1 MB + 100 bytes
        const int extraBytes = 100;
        byte[] chunk = new byte[65536]; // 64 KiB
        Array.Fill(chunk, (byte)'X');

        // Write 16 chunks = 1,048,576 bytes (exactly 1 MB)
        for (int i = 0; i < 16; i++)
        {
            console.WriteOutput(chunk);
        }

        Assert.Equal(ConsoleDevice.DefaultMaxCapacity, console.BufferLength);
        Assert.Equal(0L, console.TotalBytesTrimmed);

        // Write extra 100 bytes of 'Y'
        byte[] extra = new byte[extraBytes];
        Array.Fill(extra, (byte)'Y');
        console.WriteOutput(extra);

        Assert.Equal(ConsoleDevice.DefaultMaxCapacity, console.BufferLength);
        Assert.Equal(extraBytes, console.TotalBytesTrimmed);
        Assert.True(console.HasTrimmed);
    }

    // ---- Input Queue, Recording, and Replay (AC-047, NFR-016) ---------------------------

    [Fact]
    public void Input_queue_FIFO_order_and_empty_check()
    {
        var console = new ConsoleDevice();

        Assert.False(console.TryReadInput(out byte emptyVal));
        Assert.Equal(0, emptyVal);

        console.EnqueueInput((byte)'K');
        console.EnqueueInputString("EY");

        Assert.Equal(3, console.PendingInputCount);

        Assert.True(console.TryReadInput(out byte b1));
        Assert.Equal((byte)'K', b1);

        Assert.True(console.TryReadInput(out byte b2));
        Assert.Equal((byte)'E', b2);

        Assert.True(console.TryReadInput(out byte b3));
        Assert.Equal((byte)'Y', b3);

        Assert.False(console.TryReadInput(out _));
        Assert.Equal(0, console.PendingInputCount);
    }

    [Fact]
    public void Input_recording_stamps_instruction_counts_accurately()
    {
        var console = new ConsoleDevice();

        console.CurrentInstructionCount = 42;
        console.EnqueueInput((byte)'A');
        console.TryReadInput(out _);

        console.CurrentInstructionCount = 1000;
        console.EnqueueInput((byte)'B');
        console.TryReadInput(out _);

        Assert.Equal(2, console.RecordedInputs.Count);
        Assert.Equal(new InputEvent(42, (byte)'A'), console.RecordedInputs[0]);
        Assert.Equal(new InputEvent(1000, (byte)'B'), console.RecordedInputs[1]);
    }

    [Fact]
    public void Input_recording_uses_InstructionCountProvider_when_configured()
    {
        var console = new ConsoleDevice();
        ulong count = 500;
        console.InstructionCountProvider = () => count;

        console.EnqueueInput((byte)'Q');
        console.TryReadInput(out _);

        count = 999;
        console.EnqueueInput((byte)'R');
        console.TryReadInput(out _);

        Assert.Equal(2, console.RecordedInputs.Count);
        Assert.Equal(500UL, console.RecordedInputs[0].InstructionCount);
        Assert.Equal(999UL, console.RecordedInputs[1].InstructionCount);
    }

    [Fact]
    public void AC_047_Deterministic_replay_from_recorded_input_sequence()
    {
        // AC-047: Given a recorded input sequence, when run twice, both produce identical traces.
        var recordedEvents = new List<InputEvent>
        {
            new(100, (byte)'1'),
            new(200, (byte)'2'),
            new(350, (byte)'3')
        };

        var console = new ConsoleDevice();
        console.SetReplayInputs(recordedEvents);
        Assert.True(console.ReplayMode);
        Assert.Equal(3, console.PendingInputCount);

        // Before step 100: no input available
        console.CurrentInstructionCount = 50;
        Assert.False(console.TryReadInput(out _));

        console.CurrentInstructionCount = 99;
        Assert.False(console.TryReadInput(out _));

        // At step 100: '1' becomes available
        console.CurrentInstructionCount = 100;
        Assert.True(console.TryReadInput(out byte val1));
        Assert.Equal((byte)'1', val1);

        // Between 101 and 199: no input available
        console.CurrentInstructionCount = 150;
        Assert.False(console.TryReadInput(out _));

        // At step 200: '2' becomes available
        console.CurrentInstructionCount = 200;
        Assert.True(console.TryReadInput(out byte val2));
        Assert.Equal((byte)'2', val2);

        // At step 350: '3' becomes available
        console.CurrentInstructionCount = 350;
        Assert.True(console.TryReadInput(out byte val3));
        Assert.Equal((byte)'3', val3);

        // Replay queue exhausted
        console.CurrentInstructionCount = 400;
        Assert.False(console.TryReadInput(out _));
    }

    // ---- Clear and Reset (FR-063) ------------------------------------------------------

    [Fact]
    public void Clear_resets_output_column_bel_and_queues_preserving_recorded_history()
    {
        var console = new ConsoleDevice();

        console.WriteString("Hello");
        console.RingBell();
        console.CurrentInstructionCount = 10;
        console.EnqueueInput((byte)'Z');
        console.TryReadInput(out _);

        Assert.Equal(5, console.Column);
        Assert.Equal(1, console.BelCount);
        Assert.Single(console.RecordedInputs);

        console.Clear();

        Assert.Equal(0, console.Column);
        Assert.Equal(0, console.BelCount);
        Assert.Equal(0, console.BufferLength);
        Assert.Equal(0L, console.TotalBytesWritten);
        Assert.Equal(string.Empty, console.TakeOutput());
        Assert.Equal(string.Empty, console.GetAllText());
        Assert.False(console.TryReadInput(out _));

        // Recorded log preserved until explicit ClearRecording()
        Assert.Single(console.RecordedInputs);

        console.ClearRecording();
        Assert.Empty(console.RecordedInputs);
    }
}
