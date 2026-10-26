using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Poc.Mapping;

/// <summary>Independent checks of a <see cref="SourceMap"/> against the binary, the map symbols and ndisasm.</summary>
public static partial class Verification
{
    [GeneratedRegex(@"^\s*(?<name>[A-Za-z_.?@$][\w.?@$#~]*)\s*(?::|\s+(?:db|dw|dd|times|resb|resw)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex LabelDefinition();

    [GeneratedRegex(@"\[\s*cpu\s+(?<level>[^\]\s]+)\s*\]", RegexOptions.IgnoreCase)]
    private static partial Regex CpuDirective();

    /// <summary>Every known listing byte equals the image byte at the mapped address.</summary>
    public static List<string> CheckBytes(SourceMap sm, byte[] image)
    {
        var problems = new List<string>();
        foreach (var r in sm.Ranges.Where(r => !r.IsReserved))
        {
            int fileStart = r.Address - sm.Map.Origin;   // bin: Start == Vstart here (no vstart/follows used)
            for (int i = 0; i < r.Bytes.Count; i++)
            {
                if (r.Bytes[i] is not byte expected) continue;           // relocated value: final only in the image
                int pos = fileStart + i;
                if (pos < 0 || pos >= image.Length) { problems.Add($"{r.Address + i:X4} outside image ({r.Location})"); break; }
                if (image[pos] != expected) problems.Add($"{r.Address + i:X4}: image {image[pos]:X2} != listing {expected:X2} ({r.Location})");
            }
        }
        return problems;
    }

    /// <summary>Each label's map-file address equals the first mapped range at/after the label's listing row in the same section.</summary>
    public static List<string> CheckSymbols(SourceMap sm)
    {
        var problems = new List<string>();
        foreach (var row in sm.Rows.Where(r => r.ErrorText is null))
        {
            var m = LabelDefinition().Match(row.Source);
            if (!m.Success || !sm.Map.Symbols.TryGetValue(m.Groups["name"].Value, out int symbolAddress)) continue;
            var next = sm.Ranges.Where(r => r.ListingIndex >= row.Index).OrderBy(r => r.ListingIndex).FirstOrDefault();
            if (next is null) continue;                                   // label at the very end: nothing follows
            if (next.Address != symbolAddress)
                problems.Add($"symbol {m.Groups["name"].Value}: map {symbolAddress:X4} != mapped {next.Address:X4}");
        }
        return problems;
    }

    /// <summary>Every mapped range in the code section starts on an instruction boundary according to ndisasm.</summary>
    public static List<string> CheckInstructionBoundaries(SourceMap sm, byte[] image, string ndisasmPath, string codeSection = ".text")
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"poc002-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(tmp, image);
        try
        {
            var psi = new ProcessStartInfo(ndisasmPath) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            foreach (string a in new[] { "-b", "16", "-o", sm.Map.Origin.ToString("X", CultureInfo.InvariantCulture) + "h", tmp }) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            var starts = output.Split('\n').Where(l => l.Length >= 8)
                .Select(l => int.TryParse(l.AsSpan(0, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int a) ? a : -1)
                .Where(a => a >= 0).ToHashSet();
            if (!sm.Map.Sections.TryGetValue(codeSection, out var code)) return [];
            int codeEnd = sm.Map.Sections.Values.Where(s => s.Vstart > code.Vstart).Select(s => s.Vstart).DefaultIfEmpty(int.MaxValue).Min();
            return sm.Ranges.Where(r => r.Section == codeSection && r.Address < codeEnd && !starts.Contains(r.Address))
                            .Select(r => $"{r.Address:X4} ({r.Location}) is not an instruction start per ndisasm").ToList();
        }
        finally { File.Delete(tmp); }
    }

    /// <summary>CPU levels set by any <c>cpu</c> directive after preprocessing (catches macros and includes).</summary>
    public static IReadOnlyList<string> CpuDirectives(string preprocessed) =>
        CpuDirective().Matches(preprocessed).Select(m => m.Groups["level"].Value).ToList();
}
