using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ProfanityCommentsAnalyzer;

[ExcludeFromCodeCoverage]
public sealed class WordListLoadException : InvalidOperationException
{
    public WordListLoadException(string message)
        : base(message)
    {
    }

    public static WordListLoadException ManifestNotFound(string fileName, bool embedded)
    {
        if (embedded)
        {
            return new WordListLoadException(
                $"Built-in profanity word list manifest '{fileName}' was not found in the ProfanityCommentsAnalyzer package. "
                + "Reinstall or update the NuGet package.");
        }

        return new WordListLoadException(
            $"Profanity word list manifest '{fileName}' was not found. "
            + "Add languages.json to AdditionalFiles (for example profanity/languages.json).");
    }

    public static WordListLoadException ManifestEmpty(string fileName)
    {
        return new WordListLoadException(
            $"Profanity word list manifest '{fileName}' is empty. "
            + "Provide a JSON document listing language codes, for example: "
            + "{\"languages\":[\"en\",\"hu\",\"de\",\"ro\",\"it\"]}");
    }

    public static WordListLoadException ManifestHasNoLanguages(string fileName)
    {
        return new WordListLoadException(
            $"Profanity word list manifest '{fileName}' does not list any languages. "
            + "Add at least one language code to the \"languages\" array, for example \"en\".");
    }

    public static WordListLoadException LanguageFileNotFound(string languageCode)
    {
        return new WordListLoadException(
            $"Profanity word list manifest 'languages.json' references language '{languageCode}', "
            + $"but '{languageCode}.json' was not found. "
            + $"Add '{languageCode}.json' to AdditionalFiles next to languages.json.");
    }

    public static WordListLoadException LanguageFileEmpty(string fileName)
    {
        return new WordListLoadException(
            $"Profanity language file '{fileName}' is empty. "
            + "Provide a JSON document with \"code\", \"name\", and a non-empty \"entries\" array.");
    }

    public static WordListLoadException InvalidJson(string fileName, JsonException exception)
    {
        return new WordListLoadException(
            $"Profanity word list file '{fileName}' is not valid JSON: {exception.Message}");
    }

    public static WordListLoadException InvalidLanguageDocument(string fileName, string detail)
    {
        return new WordListLoadException(
            $"Profanity language file '{fileName}' is invalid: {detail}");
    }

    public static WordListLoadException IncompleteEntry(string fileName, int entryIndex)
    {
        return new WordListLoadException(
            $"Profanity language file '{fileName}' has an incomplete entry at index {entryIndex}. "
            + "Each entry requires non-empty \"word\" and \"pattern\" properties.");
    }
}
