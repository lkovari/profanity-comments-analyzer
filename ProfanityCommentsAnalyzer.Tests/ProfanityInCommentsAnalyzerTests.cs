using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class ProfanityInCommentsAnalyzerTests
{
    [Fact]
    public Task Flags_six_known_comments_in_fixture()
    {
        const string source = """
            public class Sample
            {
                // dunába lőném ha még egyszer ilyet leír
                // letöröm a kezét ha ilyet mégegyszer leír
                // bassza meg az ilyen fejlesztő
                // I'll kill whoever wrote this
                // should be shot for this code
                // wtf is this shit
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 9);
    }

    [Fact]
    public Task Does_not_flag_string_literals()
    {
        const string source = """
            public class Sample
            {
                private string value = "wtf is this shit";
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 0);
    }

    [Fact]
    public Task Does_not_flag_clean_comments()
    {
        const string source = """
            public class Sample
            {
                // clean and well structured code
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 0);
    }

    [Fact]
    public Task Loads_additional_words_from_json_file()
    {
        const string source = """
            public class Sample
            {
                // custom phrase here
            }
            """;

        const string json = """
            {
              "extraPatterns": [
                {
                  "word": "custom phrase",
                  "pattern": "custom\\s+phrase",
                  "severity": "Mild"
                }
              ]
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(
            source,
            1,
            additionalFiles: [("profanity-words.custom.json", json)]);
    }

    [Fact]
    public Task Flags_multiline_comment()
    {
        const string source = """
            public class Sample
            {
                /* what a piece of shit */
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 1);
    }

    [Fact]
    public Task Flags_single_line_documentation_comment()
    {
        const string source = """
            public class Sample
            {
                /// damn this documented API
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 1);
    }

    [Fact]
    public Task Flags_multiline_documentation_comment()
    {
        const string source = """
            public class Sample
            {
                /**
                 * damn this documented API
                 */
                public void Run() { }
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 1);
    }

    [Fact]
    public Task Skips_short_and_non_cs_file_paths()
    {
        const string source = """
            public class Sample
            {
                // damn this bug
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 0, filePath: "ab");
    }

    [Fact]
    public Task Skips_non_cs_files()
    {
        const string source = """
            public class Sample
            {
                // damn this bug
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(source, 0, filePath: "Sample.vb");
    }

    [Fact]
    public Task Reports_word_list_load_failure_when_languages_json_references_missing_file()
    {
        const string source = """
            public class Sample
            {
                // clean comment
            }
            """;

        const string manifest = """
            {
              "languages": ["en", "fr"]
            }
            """;

        return AnalyzerTestHelper.AssertAnalyzerReportsWordListLoadFailureAsync(
            source,
            "fr.json",
            additionalFiles: [("profanity/languages.json", manifest)]);
    }

    [Fact]
    public Task Matches_extra_patterns_when_languages_filtered_to_english_only()
    {
        const string source = """
            public class Sample
            {
                // team banned phrase here
            }
            """;

        const string extraPatterns = """
            {
              "entries": [
                {
                  "word": "team banned phrase",
                  "pattern": "team\\s+banned\\s+phrase",
                  "severity": "moderate"
                }
              ]
            }
            """;

        return AnalyzerTestHelper.VerifyDiagnosticCount(
            source,
            1,
            AnalyzerTestHelper.GlobalConfig("profanity_comments_analyzer.languages = en"),
            additionalFiles: [("profanity/extra-patterns.json", extraPatterns)]);
    }

    [Fact]
    public Task Reports_exact_pca001_message_and_location()
    {
        const string source = """
            public class Sample
            {
                // damn this legacy API
            }
            """;

        return AnalyzerTestHelper.VerifyPca001(
            source,
            startLine: 3,
            startColumn: 5,
            endLine: 3,
            endColumn: 28,
            matched: "damn",
            language: "en",
            severity: "mild");
    }
}
