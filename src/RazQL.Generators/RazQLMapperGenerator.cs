using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RazQL.Generators.Analysis;
using RazQL.Generators.Discovery;
using RazQL.Generators.Emission;
using RazQL.Generators.Models;
using RazQL.Generators.Validation;

namespace RazQL.Generators;

/// <summary>Generates validated implementations for interfaces marked with <c>RazQLMapperAttribute</c>.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class RazQLMapperGenerator : IIncrementalGenerator
{
    private const string MapperAttributeMetadataName = "RazQL.RazQLMapperAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var decoratedInterfaces = context.SyntaxProvider.ForAttributeWithMetadataName(
            MapperAttributeMetadataName,
            static (node, _) => node is InterfaceDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol);

        context.RegisterSourceOutput(decoratedInterfaces, static (sourceContext, mapperInterface) =>
        {
            if (!ContainsTypeParameters(mapperInterface))
            {
                return;
            }

            sourceContext.ReportDiagnostic(Diagnostic.Create(
                RazQLDiagnostics.OpenGenericMapper,
                mapperInterface.Locations.FirstOrDefault(),
                mapperInterface.ToDisplayString()));
        });

        var mapperInterfaces = decoratedInterfaces
            .Where(static mapperInterface => !ContainsTypeParameters(mapperInterface));

        var knownTypes = context.CompilationProvider.Select(
            static (compilation, _) => KnownTypeSymbols.From(compilation));

        var mapperModels = mapperInterfaces
            .Combine(knownTypes)
            .Select(static (source, _) => MapperModelFactory.Create(source.Left, source.Right));

        var templateSources = context.AdditionalTextsProvider
            .Where(static source => source.Path.EndsWith(".sql.cshtml", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (source, _) => TemplateSourceModel.From(source.Left, source.Right))
            .Collect();

        var mapperAnalyses = mapperModels
            .Combine(knownTypes)
            .Combine(templateSources)
            .Select(static (source, _) => MapperAnalyzer.Analyze(
                source.Left.Left,
                source.Left.Right,
                source.Right));

        context.RegisterSourceOutput(mapperAnalyses, static (sourceContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceContext.ReportDiagnostic(diagnostic);
            }
        });

        var collectedMapperAnalyses = mapperAnalyses.Collect();

        context.RegisterSourceOutput(collectedMapperAnalyses, static (sourceContext, analyses) =>
        {
            var validMappers = analyses
                .Where(static analysis => analysis.IsValid)
                .Select(static analysis => analysis.Model)
                .ToArray();
            var groupNameDiagnostics = MapperQueryGroupNameValidator.Validate(validMappers).ToArray();

            foreach (var diagnostic in groupNameDiagnostics)
            {
                sourceContext.ReportDiagnostic(diagnostic);
            }

            if (groupNameDiagnostics.Length != 0)
            {
                return;
            }

            foreach (var mapper in validMappers)
            {
                sourceContext.AddSource(
                    GeneratedMapperEmitter.GetHintName(mapper),
                    SourceText.From(GeneratedMapperEmitter.Emit(mapper), Encoding.UTF8));
            }
        });
    }

    private static bool ContainsTypeParameters(INamedTypeSymbol mapperInterface)
    {
        for (var type = mapperInterface; type is not null; type = type.ContainingType)
        {
            if (type.TypeParameters.Length != 0)
            {
                return true;
            }
        }

        return false;
    }
}
