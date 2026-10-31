using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;

namespace AssemblyStudio.App.Editor;

/// <summary>
/// The NASM 8086 highlighting (FR-011): <c>NasmSyntax.xshd</c>, with each named color drawn with the theme's
/// <c>Syntax.&lt;name&gt;</c> brush, looked up when the text is rendered. A theme switch only needs a redraw.
/// </summary>
public static class NasmHighlighting
{
    /// <summary>The token categories (color names in the XSHD; <c>Syntax.Text</c> is the editor's foreground).</summary>
    public static readonly IReadOnlyList<string> Categories = ["Mnemonic", "Register", "Directive", "Number", "String", "Comment", "Label"];

    private static readonly Lazy<IHighlightingDefinition> Shared = new(Load);

    /// <summary>The highlighting definition (loaded once).</summary>
    public static IHighlightingDefinition Definition => Shared.Value;

    private static IHighlightingDefinition Load()
    {
        using Stream stream = typeof(NasmHighlighting).Assembly.GetManifestResourceStream("AssemblyStudio.App.Editor.NasmSyntax.xshd")
            ?? throw new InvalidOperationException("The NASM syntax definition is not embedded.");
        using var reader = XmlReader.Create(stream);
        IHighlightingDefinition definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        foreach (string name in Categories)
        {
            definition.GetNamedColor(name).Foreground = new ThemeBrush("Syntax." + name);
        }

        return definition;
    }

    /// <summary>A highlighting brush that resolves a theme token from the application resources each time it is drawn.</summary>
    private sealed class ThemeBrush(string token) : HighlightingBrush
    {
        public override Brush? GetBrush(ITextRunConstructionContext context) =>
            Application.Current?.TryFindResource(token) as Brush;

        public override string ToString() => token;
    }
}
