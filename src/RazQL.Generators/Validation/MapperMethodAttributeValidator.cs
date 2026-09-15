using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperMethodAttributeValidator
{
    public static IEnumerable<Diagnostic> Validate(
        MapperMethodModel methodModel,
        KnownTypeSymbols knownTypes)
    {
        if (methodModel.HasConflictingTemplateSourceAttributes)
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.ConflictingTemplateSourceAttributes,
                methodModel.TemplateSourceLoaderAttributeLocation ?? methodModel.Method.Locations.FirstOrDefault(),
                methodModel.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        if (methodModel.HasInlineQuerySource && string.IsNullOrWhiteSpace(methodModel.InlineQuerySource))
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.InvalidInlineQuerySource,
                methodModel.TemplateSourceLoaderAttributeLocation ?? methodModel.Method.Locations.FirstOrDefault(),
                methodModel.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        if (methodModel.ConfiguredTemplateName is not null &&
            string.IsNullOrWhiteSpace(methodModel.ConfiguredTemplateName))
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.InvalidAttributeName,
                methodModel.TemplateNameAttributeLocation ?? methodModel.Method.Locations.FirstOrDefault(),
                "TemplateName",
                methodModel.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        var templateSourceLoaderDiagnostic = TemplateSourceLoaderTypeValidator.Validate(
            methodModel.ConfiguredTemplateSourceLoaderType,
            methodModel.Method,
            methodModel.TemplateSourceLoaderAttributeLocation,
            knownTypes);
        if (templateSourceLoaderDiagnostic is not null)
        {
            yield return templateSourceLoaderDiagnostic;
        }
    }
}
