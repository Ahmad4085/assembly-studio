using System.Globalization;
using AssemblyStudio.Core;

namespace AssemblyStudio.App.Panels.Memory;

/// <summary>
/// Reads a memory address the user typed (FR-054): <c>segment:offset</c> where each part is hexadecimal (an optional
/// <c>h</c> suffix or <c>0x</c> prefix) or a register (segment: CS, DS, ES, SS; offset: AX, BX, CX, DX, SI, DI, BP, SP, IP),
/// or an offset alone, which uses DS. Registers take their values from the CPU state of the latest pause.
/// </summary>
public static class MemoryAddress
{
    /// <summary>Reads <paramref name="text"/>; returns whether it is an address.</summary>
    public static bool TryParse(string? text, CpuState? cpu, out ushort segment, out ushort offset)
    {
        segment = 0;
        offset = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Trim().Split(':');
        if (parts.Length > 2)
        {
            return false;
        }

        string segmentText = parts.Length == 2 ? parts[0] : "DS";
        string offsetText = parts[^1];
        return TryValue(segmentText, cpu, segmentRegister: true, out segment) && TryValue(offsetText, cpu, segmentRegister: false, out offset);
    }

    /// <summary>"0800:0100".</summary>
    public static string Format(ushort segment, ushort offset) =>
        string.Create(CultureInfo.InvariantCulture, $"{segment:X4}:{offset:X4}");

    private static bool TryValue(string text, CpuState? cpu, bool segmentRegister, out ushort value)
    {
        value = 0;
        string t = text.Trim().ToUpperInvariant();
        if (t.Length == 0)
        {
            return false;
        }

        ushort? register = (segmentRegister, cpu) switch
        {
            (true, { } c) => t switch { "CS" => c.CS, "DS" => c.DS, "ES" => c.ES, "SS" => c.SS, _ => null },
            (false, { } c) => t switch
            {
                "AX" => c.AX, "BX" => c.BX, "CX" => c.CX, "DX" => c.DX,
                "SI" => c.SI, "DI" => c.DI, "BP" => c.BP, "SP" => c.SP, "IP" => c.IP,
                _ => null,
            },
            _ => null,
        };
        if (register is ushort r)
        {
            value = r;
            return true;
        }

        if (t.StartsWith("0X", StringComparison.Ordinal))
        {
            t = t[2..];
        }
        else if (t.EndsWith('H'))
        {
            t = t[..^1];
        }

        return t.Length is > 0 and <= 4 && ushort.TryParse(t, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }
}
