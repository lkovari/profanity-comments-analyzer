using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

using AnalyzerConfigOptions = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
using AnalyzerConfigOptionsProvider = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider;

namespace ProfanityCommentsAnalyzer.Tests;

public static class AnalyzerTestHelper
{
    public static Task VerifyAnalyzer(
        string source,
        DiagnosticResult[] expected,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null)
    {
        var test = CreateTest(source, globalConfig, additionalFiles);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyPca001(
        string source,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string matched,
        string language,
        string severity,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null)
    {
        return VerifyAnalyzer(
            source,
            [
                Diagnostic(DiagnosticDescriptors.DiagnosticId)
                    .WithSpan(startLine, startColumn, endLine, endColumn)
                    .WithArguments(matched, language, severity),
            ],
            globalConfig,
            additionalFiles);
    }

    public static Task VerifyPca001(
        string source,
        int markupIndex,
        string matched,
        string language,
        string severity,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null)
    {
        return VerifyAnalyzer(
            source,
            [Pca001Diagnostic(markupIndex, matched, language, severity)],
            globalConfig,
            additionalFiles);
    }

    public static DiagnosticResult Pca001Diagnostic(
        int markupIndex,
        string matched,
        string language,
        string severity)
    {
        return Diagnostic(DiagnosticDescriptors.DiagnosticId)
            .WithLocation(markupIndex)
            .WithArguments(matched, language, severity);
    }

    public static async Task AssertAnalyzerReportsWordListLoadFailureAsync(
        string source,
        string expectedMessageFragment,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null,
        string filePath = "Test0.cs")
    {
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, globalConfig, additionalFiles, filePath);

        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.GetMessage(null).Contains(
                expectedMessageFragment,
                StringComparison.OrdinalIgnoreCase));
    }

    public static async Task VerifyDiagnosticCount(
        string source,
        int expectedCount,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null,
        string filePath = "Test0.cs")
    {
        var diagnostics = await GetAnalyzerDiagnosticsAsync(source, globalConfig, additionalFiles, filePath);
        Assert.Equal(expectedCount, diagnostics.Count(d => d.Id == DiagnosticDescriptors.DiagnosticId));
    }

    public static async Task<IReadOnlyList<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        string source,
        string? globalConfig = null,
        (string path, string content)[]? additionalFiles = null,
        string filePath = "Test0.cs")
    {
        var references = await ReferenceAssemblies.Net.Net90.ResolveAsync(
            null,
            CancellationToken.None);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var compilation = CSharpCompilation.Create(
            "ProfanityCommentsAnalyzerTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalTextFiles = BuildAdditionalTexts(additionalFiles);
        var configProvider = BuildConfigProvider(globalConfig);
        var options = new AnalyzerOptions(additionalTextFiles, configProvider);
        var analyzer = new ProfanityInCommentsAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            options);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static ImmutableArray<AdditionalText> BuildAdditionalTexts(
        (string path, string content)[]? additionalFiles)
    {
        if (additionalFiles is null || additionalFiles.Length == 0)
        {
            return ImmutableArray<AdditionalText>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<AdditionalText>();
        foreach (var (path, content) in additionalFiles)
        {
            builder.Add(new InMemoryAdditionalText(path, content));
        }

        return builder.ToImmutable();
    }

    private static AnalyzerConfigOptionsProvider BuildConfigProvider(string? globalConfig)
    {
        if (string.IsNullOrWhiteSpace(globalConfig))
        {
            return new EmptyAnalyzerConfigOptionsProvider();
        }

        var builder = ImmutableDictionary.CreateBuilder<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var line in globalConfig.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0
                || trimmed.StartsWith("is_global", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("root", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("["))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            builder[key] = value;
        }

        return new DictionaryAnalyzerConfigOptionsProvider(builder.ToImmutable());
    }

    private static CSharpAnalyzerTest<ProfanityInCommentsAnalyzer, DefaultVerifier> CreateTest(
        string source,
        string? globalConfig,
        (string path, string content)[]? additionalFiles)
    {
        var test = new CSharpAnalyzerTest<ProfanityInCommentsAnalyzer, DefaultVerifier>
        {
            TestCode = source,
        };

        if (globalConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
        }

        if (additionalFiles is not null)
        {
            foreach (var (path, content) in additionalFiles)
            {
                test.TestState.AdditionalFiles.Add((path, content));
            }
        }

        return test;
    }

    public static DiagnosticResult Diagnostic(string id) =>
        new DiagnosticResult(id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);

    public static string GlobalConfig(params string[] lines)
    {
        return string.Join('\n', new[] { "is_global = true" }.Concat(lines));
    }

    public static string EditorConfig(params string[] lines)
    {
        return GlobalConfig(lines);
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

    private static readonly DictionaryAnalyzerConfigOptions EmptyConfigOptions =
        new(ImmutableDictionary<string, string>.Empty);

    private sealed class EmptyAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => EmptyConfigOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyConfigOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => EmptyConfigOptions;
    }

    private sealed class DictionaryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions;

        public DictionaryAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> values)
        {
            _globalOptions = new DictionaryAnalyzerConfigOptions(values);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyConfigOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => EmptyConfigOptions;
    }

    private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _values;

        public DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (_values.TryGetValue(key, out var found) && found is not null)
            {
                value = found;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
