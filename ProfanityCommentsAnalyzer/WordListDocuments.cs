using ProfanityCommentsAnalyzer.Models;

namespace ProfanityCommentsAnalyzer;

internal sealed class LanguagesManifestDocument
{
    public List<string>? Languages { get; init; }
}

internal sealed class ProfanityEntryDocument
{
    public string? Word { get; init; }

    public string? Pattern { get; init; }

    public Severity Severity { get; init; }

    public Category? Category { get; init; }

    public ProfanityEntry? ToEntry()
    {
        if (Word is null || Pattern is null)
        {
            return null;
        }

        return new ProfanityEntry(Word, Pattern, Severity, Category);
    }
}

internal sealed class LanguageDocument
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    public List<ProfanityEntryDocument>? Entries { get; init; }

    public LanguageDefinition ToDefinitionStrict(string fileName)
    {
        if (Code is null || string.IsNullOrWhiteSpace(Code))
        {
            throw WordListLoadException.InvalidLanguageDocument(
                fileName,
                "property \"code\" is required and must not be empty.");
        }

        if (Name is null || string.IsNullOrWhiteSpace(Name))
        {
            throw WordListLoadException.InvalidLanguageDocument(
                fileName,
                "property \"name\" is required and must not be empty.");
        }

        if (Entries is null || Entries.Count == 0)
        {
            throw WordListLoadException.InvalidLanguageDocument(
                fileName,
                "property \"entries\" must contain at least one word entry.");
        }

        var entries = new List<ProfanityEntry>();
        for (var index = 0; index < Entries.Count; index++)
        {
            var entry = Entries[index];
            if (entry.Word is null
                || string.IsNullOrWhiteSpace(entry.Word)
                || entry.Pattern is null
                || string.IsNullOrWhiteSpace(entry.Pattern))
            {
                throw WordListLoadException.IncompleteEntry(fileName, index);
            }

            entries.Add(new ProfanityEntry(entry.Word, entry.Pattern, entry.Severity, entry.Category));
        }

        return new LanguageDefinition(Code, Name, entries);
    }
}

internal sealed class ExtraPatternsDocument
{
    public List<ProfanityEntryDocument>? Entries { get; init; }

    public List<ProfanityEntryDocument>? ExtraPatterns { get; init; }

    public IEnumerable<ProfanityEntry> ToEntries()
    {
        var source = Entries ?? ExtraPatterns;
        if (source is null)
        {
            return [];
        }

        var results = new List<ProfanityEntry>();
        foreach (var entry in source)
        {
            var mapped = entry.ToEntry();
            if (mapped is not null)
            {
                results.Add(mapped);
            }
        }

        return results;
    }
}
