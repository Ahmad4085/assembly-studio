using System.Globalization;
using System.Resources;

namespace AssemblyStudio.Toolchain.Nasm;

/// <summary>Why an assembler can't be used (NFR-032), shown in Settings, the status bar and the build output; the texts live in <c>AssemblerProblems.resx</c>.</summary>
internal static class AssemblerProblems
{
    internal static readonly ResourceManager Resources = new("AssemblyStudio.Toolchain.Nasm.AssemblerProblems", typeof(AssemblerProblems).Assembly);

    /// <summary>The text of <paramref name="key"/> for the current UI culture, formatted with <paramref name="args"/>.</summary>
    public static string Text(string key, params object?[] args)
    {
        string template = Resources.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"Assembler problem resource '{key}' is missing.");
        return args.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, args);
    }
}
