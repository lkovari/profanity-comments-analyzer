using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class ProfanityMatcherTests
{
    private static readonly CompiledPattern MildPattern = ProfanityMatcher.CompilePatterns(
        [new ProfanityEntry("test", @"\btest\b", Severity.Mild)],
        "en")[0];

    private static readonly CompiledPattern SeverePattern = ProfanityMatcher.CompilePatterns(
        [new ProfanityEntry("kill", @"\bkill\b", Severity.Severe, Category.Threat)],
        "en")[0];

    [Fact]
    public void Filters_by_minimum_severity()
    {
        var matches = ProfanityMatcher.FindMatches(
            "test and kill",
            Severity.Severe,
            [],
            [MildPattern, SeverePattern],
            []);

        Assert.Single(matches);
        Assert.Equal("kill", matches[0].Matched);
    }

    [Fact]
    public void Respects_allow_list_case_insensitive()
    {
        var matches = ProfanityMatcher.FindMatches(
            "TEST value",
            Severity.Mild,
            ["test"],
            [MildPattern],
            []);

        Assert.Empty(matches);
    }

    [Fact]
    public void Finds_multiple_matches()
    {
        var matches = ProfanityMatcher.FindMatches(
            "test test",
            Severity.Mild,
            [],
            [MildPattern],
            []);

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void Is_case_insensitive()
    {
        var matches = ProfanityMatcher.FindMatches(
            "TeSt",
            Severity.Mild,
            [],
            [MildPattern],
            []);

        Assert.Single(matches);
        Assert.Equal("TeSt", matches[0].Matched);
    }

    [Fact]
    public void Allow_list_does_not_block_unlisted_words()
    {
        var matches = ProfanityMatcher.FindMatches(
            "TEST value",
            Severity.Mild,
            ["other"],
            [MildPattern],
            []);

        Assert.Single(matches);
    }

    [Fact]
    public void Includes_extra_patterns()
    {
        var extra = ProfanityMatcher.CompilePatterns(
            [new ProfanityEntry("yikes", @"\byikes\b", Severity.Mild)]);

        var matches = ProfanityMatcher.FindMatches(
            "yikes",
            Severity.Mild,
            [],
            [],
            extra);

        Assert.Single(matches);
        Assert.Equal("custom", matches[0].Lang);
    }
}
