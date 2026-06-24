using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ProfanityCommentsAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProfanityInCommentsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ProfanityInComment);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        var config = AnalyzerConfig.FromOptions(context.Options);
        var registry = WordListRegistry.Load(context.Options.AdditionalFiles);
        var patterns = registry.GetPatternsForLanguages(config.Languages);

        context.RegisterSyntaxTreeAction(treeContext =>
        {
            var tree = treeContext.Tree;
            if (!IsCSharpSourceFile(tree.FilePath))
            {
                return;
            }

            var root = tree.GetRoot(treeContext.CancellationToken);
            foreach (var trivia in root.DescendantTrivia())
            {
                if (!IsCommentTrivia(trivia))
                {
                    continue;
                }

                var commentText = GetCommentText(trivia);
                var matches = ProfanityMatcher.FindMatches(
                    commentText,
                    config.MinSeverity,
                    config.AllowList,
                    patterns,
                    []);

                foreach (var match in matches)
                {
                    var severityLabel = match.Entry.Severity.ToString().ToLowerInvariant();
                    var langLabel = match.Lang ?? "custom";
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ProfanityInComment,
                        Location.Create(tree, trivia.FullSpan),
                        match.Matched,
                        langLabel,
                        severityLabel);

                    treeContext.ReportDiagnostic(diagnostic);
                }
            }
        });
    }

    private static string GetCommentText(SyntaxTrivia trivia)
    {
        var text = trivia.ToString();
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
            && text.StartsWith("//", StringComparison.Ordinal))
        {
            return text.Substring(2).TrimStart();
        }

        if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
            && text.StartsWith("/*", StringComparison.Ordinal)
            && text.EndsWith("*/", StringComparison.Ordinal))
        {
            return text.Substring(2, text.Length - 4);
        }

        if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            && text.StartsWith("///", StringComparison.Ordinal))
        {
            return text.Substring(3).TrimStart();
        }

        return text;
    }

    private static bool IsCSharpSourceFile(string? filePath)
    {
        return filePath != null
            && filePath.Length >= 3
            && string.Compare(filePath, filePath.Length - 3, ".cs", 0, 3, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static bool IsCommentTrivia(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }
}
