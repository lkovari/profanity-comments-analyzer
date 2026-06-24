using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class ProfanityMatcherLanguageTests
{
    [Fact]
    public void English_spot_checks()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["en"]);
        Assert.NotEmpty(ProfanityMatcher.FindMatches("damn this bug again", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("I'll kill whoever wrote this", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("what a dumbass mistake", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void Hungarian_spot_checks()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["hu"]);
        Assert.NotEmpty(ProfanityMatcher.FindMatches("szar ez az egész", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("bassza meg ezt a kódot", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("dunába lőném ha még egyszer", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void German_spot_checks()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["de"]);
        Assert.NotEmpty(ProfanityMatcher.FindMatches("so ein Mist", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("ich bringe dich um", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void Romanian_spot_checks()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["ro"]);
        Assert.NotEmpty(ProfanityMatcher.FindMatches("ce prost esti", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("căcat de cod", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("te omor daca mai scrii", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void Italian_spot_checks()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["it"]);
        Assert.NotEmpty(ProfanityMatcher.FindMatches("vaffanculo che codice", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("che cazzo combini", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("non capisco questo", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void Critique_spot_checks()
    {
        var patterns = LanguageRegistry.AllPatterns;
        Assert.NotEmpty(ProfanityMatcher.FindMatches("gyatra megoldás", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("dumpster fire", Severity.Mild, [], patterns, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("nicht wartbar", Severity.Mild, [], patterns, []));
    }

    [Fact]
    public void Allow_list_skips_exact_match()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["en"]);
        var matches = ProfanityMatcher.FindMatches("what the hack is going on", Severity.Mild, ["hack"], patterns, []);
        Assert.Empty(matches);
    }

    [Fact]
    public void Min_severity_filters_matches()
    {
        var patterns = LanguageRegistry.GetPatternsForLanguages(["en"]);
        var matches = ProfanityMatcher.FindMatches("damn this bug", Severity.Severe, [], patterns, []);
        Assert.Empty(matches);
    }

    [Fact]
    public void Language_filter_limits_patterns()
    {
        var english = LanguageRegistry.GetPatternsForLanguages(["en"]);
        var hungarian = LanguageRegistry.GetPatternsForLanguages(["hu"]);
        Assert.Empty(ProfanityMatcher.FindMatches("szar ez az egész", Severity.Mild, [], english, []));
        Assert.NotEmpty(ProfanityMatcher.FindMatches("szar ez az egész", Severity.Mild, [], hungarian, []));
    }
}
