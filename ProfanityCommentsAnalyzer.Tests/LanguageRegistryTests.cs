using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class LanguageRegistryTests
{
    [Fact]
    public void Registers_five_languages()
    {
        Assert.Equal(["en", "hu", "de", "ro", "it"], LanguageRegistry.RegisteredLanguages);
    }

    [Fact]
    public void Filters_patterns_by_language()
    {
        var englishOnly = LanguageRegistry.GetPatternsForLanguages(["en"]);
        var hungarianOnly = LanguageRegistry.GetPatternsForLanguages(["hu"]);

        Assert.NotEqual(englishOnly.Count, hungarianOnly.Count);
        Assert.True(englishOnly.Count > 0);
        Assert.True(hungarianOnly.Count > 0);
    }

    [Fact]
    public void Returns_all_patterns_when_codes_null()
    {
        var all = LanguageRegistry.AllPatterns;
        var filtered = LanguageRegistry.GetPatternsForLanguages(null);

        Assert.Equal(all.Count, filtered.Count);
    }

    [Fact]
    public void Caches_filtered_patterns()
    {
        var first = LanguageRegistry.GetPatternsForLanguages(["de", "en"]);
        var second = LanguageRegistry.GetPatternsForLanguages(["en", "de"]);

        Assert.Equal(first.Count, second.Count);
        Assert.Same(first, second);
    }
}
