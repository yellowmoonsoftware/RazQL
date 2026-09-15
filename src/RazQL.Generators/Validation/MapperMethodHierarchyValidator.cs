using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperMethodHierarchyValidator
{
    public static IEnumerable<Diagnostic> Validate(
        MapperModel mapper)
    {
        var mapperName = mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var methods = mapper.Methods;

        for (var currentIndex = 0; currentIndex < methods.Count; currentIndex++)
        {
            for (var candidateIndex = currentIndex + 1; candidateIndex < methods.Count; candidateIndex++)
            {
                var current = methods[currentIndex].Method;
                var candidate = methods[candidateIndex].Method;

                if (!HaveSameSignature(current, candidate))
                {
                    continue;
                }

                if (InheritsFrom(current.ContainingType, candidate.ContainingType))
                {
                    yield return CreateDiagnostic(current, candidate, mapperName);
                }
                else if (InheritsFrom(candidate.ContainingType, current.ContainingType))
                {
                    yield return CreateDiagnostic(candidate, current, mapperName);
                }
                else
                {
                    yield return CreateDiagnostic(current, candidate, mapperName);
                    yield return CreateDiagnostic(candidate, current, mapperName);
                }
            }
        }
    }

    private static bool HaveSameSignature(IMethodSymbol left, IMethodSymbol right)
    {
        if (left.Name != right.Name ||
            left.Arity != right.Arity ||
            left.Parameters.Length != right.Parameters.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Parameters.Length; index++)
        {
            var leftParameter = left.Parameters[index];
            var rightParameter = right.Parameters[index];

            if (leftParameter.RefKind != rightParameter.RefKind ||
                !SymbolEqualityComparer.Default.Equals(leftParameter.Type, rightParameter.Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool InheritsFrom(INamedTypeSymbol candidate, INamedTypeSymbol baseInterface)
    {
        return candidate.AllInterfaces.Any(interfaceType =>
            SymbolEqualityComparer.Default.Equals(interfaceType, baseInterface));
    }

    private static Diagnostic CreateDiagnostic(
        IMethodSymbol method,
        IMethodSymbol conflictingMethod,
        string mapperName)
    {
        return Diagnostic.Create(
            RazQLDiagnostics.ConflictingInheritedMapperMethod,
            method.Locations.FirstOrDefault(),
            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            conflictingMethod.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            mapperName);
    }
}
