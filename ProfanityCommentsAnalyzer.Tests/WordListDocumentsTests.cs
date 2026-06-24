using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class WordListDocumentsTests
{
    [Fact]
    public void ToEntry_returns_null_when_word_or_pattern_missing()
    {
        Assert.Null(new ProfanityEntryDocument { Pattern = "x", Severity = Severity.Mild }.ToEntry());
        Assert.Null(new ProfanityEntryDocument { Word = "x", Severity = Severity.Mild }.ToEntry());
        Assert.NotNull(new ProfanityEntryDocument { Word = "x", Pattern = "x", Severity = Severity.Mild }.ToEntry());
    }

    [Fact]
    public void ToDefinitionStrict_throws_for_missing_code()
    {
        var document = new LanguageDocument
        {
            Name = "English",
            Entries =
            [
                new ProfanityEntryDocument { Word = "x", Pattern = "x", Severity = Severity.Mild },
            ],
        };

        var exception = Assert.Throws<WordListLoadException>(() =>
            document.ToDefinitionStrict("en.json"));

        Assert.Contains("code", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDefinitionStrict_throws_when_entries_null()
    {
        var document = new LanguageDocument
        {
            Code = "en",
            Name = "English",
            Entries = null,
        };

        var exception = Assert.Throws<WordListLoadException>(() =>
            document.ToDefinitionStrict("en.json"));

        Assert.Contains("entries", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtraPatternsDocument_returns_empty_when_unset()
    {
        Assert.Empty(new ExtraPatternsDocument().ToEntries());
    }

    [Fact]
    public void ExtraPatternsDocument_skips_incomplete_entries()
    {
        var document = new ExtraPatternsDocument
        {
            Entries =
            [
                new ProfanityEntryDocument { Word = null, Pattern = "x", Severity = Severity.Mild },
                new ProfanityEntryDocument { Word = "ok", Pattern = "ok", Severity = Severity.Mild },
            ],
        };

        Assert.Single(document.ToEntries());
    }

    [Fact]
    public void ToDefinitionStrict_throws_for_incomplete_entry()
    {
        var document = new LanguageDocument
        {
            Code = "en",
            Name = "English",
            Entries =
            [
                new ProfanityEntryDocument { Word = null, Pattern = "x", Severity = Severity.Mild },
            ],
        };

        var exception = Assert.Throws<WordListLoadException>(() =>
            document.ToDefinitionStrict("en.json"));

        Assert.Contains("index 0", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDefinitionStrict_throws_without_required_fields()
    {
        var document = new LanguageDocument
        {
            Code = "en",
            Entries = [],
        };

        var exception = Assert.Throws<WordListLoadException>(() =>
            document.ToDefinitionStrict("en.json"));

        Assert.Contains("name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtraPatternsDocument_reads_extraPatterns_property()
    {
        var document = new ExtraPatternsDocument
        {
            ExtraPatterns =
            [
                new ProfanityEntryDocument { Word = "x", Pattern = "x", Severity = Severity.Mild },
            ],
        };

        Assert.Single(document.ToEntries());
    }
}
