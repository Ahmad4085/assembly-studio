using System.Globalization;
using System.Resources;

namespace AssemblyStudio.Os.Dos;

/// <summary>The DOS service error texts (NFR-032): the what, where and next step of each DOS code, in <c>DosMessages.resx</c>.</summary>
internal static class DosMessages
{
    internal static readonly ResourceManager Resources = new("AssemblyStudio.Os.Dos.DosMessages", typeof(DosMessages).Assembly);

    /// <summary>The text of <paramref name="key"/> for the current UI culture, formatted with <paramref name="args"/>.</summary>
    public static string Text(string key, params object?[] args)
    {
        string template = Resources.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"DOS message resource '{key}' is missing.");
        return args.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, args);
    }
}
