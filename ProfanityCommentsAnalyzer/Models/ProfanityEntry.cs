namespace ProfanityCommentsAnalyzer.Models;

public sealed record ProfanityEntry(
    string Word,
    string Pattern,
    Severity Severity,
    Category? Category = null);
