namespace ProfanityCommentsAnalyzer.Models;

public sealed record LanguageDefinition(
    string Code,
    string Name,
    IReadOnlyList<ProfanityEntry> Entries);
