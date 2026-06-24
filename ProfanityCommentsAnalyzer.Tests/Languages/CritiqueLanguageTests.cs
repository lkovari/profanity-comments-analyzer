using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests.Languages;

public class CritiqueLanguageTests
{
    [Fact]
    public void Critique_entries_have_minimum_count()
    {
        var total = LanguageRegistry.RegisteredLanguages
            .Select(code => LanguageRegistry.GetLanguage(code))
            .Where(language => language is not null)
            .SelectMany(language => language!.Entries)
            .Count(entry => entry.Category == Category.PoorCode
                || entry.Category == Category.BadPractice
                || entry.Category == Category.Confusion);

        Assert.True(total >= 203);
    }
}
