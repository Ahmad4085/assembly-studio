using System.Globalization;
using System.Resources;

namespace AssemblyStudio.Runtime.Interrupts;

/// <summary>The runtime message texts (NFR-032): the what, where and next step of each RTE code, in <c>RuntimeMessages.resx</c>.</summary>
internal static class RuntimeMessages
{
    internal static readonly ResourceManager Resources = new("AssemblyStudio.Runtime.Interrupts.RuntimeMessages", typeof(RuntimeMessages).Assembly);

    /// <summary>The text of <paramref name="key"/> for the current UI culture, formatted with <paramref name="args"/>.</summary>
    public static string Text(string key, params object?[] args)
    {
        string template = Resources.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"Runtime message resource '{key}' is missing.");
        return args.Length == 0 ? template : string.Format(CultureInfo.CurrentCulture, template, args);
    }
}
