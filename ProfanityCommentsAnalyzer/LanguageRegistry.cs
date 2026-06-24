using ProfanityCommentsAnalyzer.Models;

namespace ProfanityCommentsAnalyzer;

public static class LanguageRegistry
{
    private static readonly WordListRegistry Defaults = WordListRegistry.LoadDefaults();

    public static IReadOnlyList<string> RegisteredLanguages => Defaults.RegisteredLanguages;

    public static IReadOnlyList<CompiledPattern> AllPatterns => Defaults.AllPatterns;

    public static IReadOnlyList<CompiledPattern> GetPatternsForLanguages(IEnumerable<string>? codes) =>
        Defaults.GetPatternsForLanguages(codes);

    public static LanguageDefinition? GetLanguage(string code) => Defaults.GetLanguage(code);
}
