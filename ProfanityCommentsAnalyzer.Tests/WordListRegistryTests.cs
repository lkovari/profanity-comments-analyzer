using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ProfanityCommentsAnalyzer.Models;
using Xunit;

namespace ProfanityCommentsAnalyzer.Tests;

public class WordListRegistryTests
{
    [Fact]
    public void Loads_embedded_defaults()
    {
        var registry = WordListRegistry.LoadDefaults();

        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
        Assert.True(registry.GetLanguage("en")?.Entries.Count >= 190);
    }

    [Fact]
    public void Loads_extra_patterns_file()
    {
        const string json = """
            {
              "entries": [
                {
                  "word": "yikes",
                  "pattern": "\\byikes\\b",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity/extra-patterns.json", json)]);

        Assert.Contains(registry.ExtraPatterns, pattern => pattern.Entry.Word == "yikes");
    }

    [Fact]
    public void Overrides_language_file()
    {
        const string json = """
            {
              "code": "hu",
              "name": "Hungarian",
              "entries": [
                {
                  "word": "custom hu",
                  "pattern": "custom\\s+hu",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity/hu.json", json)]);

        Assert.Single(registry.GetLanguage("hu")!.Entries);
        Assert.Equal("custom hu", registry.GetLanguage("hu")!.Entries[0].Word);
    }

    [Fact]
    public void Languages_manifest_can_remove_language()
    {
        const string manifest = """
            {
              "languages": ["en", "de"]
            }
            """;

        var registry = LoadFromFiles([("profanity/languages.json", manifest)]);

        Assert.Equal(["en", "de"], registry.RegisteredLanguages);
        Assert.Null(registry.GetLanguage("hu"));
    }

    [Fact]
    public void Loads_custom_language_from_json()
    {
        const string json = """
            {
              "code": "fr",
              "name": "French",
              "entries": [
                {
                  "word": "merde",
                  "pattern": "\\bm[e3]rd[e3]\\b",
                  "severity": "moderate",
                  "category": "profanity"
                }
              ]
            }
            """;

        const string manifest = """
            {
              "languages": ["en", "fr"]
            }
            """;

        var registry = LoadFromFiles([
            ("profanity/languages.json", manifest),
            ("profanity/fr.json", json),
        ]);

        Assert.Equal(["en", "fr"], registry.RegisteredLanguages);
        Assert.Single(registry.GetLanguage("fr")!.Entries);
    }

    [Fact]
    public void Throws_when_language_file_is_invalid_json()
    {
        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/bad.json", "{not json")]));

        Assert.Contains("bad.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not valid JSON", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_language_file_is_empty()
    {
        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", "   ")]));

        Assert.Contains("en.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Loads_legacy_extra_patterns_document()
    {
        const string json = """
            {
              "extraPatterns": [
                {
                  "word": "custom phrase",
                  "pattern": "custom\\s+phrase",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity-words.custom.json", json)]);

        Assert.Contains(registry.ExtraPatterns, pattern => pattern.Entry.Word == "custom phrase");
    }

    [Fact]
    public void Legacy_language_document_overrides_embedded_language()
    {
        const string json = """
            {
              "code": "en",
              "name": "English",
              "entries": [
                {
                  "word": "override",
                  "pattern": "override",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity-words.custom.json", json)]);

        Assert.Single(registry.GetLanguage("en")!.Entries);
        Assert.Equal("override", registry.GetLanguage("en")!.Entries[0].Word);
    }

    [Fact]
    public void GetPatternsForLanguages_null_returns_all()
    {
        var registry = WordListRegistry.LoadDefaults();
        Assert.Same(registry.AllPatterns, registry.GetPatternsForLanguages(null));
    }

    [Fact]
    public void Ignores_empty_extra_patterns_file()
    {
        var registry = LoadFromFiles([("profanity/extra-patterns.json", "   ")]);
        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
        Assert.Empty(registry.ExtraPatterns);
    }

    [Fact]
    public void Throws_when_manifest_contains_blank_language_code()
    {
        const string manifest = """
            {
              "languages": ["en", "  "]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/languages.json", manifest)]));

        Assert.Contains("must not be empty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Skips_root_language_file_without_code()
    {
        const string json = """
            {
              "name": "English",
              "entries": [
                {
                  "word": "x",
                  "pattern": "x",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("en.json", json)]);
        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
        Assert.True(registry.GetLanguage("en")?.Entries.Count >= 190);
    }

    [Fact]
    public void Skips_empty_legacy_word_list_file()
    {
        var registry = LoadFromFiles([("profanity-words.custom.json", "   ")]);
        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
    }

    [Fact]
    public void Skips_legacy_language_document_missing_name()
    {
        const string json = """
            {
              "code": "en",
              "entries": [
                {
                  "word": "x",
                  "pattern": "x",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity-words.custom.json", json)]);
        Assert.True(registry.GetLanguage("en")?.Entries.Count >= 190);
    }

    [Fact]
    public void Throws_when_language_file_is_null_document()
    {
        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", "null")]));

        Assert.Contains("en.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Throws_when_manifest_file_has_null_text()
    {
        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFilesWithNullText([("profanity/languages.json", null)]));

        Assert.Contains("languages.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ignores_non_word_list_additional_files()
    {
        var registry = LoadFromFiles([("appsettings.json", "{}")]);
        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
    }

    [Fact]
    public void Throws_when_language_file_has_invalid_severity()
    {
        const string json = """
            {
              "code": "en",
              "name": "English",
              "entries": [
                {
                  "word": "broken",
                  "pattern": "broken",
                  "severity": "not-a-level"
                }
              ]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", json)]));

        Assert.Contains("en.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not valid JSON", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetLanguage_returns_null_for_unregistered_code()
    {
        var registry = WordListRegistry.LoadDefaults();
        Assert.Null(registry.GetLanguage("fr"));
    }

    [Fact]
    public void Loads_extra_patterns_using_extraPatterns_property()
    {
        const string json = """
            {
              "extraPatterns": [
                {
                  "word": "team phrase",
                  "pattern": "team\\s+phrase",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([("profanity/extra-patterns.json", json)]);
        Assert.Contains(registry.ExtraPatterns, pattern => pattern.Entry.Word == "team phrase");
    }

    [Fact]
    public void Throws_when_language_file_code_does_not_match_file_name()
    {
        const string json = """
            {
              "code": "xx",
              "name": "Invalid",
              "entries": [
                {
                  "word": "nope",
                  "pattern": "nope",
                  "severity": "mild"
                }
              ]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/not-a-language.json", json)]));

        Assert.Contains("must match the file name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Caches_filtered_patterns_per_registry_instance()
    {
        var registry = WordListRegistry.LoadDefaults();
        var first = registry.GetPatternsForLanguages(["de", "en"]);
        var second = registry.GetPatternsForLanguages(["en", "de"]);
        Assert.Same(first, second);
    }

    [Fact]
    public void Throws_when_entry_is_missing_word_or_pattern()
    {
        const string json = """
            {
              "code": "en",
              "name": "English",
              "entries": [
                {
                  "pattern": "orphan",
                  "severity": "mild"
                }
              ]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", json)]));

        Assert.Contains("en.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("index 0", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Loads_files_from_root_level_json_names()
    {
        const string manifest = """
            {
              "languages": ["en"]
            }
            """;

        const string english = """
            {
              "code": "en",
              "name": "English",
              "entries": [
                {
                  "word": "root override",
                  "pattern": "root\\s+override",
                  "severity": "mild"
                }
              ]
            }
            """;

        var registry = LoadFromFiles([
            ("languages.json", manifest),
            ("en.json", english),
        ]);

        Assert.Equal(["en"], registry.RegisteredLanguages);
        Assert.Single(registry.GetLanguage("en")!.Entries);
    }

    [Fact]
    public void Throws_when_manifest_is_empty()
    {
        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("languages.json", "   ")]));

        Assert.Contains("languages.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Throws_when_manifest_has_no_languages()
    {
        const string manifest = """
            {
              "languages": []
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("languages.json", manifest)]));

        Assert.Contains("does not list any languages", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_manifest_references_missing_language_file()
    {
        const string manifest = """
            {
              "languages": ["en", "fr"]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/languages.json", manifest)]));

        Assert.Contains("fr", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fr.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Throws_when_language_file_has_no_entries()
    {
        const string json = """
            {
              "code": "en",
              "name": "English",
              "entries": []
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", json)]));

        Assert.Contains("en.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("entries", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_language_code_does_not_match_file_name()
    {
        const string json = """
            {
              "code": "hu",
              "name": "Hungarian",
              "entries": [
                {
                  "word": "x",
                  "pattern": "x",
                  "severity": "mild"
                }
              ]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("profanity/en.json", json)]));

        Assert.Contains("must match the file name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Throws_when_manifest_languages_is_null()
    {
        const string manifest = """
            {
              "languages": null
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("languages.json", manifest)]));

        Assert.Contains("does not list any languages", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Skips_non_language_json_outside_profanity_folder()
    {
        var registry = LoadFromFiles([("custom-data.json", """{"value":1}""")]);
        Assert.Equal(["en", "hu", "de", "ro", "it"], registry.RegisteredLanguages);
    }

    [Fact]
    public void Throws_when_root_language_file_code_mismatches_file_name()
    {
        const string json = """
            {
              "code": "hu",
              "name": "Hungarian",
              "entries": [
                {
                  "word": "x",
                  "pattern": "x",
                  "severity": "mild"
                }
              ]
            }
            """;

        var exception = Assert.Throws<WordListLoadException>(() =>
            LoadFromFiles([("en.json", json)]));

        Assert.Contains("must match the file name", exception.Message, StringComparison.Ordinal);
    }

    private static WordListRegistry LoadFromFiles((string path, string content)[] files)
    {
        var additionalFiles = files
            .Select(file => (AdditionalText)new InMemoryAdditionalText(file.path, file.content))
            .ToList();

        return WordListRegistry.Load(additionalFiles);
    }

    private static WordListRegistry LoadFromFilesWithNullText((string path, string? content)[] files)
    {
        var additionalFiles = files
            .Select(file => (AdditionalText)new NullTextAdditionalText(file.path, file.content))
            .ToList();

        return WordListRegistry.Load(additionalFiles);
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }

    private sealed class NullTextAdditionalText : AdditionalText
    {
        public NullTextAdditionalText(string path, string? content)
        {
            Path = path;
            _content = content;
        }

        private readonly string? _content;

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            _content is null ? null : SourceText.From(_content);
    }
}
