using System.Text.RegularExpressions;
using ProfanityCommentsAnalyzer.Models;

namespace ProfanityCommentsAnalyzer;

public sealed record CompiledPattern(ProfanityEntry Entry, Regex Regex, string Lang);

public sealed record ProfanityMatch(ProfanityEntry Entry, string Matched, string? Lang);

public static class ProfanityMatcher
{
    private const RegexOptions PatternOptions =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    public static IReadOnlyList<ProfanityMatch> FindMatches(
        string text,
        Severity minSeverity,
        IReadOnlyList<string> allowList,
        IReadOnlyList<CompiledPattern> compiledPatterns,
        IReadOnlyList<CompiledPattern> extraPatterns)
    {
        var minRank = (int)minSeverity;
        var results = new List<ProfanityMatch>();

        ScanPatterns(text, minRank, allowList, compiledPatterns, results);
        ScanPatterns(text, minRank, allowList, extraPatterns, results);

        return results;
    }

    public static IReadOnlyList<CompiledPattern> CompilePatterns(
        IEnumerable<ProfanityEntry> entries,
        string? lang = null)
    {
        return entries
            .Select(entry => new CompiledPattern(
                entry,
                new Regex(entry.Pattern, PatternOptions),
                lang ?? "custom"))
            .ToList();
    }

    private static void ScanPatterns(
        string text,
        int minRank,
        IReadOnlyList<string> allowList,
        IReadOnlyList<CompiledPattern> patterns,
        List<ProfanityMatch> results)
    {
        foreach (var compiled in patterns)
        {
            if ((int)compiled.Entry.Severity < minRank)
            {
                continue;
            }

            foreach (Match match in compiled.Regex.Matches(text))
            {
                var matched = match.Value;
                if (IsAllowed(matched, allowList))
                {
                    continue;
                }

                results.Add(new ProfanityMatch(compiled.Entry, matched, compiled.Lang));
            }
        }
    }

    private static bool IsAllowed(string matched, IReadOnlyList<string> allowList)
    {
        foreach (var allowed in allowList)
        {
            if (string.Equals(allowed, matched, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
