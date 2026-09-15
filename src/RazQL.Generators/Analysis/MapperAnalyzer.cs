using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;
using RazQL.Generators.Validation;

namespace RazQL.Generators.Analysis;

internal static class MapperAnalyzer
{
    public static MapperAnalysis Analyze(
        MapperModel mapper,
        KnownTypeSymbols knownTypes,
        IReadOnlyList<TemplateSourceModel> templateSources)
    {
        var diagnostics = new List<Diagnostic>();

        diagnostics.AddRange(MapperInterfaceValidator.Validate(mapper));

        diagnostics.AddRange(MapperAttributeValidator.Validate(mapper, knownTypes));

        diagnostics.AddRange(MapperMemberValidator.Validate(mapper));

        foreach (var method in mapper.Methods)
        {
            diagnostics.AddRange(MapperMethodValidator.Validate(method.Method, knownTypes));
            diagnostics.AddRange(MapperMethodAttributeValidator.Validate(method, knownTypes));
        }

        diagnostics.AddRange(MapperQueryNameValidator.Validate(mapper));
        diagnostics.AddRange(MapperMethodHierarchyValidator.Validate(mapper));

        if (diagnostics.Count == 0)
        {
            diagnostics.AddRange(MapperTemplateSourceValidator.Validate(
                mapper,
                knownTypes,
                templateSources));
        }

        return new MapperAnalysis(mapper, diagnostics);
    }
}
