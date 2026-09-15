using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperMemberValidator
{
    public static IEnumerable<Diagnostic> Validate(
        MapperModel mapper)
    {
        var mapperName = mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        foreach (var member in mapper.Members)
        {
            var memberName = member.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            var location = member.Locations.FirstOrDefault();

            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                yield return Diagnostic.Create(
                    RazQLDiagnostics.NonPublicMapperMember,
                    location,
                    memberName,
                    mapperName);
            }

            if (member is not IMethodSymbol)
            {
                yield return Diagnostic.Create(
                    RazQLDiagnostics.UnsupportedMapperMember,
                    location,
                    mapperName,
                    member.Kind,
                    memberName);
            }
        }
    }
}
