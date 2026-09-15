using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperInterfaceValidator
{
    public static IEnumerable<Diagnostic> Validate(MapperModel mapper)
    {
        if (IsAccessibleFromGeneratedCode(mapper.MapperInterface))
        {
            yield break;
        }

        yield return Diagnostic.Create(
            RazQLDiagnostics.InaccessibleMapperInterface,
            mapper.MapperInterface.Locations.FirstOrDefault(),
            mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
    }

    private static bool IsAccessibleFromGeneratedCode(INamedTypeSymbol mapperInterface)
    {
        for (var type = mapperInterface; type is not null; type = type.ContainingType)
        {
            if (type.IsFileLocal ||
                type.DeclaredAccessibility is not (
                    Accessibility.Public or
                    Accessibility.Internal or
                    Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }
}
