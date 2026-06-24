using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests.Languages;

public class EmbeddedLanguageTests
{
    [Theory]
    [InlineData("en", 190)]
    [InlineData("hu", 219)]
    [InlineData("de", 143)]
    [InlineData("ro", 145)]
    [InlineData("it", 148)]
    public void Has_minimum_entry_count(string code, int minimumEntries)
    {
        var language = LanguageRegistry.GetLanguage(code);
        Assert.NotNull(language);
        Assert.True(language!.Entries.Count >= minimumEntries);
    }
}
