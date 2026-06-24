using Microsoft.CodeAnalysis.Diagnostics;
using ProfanityCommentsAnalyzer.Models;

namespace ProfanityCommentsAnalyzer;

public sealed class AnalyzerConfig
{
    public const string MinSeverityKey = "profanity_comments_analyzer.min_severity";
    public const string LanguagesKey = "profanity_comments_analyzer.languages";
    public const string AllowListKey = "profanity_comments_analyzer.allow_list";

    public Severity MinSeverity { get; init; } = Severity.Mild;

    public IReadOnlyList<string> Languages { get; init; } =
        ["en", "hu", "de", "ro", "it"];

    public IReadOnlyList<string> AllowList { get; init; } = [];

    public static AnalyzerConfig FromOptions(AnalyzerOptions options)
    {
        var configOptions = options.AnalyzerConfigOptionsProvider.GlobalOptions;

        var minSeverity = ParseSeverity(
            configOptions.TryGetValue(MinSeverityKey, out var minSeverityValue)
                ? minSeverityValue
                : null);

        var languages = ParseList(
            configOptions.TryGetValue(LanguagesKey, out var languagesValue)
                ? languagesValue
                : null,
            ["en", "hu", "de", "ro", "it"]);

        var allowList = ParseList(
            configOptions.TryGetValue(AllowListKey, out var allowListValue)
                ? allowListValue
                : null,
            []);

        return new AnalyzerConfig
        {
            MinSeverity = minSeverity,
            Languages = languages,
            AllowList = allowList,
        };
    }

    private static Severity ParseSeverity(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "moderate" => Severity.Moderate,
            "severe" => Severity.Severe,
            _ => Severity.Mild,
        };
    }

    private static IReadOnlyList<string> ParseList(string? value, IReadOnlyList<string> fallback)
    {
        if (value is null || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .ToList();
    }
}
