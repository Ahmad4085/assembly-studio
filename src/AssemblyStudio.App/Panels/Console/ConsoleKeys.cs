using System.Text;
using System.Windows.Input;

namespace AssemblyStudio.App.Panels.Console;

/// <summary>
/// Turns keys typed into the console into the bytes a DOS program reads (FR-064, ADR-0004): characters in code page 437,
/// Enter 0Dh, Backspace 08h, Tab 09h, Esc 1Bh, Ctrl+letter its control code, and the extended keys (arrows, Home, End,
/// Page Up/Down, Insert, Delete, F2–F5, F7–F10) as 00h followed by the scan code. F1 (Help) and F6 (next pane) stay with
/// the IDE so the console never traps the keyboard; keys with Alt or Shift+Ctrl stay with the IDE too.
/// </summary>
public static class ConsoleKeys
{
    private static readonly Encoding Cp437 = CreateEncoding();

    /// <summary>The bytes of a key press that isn't a character, or <see langword="null"/> when the key isn't for the program.
    /// Ctrl+C is not translated here: it is routed by D-015.</summary>
    public static byte[]? FromKey(Key key, ModifierKeys modifiers)
    {
        if (modifiers == ModifierKeys.Control && key is >= Key.A and <= Key.Z && key != Key.C)
        {
            return [(byte)(key - Key.A + 1)];
        }

        if (modifiers != ModifierKeys.None)
        {
            return null;
        }

        return key switch
        {
            Key.Enter => [0x0D],
            Key.Back => [0x08],
            Key.Tab => [0x09],
            Key.Escape => [0x1B],
            Key.F2 => [0, 0x3C],
            Key.F3 => [0, 0x3D],
            Key.F4 => [0, 0x3E],
            Key.F5 => [0, 0x3F],
            Key.F7 => [0, 0x41],
            Key.F8 => [0, 0x42],
            Key.F9 => [0, 0x43],
            Key.F10 => [0, 0x44],
            Key.Home => [0, 0x47],
            Key.Up => [0, 0x48],
            Key.PageUp => [0, 0x49],
            Key.Left => [0, 0x4B],
            Key.Right => [0, 0x4D],
            Key.End => [0, 0x4F],
            Key.Down => [0, 0x50],
            Key.PageDown => [0, 0x51],
            Key.Insert => [0, 0x52],
            Key.Delete => [0, 0x53],
            _ => null,
        };
    }

    /// <summary>Typed text in code page 437. Control characters are left to <see cref="FromKey"/>; characters code page 437
    /// can't represent are dropped.</summary>
    public static byte[] FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var bytes = new List<byte>(text.Length);
        foreach (char c in text)
        {
            if (c < 0x20 || c == 0x7F)
            {
                continue;
            }

            byte[] encoded = Cp437.GetBytes([c]);
            if (encoded.Length == 1 && (encoded[0] != (byte)'?' || c == '?'))
            {
                bytes.Add(encoded[0]);
            }
        }

        return [.. bytes];
    }

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(437, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
    }
}
