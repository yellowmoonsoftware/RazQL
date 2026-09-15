using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperAttributeValidator
{
    public static IEnumerable<Diagnostic> Validate(
        MapperModel mapper,
        KnownTypeSymbols knownTypes)
    {
        if (mapper.HasConflictingTemplateSourceAttributes)
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.ConflictingTemplateSourceAttributes,
                mapper.TemplateSourceLoaderAttributeLocation ?? mapper.MapperInterface.Locations.FirstOrDefault(),
                mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        if (mapper.ConfiguredQueryGroupName is not null &&
            string.IsNullOrWhiteSpace(mapper.ConfiguredQueryGroupName))
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.InvalidAttributeName,
                mapper.MapperAttributeLocation ?? mapper.MapperInterface.Locations.FirstOrDefault(),
                "RazQLMapperAttribute",
                mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        var templateSourceLoaderDiagnostic = TemplateSourceLoaderTypeValidator.Validate(
            mapper.ConfiguredTemplateSourceLoaderType,
            mapper.MapperInterface,
            mapper.TemplateSourceLoaderAttributeLocation,
            knownTypes);
        if (templateSourceLoaderDiagnostic is not null)
        {
            yield return templateSourceLoaderDiagnostic;
        }
    }
}
