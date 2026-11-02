using System.Globalization;
using AssemblyStudio.App.Settings;

namespace AssemblyStudio.App.Panels.Machine;

/// <summary>
/// Formats a register value (FR-047): hexadecimal (<c>7F80</c>, the width's digits), unsigned decimal, signed decimal
/// (two's complement of the width) or binary in groups of four bits.
/// </summary>
public static class ValueFormat
{
    /// <summary>The value as <paramref name="format"/> shows it.</summary>
    /// <param name="value">The value.</param>
    /// <param name="bits">8 or 16.</param>
    /// <param name="format">The format.</param>
    public static string Format(ushort value, int bits, NumberFormat format)
    {
        if (bits is not (8 or 16))
        {
            throw new ArgumentOutOfRangeException(nameof(bits), bits, "8 or 16");
        }

        int masked = bits == 8 ? value & 0xFF : value;
        return format switch
        {
            NumberFormat.UnsignedDecimal => masked.ToString(CultureInfo.InvariantCulture),
            NumberFormat.SignedDecimal => (bits == 8 ? (sbyte)masked : (short)masked).ToString(CultureInfo.InvariantCulture),
            NumberFormat.Binary => Binary(masked, bits),
            _ => masked.ToString(bits == 8 ? "X2" : "X4", CultureInfo.InvariantCulture),
        };
    }

    /// <summary>The next format of Ctrl+Alt+N: hex → unsigned → signed → binary → hex.</summary>
    public static NumberFormat Next(NumberFormat format) => format switch
    {
        NumberFormat.Hexadecimal => NumberFormat.UnsignedDecimal,
        NumberFormat.UnsignedDecimal => NumberFormat.SignedDecimal,
        NumberFormat.SignedDecimal => NumberFormat.Binary,
        _ => NumberFormat.Hexadecimal,
    };

    private static string Binary(int value, int bits)
    {
        string digits = Convert.ToString(value, 2).PadLeft(bits, '0');
        return string.Join(" ", Enumerable.Range(0, bits / 4).Select(i => digits.Substring(i * 4, 4)));
    }
}
