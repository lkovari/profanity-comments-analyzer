using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class AnalyzerConfigTests
{
    private const string Source = """
        public class Sample
        {
            // damn this bug
        }
        """;

    [Fact]
    public Task Default_flags_mild_profanity()
    {
        return AnalyzerTestHelper.VerifyDiagnosticCount(Source, 1);
    }

    [Fact]
    public Task Min_severity_severe_skips_mild()
    {
        return AnalyzerTestHelper.VerifyDiagnosticCount(
            Source,
            0,
            AnalyzerTestHelper.GlobalConfig("profanity_comments_analyzer.min_severity = severe"));
    }

    [Fact]
    public Task Languages_filter_limits_matches()
    {
        return AnalyzerTestHelper.VerifyDiagnosticCount(
            Source,
            0,
            AnalyzerTestHelper.GlobalConfig("profanity_comments_analyzer.languages = hu"));
    }

    [Fact]
    public Task Allow_list_permits_matching_word()
    {
        const string hackSource = """
            public class Sample
            {
                // what the hack is going on
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(
            hackSource,
            0,
            AnalyzerTestHelper.GlobalConfig("profanity_comments_analyzer.allow_list = hack"));
    }

    [Fact]
    public Task Min_severity_moderate_skips_mild()
    {
        return AnalyzerTestHelper.VerifyDiagnosticCount(
            Source,
            0,
            AnalyzerTestHelper.GlobalConfig("profanity_comments_analyzer.min_severity = moderate"));
    }

    [Fact]
    public Task Blank_list_values_use_defaults()
    {
        return AnalyzerTestHelper.VerifyDiagnosticCount(
            Source,
            1,
            AnalyzerTestHelper.GlobalConfig(
                "profanity_comments_analyzer.min_severity = ",
                "profanity_comments_analyzer.languages = ",
                "profanity_comments_analyzer.allow_list = "));
    }
}
