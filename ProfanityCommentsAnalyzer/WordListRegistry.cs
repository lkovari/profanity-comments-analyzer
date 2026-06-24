using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ProfanityCommentsAnalyzer.Models;

namespace ProfanityCommentsAnalyzer;

public sealed class WordListRegistry
{
    private static readonly Regex LanguageFileNamePattern = new(
        @"^[a-z0-9-]{2,16}\.json$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly IReadOnlyList<string> _registeredLanguages;
    private readonly IReadOnlyDictionary<string, LanguageDefinition> _languages;
    private readonly IReadOnlyList<CompiledPattern> _extraPatterns;
    private readonly IReadOnlyList<CompiledPattern> _allPatterns;
    private readonly Dictionary<string, CompiledPattern[]> _patternCache = new();

    private WordListRegistry(
        IReadOnlyList<string> registeredLanguages,
        IReadOnlyDictionary<string, LanguageDefinition> languages,
        IReadOnlyList<CompiledPattern> extraPatterns,
        IReadOnlyList<CompiledPattern> allPatterns)
    {
        _registeredLanguages = registeredLanguages;
        _languages = languages;
        _extraPatterns = extraPatterns;
        _allPatterns = allPatterns;
    }

    public IReadOnlyList<string> RegisteredLanguages => _registeredLanguages;

    public IReadOnlyList<CompiledPattern> AllPatterns => _allPatterns;

    public IReadOnlyList<CompiledPattern> ExtraPatterns => _extraPatterns;

    public LanguageDefinition? GetLanguage(string code)
    {
        if (!_registeredLanguages.Contains(code, StringComparer.Ordinal))
        {
            return null;
        }

        return _languages.TryGetValue(code, out var language) ? language : null;
    }

    public IReadOnlyList<CompiledPattern> GetPatternsForLanguages(IEnumerable<string>? codes)
    {
        if (codes is null)
        {
            return _allPatterns;
        }

        var key = string.Join(",", codes.OrderBy(code => code, StringComparer.Ordinal));
        if (_patternCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var activeCodes = new HashSet<string>(codes, StringComparer.Ordinal);
        var languagePatterns = _languages.Values
            .Where(language => activeCodes.Contains(language.Code))
            .SelectMany(language => ProfanityMatcher.CompilePatterns(language.Entries, language.Code));

        var built = languagePatterns.Concat(_extraPatterns).ToArray();
        _patternCache[key] = built;
        return built;
    }

    public static WordListRegistry Load(IEnumerable<AdditionalText> additionalFiles)
    {
        var embeddedLanguages = LoadEmbeddedLanguages(out var embeddedManifest);
        var manifestCodes = embeddedManifest;
        var languages = new Dictionary<string, LanguageDefinition>(StringComparer.Ordinal);
        foreach (var pair in embeddedLanguages)
        {
            languages[pair.Key] = pair.Value;
        }
        var extraEntries = new List<ProfanityEntry>();

        foreach (var file in additionalFiles)
        {
            if (!IsWordListFile(file.Path))
            {
                continue;
            }

            var fileName = Path.GetFileName(file.Path);
            var text = file.GetText()?.ToString();

            if (string.Equals(fileName, "languages.json", StringComparison.OrdinalIgnoreCase))
            {
                if (text is null || string.IsNullOrWhiteSpace(text))
                {
                    throw WordListLoadException.ManifestEmpty(fileName);
                }

                manifestCodes = ParseManifest(text, fileName);
                continue;
            }

            if (string.Equals(fileName, "extra-patterns.json", StringComparison.OrdinalIgnoreCase))
            {
                if (text is null || string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                extraEntries.AddRange(ParseExtraPatterns(text));
                continue;
            }

            if (IsLanguageFileName(fileName, out var languageCode))
            {
                if (text is null || string.IsNullOrWhiteSpace(text))
                {
                    throw WordListLoadException.LanguageFileEmpty(fileName);
                }

                if (IsProfanityWordListPath(file.Path))
                {
                    var strictDocument = DeserializeRequired<LanguageDocument>(text, fileName);
                    var strictDefinition = strictDocument.ToDefinitionStrict(fileName);
                    if (!string.Equals(strictDefinition.Code, languageCode, StringComparison.OrdinalIgnoreCase))
                    {
                        throw WordListLoadException.InvalidLanguageDocument(
                            fileName,
                            $"property \"code\" must match the file name '{languageCode}'.");
                    }

                    languages[strictDefinition.Code] = strictDefinition;
                    continue;
                }

                var document = TryDeserialize<LanguageDocument>(text);
                if (document?.Code is null)
                {
                    continue;
                }

                var definition = document.ToDefinitionStrict(fileName);
                if (!string.Equals(definition.Code, languageCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw WordListLoadException.InvalidLanguageDocument(
                        fileName,
                        $"property \"code\" must match the file name '{languageCode}'.");
                }

                languages[definition.Code] = definition;
                continue;
            }

            if (text is null || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var legacyLanguage = TryDeserialize<LanguageDocument>(text);
            if (legacyLanguage?.Code is not null
                && legacyLanguage.Name is not null
                && legacyLanguage.Entries is not null)
            {
                var legacyFileName = $"{legacyLanguage.Code}.json";
                languages[legacyLanguage.Code] = legacyLanguage.ToDefinitionStrict(legacyFileName);
                continue;
            }

            extraEntries.AddRange(ParseLegacyDocument(text));
        }

        EnsureManifestLanguagesLoaded(manifestCodes, languages);

        var registeredCodes = manifestCodes
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var activeLanguages = registeredCodes
            .Select(code => languages[code])
            .ToList();

        var extraPatterns = ProfanityMatcher.CompilePatterns(extraEntries).ToList();
        var allPatterns = activeLanguages
            .SelectMany(language => ProfanityMatcher.CompilePatterns(language.Entries, language.Code))
            .Concat(extraPatterns)
            .ToList();

        return new WordListRegistry(
            registeredCodes,
            languages,
            extraPatterns,
            allPatterns);
    }

    public static WordListRegistry LoadDefaults()
    {
        return Load(Array.Empty<AdditionalText>());
    }

    private static void EnsureManifestLanguagesLoaded(
        IReadOnlyList<string> manifestCodes,
        IReadOnlyDictionary<string, LanguageDefinition> languages)
    {
        foreach (var code in manifestCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw WordListLoadException.InvalidLanguageDocument(
                    "languages.json",
                    "language codes must not be empty.");
            }

            if (!languages.ContainsKey(code))
            {
                throw WordListLoadException.LanguageFileNotFound(code);
            }
        }
    }

    private static IReadOnlyList<string> ParseManifest(string json, string fileName)
    {
        var manifest = DeserializeRequired<LanguagesManifestDocument>(json, fileName);
        if (manifest.Languages is null)
        {
            throw WordListLoadException.ManifestHasNoLanguages(fileName);
        }

        if (manifest.Languages.Count == 0)
        {
            throw WordListLoadException.ManifestHasNoLanguages(fileName);
        }

        return manifest.Languages;
    }

    private static LanguageDefinition ParseLanguageFile(string json, string fileName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw WordListLoadException.LanguageFileEmpty(fileName);
        }

        var document = DeserializeRequired<LanguageDocument>(json, fileName);
        return document.ToDefinitionStrict(fileName);
    }

    [ExcludeFromCodeCoverage]
    private static bool IsProfanityWordListPath(string path)
    {
        return path.IndexOf("profanity", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    [ExcludeFromCodeCoverage]
    private static bool IsWordListFile(string path)
    {
        if (path.IndexOf("profanity", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        var fileName = Path.GetFileName(path);
        if (string.Equals(fileName, "languages.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(fileName, "extra-patterns.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsLanguageFileName(fileName, out _);
    }

    [ExcludeFromCodeCoverage]
    private static bool IsLanguageFileName(string fileName, out string languageCode)
    {
        languageCode = string.Empty;
        if (!LanguageFileNamePattern.IsMatch(fileName))
        {
            return false;
        }

        var stem = fileName.Substring(0, fileName.Length - 5);
        if (string.Equals(stem, "languages", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stem, "extra-patterns", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        languageCode = stem;
        return true;
    }

    [ExcludeFromCodeCoverage]
    private static IReadOnlyDictionary<string, LanguageDefinition> LoadEmbeddedLanguages(
        out IReadOnlyList<string> manifestCodes)
    {
        string manifestJson;
        try
        {
            manifestJson = ReadEmbeddedResource("languages.json");
        }
        catch (InvalidOperationException)
        {
            throw WordListLoadException.ManifestNotFound("languages.json", embedded: true);
        }

        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw WordListLoadException.ManifestEmpty("languages.json");
        }

        manifestCodes = ParseManifest(manifestJson, "languages.json");

        var languages = new Dictionary<string, LanguageDefinition>(StringComparer.Ordinal);
        foreach (var code in manifestCodes)
        {
            string languageJson;
            try
            {
                languageJson = ReadEmbeddedResource($"{code}.json");
            }
            catch (InvalidOperationException)
            {
                throw WordListLoadException.LanguageFileNotFound(code);
            }

            languages[code] = ParseLanguageFile(languageJson, $"{code}.json");
        }

        return languages;
    }

    [ExcludeFromCodeCoverage]
    private static string ReadEmbeddedResource(string fileName)
    {
        var assembly = typeof(WordListRegistry).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("." + fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource {fileName} was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource {fileName} could not be opened.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static T DeserializeRequired<T>(string json, string fileName)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw WordListLoadException.InvalidLanguageDocument(fileName, "document is empty.");
        }
        catch (JsonException exception)
        {
            throw WordListLoadException.InvalidJson(fileName, exception);
        }
    }

    [ExcludeFromCodeCoverage]
    private static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<ProfanityEntry> ParseExtraPatterns(string json)
    {
        var document = TryDeserialize<ExtraPatternsDocument>(json);
        if (document is null)
        {
            return [];
        }

        return document.ToEntries();
    }

    [ExcludeFromCodeCoverage]
    private static IEnumerable<ProfanityEntry> ParseLegacyDocument(string json)
    {
        var extraDocument = TryDeserialize<ExtraPatternsDocument>(json);
        if (extraDocument is null)
        {
            return [];
        }

        return extraDocument.ToEntries();
    }
}
