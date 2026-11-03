using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace AssemblyStudio.ArchitectureTests;

/// <summary>
/// TASK-104, AC-060 (NFR-032): no user-facing string is hard-coded outside resource files. Every C# string literal and
/// XAML text under <c>src</c> that reads as words is either in a context that never reaches the screen (a log entry, a
/// developer exception, a regular expression) or listed in <see cref="Allowed"/> with the reason it stays as it is.
/// </summary>
public sealed partial class LocalizableStringTests
{
    /// <summary>The literals that read as words but are not translatable user text, each with its reason.</summary>
    private static readonly (string File, string Text, string Reason)[] Allowed =
    [
        // The About diagnostics block is English on purpose so it can be pasted into a bug report as-is (FR-076).
        ("AssemblyStudio.App/About/AboutViewModel.cs", "(version unknown)", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "not usable ({}, {}): {}", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "Execution engine", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "{} {} (Intel 8086, real mode, interpreter)", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "DOS services", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "Operating system", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "Log folder", "About diagnostics, English by design (FR-076)"),
        ("AssemblyStudio.App/About/AboutViewModel.cs", "Assembly Studio {}", "About diagnostics, English by design (FR-076)"),
        // Names a user never reads as text.
        ("AssemblyStudio.App/App.xaml.cs", "build and run", "startup timeline step name, written to the log only"),
        ("AssemblyStudio.App/App.xaml.cs", "first frame", "startup timeline step name, written to the log only"),
        ("AssemblyStudio.App/App.xaml.cs", "Editor warm-up", "thread name, seen only in a debugger"),
        ("AssemblyStudio.App/Logging/StartupTimeline.cs", "runtime and WPF", "startup timeline step name, written to the log only"),
        ("AssemblyStudio.App/Logging/StartupTimeline.cs", "Startup {} ms to the first frame: {}; JIT {} ms in all", "log entry (written by App.xaml.cs)"),
        ("AssemblyStudio.Content/ContentCatalog.cs", "Content item '{}'", "start of the catalog's developer exceptions"),
        ("AssemblyStudio.App/Logging/FileLog.cs", " [entry truncated]", "log entry"),
        ("AssemblyStudio.App/Logging/FileLog.cs", "yyyy-MM-dd HH:mm:ss.fff zzz", "date format pattern of the log"),
        ("AssemblyStudio.Debugging/DebugSession.cs", "Assembly Studio debug session", "thread name, seen only in a debugger"),
        ("AssemblyStudio.App/Settings/AppSettings.cs", "Cascadia Mono, Consolas", "font family names (default setting)"),
        // Technical notation, the same in every language: interrupts, registers and NASM's own output.
        ("AssemblyStudio.Os.Dos/DosServices.cs", "INT 21h AH=00h", "interrupt and register notation, inserted into DOS001"),
        ("AssemblyStudio.Toolchain.Nasm/NasmAssembler.cs", "[map all]", "NASM directive passed on its command line"),
        ("AssemblyStudio.Toolchain.Nasm/SourceMapping/NasmListing.cs", "-- Program origin", "NASM map file heading (parsed)"),
        ("AssemblyStudio.Toolchain.Nasm/SourceMapping/NasmListing.cs", "-- Sections (summary)", "NASM map file heading (parsed)"),
        ("AssemblyStudio.App/Commands/CommandTable.cs", "P2 (ui-spec §6), not planned for this release", "planning note in the command table, not shown"),
    ];

    /// <summary>Files whose literals are reference data, never user text.</summary>
    private static readonly (string File, string Reason)[] AllowedFiles =
    [
        ("AssemblyStudio.Engine.I8086/OpcodeTable.cs", "opcode mnemonics and notes; used only inside the engine, not shown"),
        ("AssemblyStudio.App/Shell/Strings.cs", "generated from Strings.resx (its doc comments repeat the texts)"),
    ];

    /// <summary>Files whose rows start with an identifier literal, <c>new("…", …)</c>, that no user reads.</summary>
    private static readonly (string File, string Reason)[] AllowedRowNames =
    [
        ("AssemblyStudio.App/Commands/CommandTable.cs", "command names identify rows for the shortcut tests (CommandTableTests); the menus and tooltips use the Strings columns"),
    ];

    [Fact]
    public void TASK_104_AC_060_no_user_facing_string_is_hard_coded_in_the_csharp_source()
    {
        string src = Path.Combine(FindRepositoryRoot(), "src");
        var found = new List<(string File, int Line, string Text)>();
        foreach (string path in SourceFiles(src, "*.cs"))
        {
            string file = Relative(src, path);
            if (AllowedFiles.Any(f => f.File == file))
            {
                continue;
            }

            string source = File.ReadAllText(path);
            foreach ((int start, string text) in Literals(source))
            {
                bool rowName = AllowedRowNames.Any(r => r.File == file) && source[..start].TrimEnd().EndsWith("new(", StringComparison.Ordinal);
                if (ReadsAsWords(text) && !rowName && !IsDeveloperContext(source, start) && !Allowed.Any(a => a.File == file && a.Text == text))
                {
                    found.Add((file, LineOf(source, start), text));
                }
            }
        }

        Assert.True(found.Count == 0, "NFR-032: move these user-facing strings into a resource file (or, if no user reads them, "
            + "add them to LocalizableStringTests.Allowed with the reason):" + Environment.NewLine
            + string.Join(Environment.NewLine, found.Select(f => $"  {f.File}:{f.Line}: \"{f.Text}\"")));
    }

    [Fact]
    public void TASK_104_every_allowed_literal_still_exists()
    {
        string src = Path.Combine(FindRepositoryRoot(), "src");
        var stale = Allowed.Where(a => !File.Exists(Path.Combine(src, a.File)) || !Literals(File.ReadAllText(Path.Combine(src, a.File))).Any(l => l.Text == a.Text))
            .Select(a => $"{a.File}: \"{a.Text}\"")
            .Concat(AllowedFiles.Concat(AllowedRowNames).Where(f => !File.Exists(Path.Combine(src, f.File))).Select(f => f.File))
            .ToList();

        Assert.True(stale.Count == 0, "Remove these entries from LocalizableStringTests:" + Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void TASK_104_AC_060_no_user_facing_text_is_hard_coded_in_XAML()
    {
        string src = Path.Combine(FindRepositoryRoot(), "src");
        var found = new List<string>();
        foreach (string path in SourceFiles(src, "*.xaml"))
        {
            string xaml = XamlComment().Replace(File.ReadAllText(path), m => new string('\n', m.Value.Count(c => c == '\n')));
            foreach (Match m in TextAttribute().Matches(xaml).Concat(ElementText().Matches(xaml)))
            {
                string value = m.Groups["v"].Value;
                if (Regex.IsMatch(System.Net.WebUtility.HtmlDecode(value), "[A-Za-z]"))
                {
                    found.Add($"  {Relative(src, path)}:{LineOf(xaml, m.Index)}: {m.Value.Trim()}");
                }
            }
        }

        Assert.True(found.Count == 0, "NFR-032: bind these texts to Strings (x:Static or DynamicResource):" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }

    [Theory]
    [InlineData("var s = \"Save the file first.\";", "Save the file first.", true)]
    [InlineData("var s = $\"Saved {name} to disk.\";", "Saved {} to disk.", true)]
    [InlineData("var s = @\"C:\\Tools \"\"quoted\"\" text\";", "C:\\Tools \"quoted\" text", true)]
    [InlineData("var s = $\"{a:X4}:{b:X4}\";", "{}:{}", false)]
    [InlineData("var s = \"INT 20h\";", "INT 20h", false)]
    [InlineData("// \"in a comment\"\nvar c = '\"'; var s = \"x\";", "x", false)]
    public void The_scanner_reads_literals_and_tells_words_from_notation(string source, string literal, bool words)
    {
        string text = Assert.Single(Literals(source)).Text;

        Assert.Equal(literal, text);
        Assert.Equal(words, ReadsAsWords(text));
    }

    [Theory]
    [InlineData("throw new InvalidOperationException(\"The map is empty.\");")]
    [InlineData("log.Write(LogLevel.Warning, \"The settings file could not be read.\");")]
    [InlineData("_log.Write(Logging.LogLevel.Warning, \"The layout could not be saved.\", ex);")]
    [InlineData("public override string ToString() => \"Suggested next step: none\";")]
    [InlineData("[GeneratedRegex(@\"NASM version (\\d+)\")]")]
    public void Logs_developer_exceptions_and_patterns_are_not_user_facing(string source)
    {
        (int start, _) = Assert.Single(Literals(source));

        Assert.True(IsDeveloperContext(source, start));
    }

    // Two words of two or more letters and a space: a sentence or a label, not a register, format or notation.
    private static bool ReadsAsWords(string text) => text.Contains(' ', StringComparison.Ordinal) && Word().Count(text) >= 2;

    // The statement the literal is in: a log entry, a trace, an exception for developers, a ToString for the debugger and
    // logs, an attribute or a regex. (File errors reach the user through FileErrors, which shows a Strings sentence.)
    private static bool IsDeveloperContext(string source, int start)
    {
        int from = source.LastIndexOfAny([';', '{', '}'], Math.Max(0, start - 1)) + 1;
        string statement = source[from..start];
        return statement.Contains("throw ", StringComparison.Ordinal)
            || statement.Contains("Exception(", StringComparison.Ordinal)
            || LogCall().IsMatch(statement)
            || statement.Contains("Trace.WriteLine(", StringComparison.Ordinal)
            || statement.Contains("override string ToString()", StringComparison.Ordinal)
            || statement.Contains("[GeneratedRegex(", StringComparison.Ordinal)
            || statement.Contains("[SuppressMessage(", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\.Write\((?:\w+\.)*LogLevel\.")]
    private static partial Regex LogCall();

    /// <summary>The string literals of a C# source with their start offsets; an interpolated string's holes are
    /// replaced by <c>{}</c> and the literals inside them are returned on their own. Comments and char literals are skipped.</summary>
    internal static IEnumerable<(int Start, string Text)> Literals(string source)
    {
        var result = new List<(int, string)>();
        int i = 0;
        while (i < source.Length)
        {
            i = Next(source, i, result);
        }

        return result;
    }

    private static int Next(string s, int i, List<(int, string)> result)
    {
        if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
        {
            int end = s.IndexOf('\n', i);
            return end < 0 ? s.Length : end;
        }

        if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*')
        {
            int end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
            return end < 0 ? s.Length : end + 2;
        }

        if (s[i] == '\'')
        {
            int j = i + 1;
            while (j < s.Length && s[j] != '\'')
            {
                j += s[j] == '\\' ? 2 : 1;
            }

            return j + 1;
        }

        int k = i;
        bool interpolated = false, verbatim = false;
        while (k < s.Length && (s[k] == '$' || s[k] == '@'))
        {
            interpolated |= s[k] == '$';
            verbatim |= s[k] == '@';
            k++;
        }

        if (k >= s.Length || s[k] != '"')
        {
            return k == i ? i + 1 : k;
        }

        return ReadString(s, i, k + 1, interpolated, verbatim, result);
    }

    private static int ReadString(string s, int start, int j, bool interpolated, bool verbatim, List<(int, string)> result)
    {
        var text = new StringBuilder();
        while (j < s.Length)
        {
            char c = s[j];
            if (c == '"')
            {
                if (verbatim && j + 1 < s.Length && s[j + 1] == '"')
                {
                    text.Append('"');
                    j += 2;
                    continue;
                }

                result.Add((start, text.ToString()));
                return j + 1;
            }

            if (!verbatim && c == '\\' && j + 1 < s.Length)
            {
                text.Append(s[j + 1] switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '0' => '\0', var e => e });
                j += 2;
                continue;
            }

            if (interpolated && c == '{')
            {
                if (j + 1 < s.Length && s[j + 1] == '{')
                {
                    text.Append('{');
                    j += 2;
                    continue;
                }

                text.Append("{}");
                j = SkipHole(s, j + 1, result);
                continue;
            }

            if (interpolated && c == '}' && j + 1 < s.Length && s[j + 1] == '}')
            {
                text.Append('}');
                j += 2;
                continue;
            }

            text.Append(c);
            j++;
        }

        return j;
    }

    // Skips an interpolation hole (its format specifier included), collecting the literals inside it.
    private static int SkipHole(string s, int j, List<(int, string)> result)
    {
        int depth = 1;
        while (j < s.Length && depth > 0)
        {
            char c = s[j];
            if (c is '"' or '$' or '@' or '\'')
            {
                j = Next(s, j, result);
                continue;
            }

            depth += c == '{' ? 1 : c == '}' ? -1 : 0;
            j++;
        }

        return j;
    }

    private static IEnumerable<string> SourceFiles(string src, string pattern) =>
        Directory.EnumerateFiles(src, pattern, SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string Relative(string src, string path) => Path.GetRelativePath(src, path).Replace('\\', '/');

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AssemblyStudio.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException($"AssemblyStudio.slnx not found above {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex("[A-Za-z]{2,}")]
    private static partial Regex Word();

    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex XamlComment();

    [GeneratedRegex(@"\b(?:Text|Content|Header|ToolTip|Title|AutomationProperties\.Name|AutomationProperties\.HelpText|AutomationProperties\.ItemStatus|InputGestureText)=""(?<v>[^""{][^""]*)""")]
    private static partial Regex TextAttribute();

    [GeneratedRegex(@">(?<v>[^<>]*[^<>\s][^<>]*)</(?:TextBlock|Run|Label|Button|MenuItem|CheckBox|RadioButton|Hyperlink|Paragraph|Bold|Italic|Span|ToolTip|GroupBox|TabItem|sys:String|x:String)>")]
    private static partial Regex ElementText();
}
