using Microsoft.CodeAnalysis;

namespace ProfanityCommentsAnalyzer;

public static class DiagnosticDescriptors
{
    public const string DiagnosticId = "PCA001";

    public static readonly DiagnosticDescriptor ProfanityInComment = new(
        id: DiagnosticId,
        title: "Profanity or offensive language in comment",
        messageFormat: "[profanity-in-comments] \"{0}\" ({1}, severity: {2}) found in comment",
        category: "CodeQuality",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Detects profanity, threats, slurs, insults, and code-critique remarks in C# comments.");
}
