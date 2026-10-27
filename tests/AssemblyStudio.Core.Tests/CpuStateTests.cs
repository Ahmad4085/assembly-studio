using AssemblyStudio.Core;
using Xunit;

namespace AssemblyStudio.Core.Tests;

/// <summary>CpuState record-struct semantics: value equality and with-expression copying.</summary>
public class CpuStateTests
{
    [Fact]
    public void Equal_states_are_equal_and_hash_equal()
    {
        CpuState a = new(1, 2, 3, 4, 5, 6, 7, 8, 0x100, 0x200, 0x300, 0x400, 0x0100, 0x0202);
        CpuState b = new(1, 2, 3, 4, 5, 6, 7, 8, 0x100, 0x200, 0x300, 0x400, 0x0100, 0x0202);

        Assert.True(a == b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_states_are_not_equal()
    {
        CpuState a = default;
        CpuState b = a with { IP = 0x0100 };

        Assert.True(a != b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void With_expression_changes_only_the_named_field()
    {
        CpuState initial = new(0x1234, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);

        CpuState after = initial with { AX = 0xFFFF, Flags = 0x0001 };

        Assert.Equal(0xFFFF, after.AX);
        Assert.Equal(0x0001, after.Flags);
        // Everything else is copied, not defaulted.
        Assert.Equal(2, after.BX);
        Assert.Equal(13, after.IP);
    }

    [Fact]
    public void State_is_immutable_by_value()
    {
        // A record struct cannot be mutated: the property is init-only and State returns a copy.
        CpuState state = new(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
        CpuState copy = state;

        Assert.Equal(state, copy);
        // with-expressions produce new instances; the original is unchanged.
        _ = state with { AX = 9 };
        Assert.Equal(1, state.AX);
    }
}
