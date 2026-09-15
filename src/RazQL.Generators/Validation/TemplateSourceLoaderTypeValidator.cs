using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class TemplateSourceLoaderTypeValidator
{
    public static Diagnostic? Validate(
        INamedTypeSymbol? loaderType,
        ISymbol target,
        Location? attributeLocation,
        KnownTypeSymbols knownTypes)
    {
        if (loaderType is null)
        {
            return null;
        }

        var implementsLoader = knownTypes.TemplateSourceLoader is { } templateSourceLoader &&
                               loaderType.AllInterfaces.Any(interfaceType =>
                                   SymbolEqualityComparer.Default.Equals(
                                       interfaceType,
                                       templateSourceLoader));
        var isOpenGeneric = loaderType.IsUnboundGenericType ||
                            loaderType.TypeArguments.Any(typeArgument =>
                                typeArgument.TypeKind == TypeKind.TypeParameter);

        return !implementsLoader ||
               loaderType.TypeKind == TypeKind.Interface ||
               loaderType.IsAbstract ||
               isOpenGeneric
            ? Diagnostic.Create(
                RazQLDiagnostics.InvalidTemplateSourceLoaderType,
                attributeLocation ?? target.Locations.FirstOrDefault(),
                loaderType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
            : null;
    }
}
