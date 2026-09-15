using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperMethodResultValidator
{
    public static Diagnostic? Validate(
        IMethodSymbol method,
        ITypeSymbol taskResultType,
        KnownTypeSymbols knownTypes)
    {
        var mappedResultType = taskResultType is INamedTypeSymbol enumerableType &&
                               IsExactInterface(enumerableType, knownTypes.GenericEnumerable)
            ? enumerableType.TypeArguments[0]
            : taskResultType;

        if (IsTaskLike(mappedResultType, knownTypes))
        {
            return CreateDiagnostic(
                method,
                mappedResultType,
                "the mapped result cannot itself be Task, Task<T>, ValueTask, or ValueTask<T>");
        }

        if (IsAllowedEnumerableScalar(mappedResultType))
        {
            return null;
        }

        if (IsOrImplements(mappedResultType, knownTypes.Enumerable))
        {
            return CreateDiagnostic(
                method,
                mappedResultType,
                "collection results must use System.Collections.Generic.IEnumerable<T> directly");
        }

        if (IsOrImplements(mappedResultType, knownTypes.GenericAsyncEnumerable))
        {
            return CreateDiagnostic(
                method,
                mappedResultType,
                "IAsyncEnumerable<T> results are not currently supported");
        }

        if (IsOrImplements(mappedResultType, knownTypes.GenericAsyncEnumerator))
        {
            return CreateDiagnostic(
                method,
                mappedResultType,
                "IAsyncEnumerator<T> results are not currently supported");
        }

        if (IsOrImplements(mappedResultType, knownTypes.Enumerator))
        {
            return CreateDiagnostic(
                method,
                mappedResultType,
                "IEnumerator results are not supported");
        }

        return null;
    }

    private static bool IsTaskLike(ITypeSymbol type, KnownTypeSymbols knownTypes)
    {
        if (IsOrDerivedFrom(type, knownTypes.Task))
        {
            return true;
        }

        return type is INamedTypeSymbol namedType &&
               (SymbolEqualityComparer.Default.Equals(namedType, knownTypes.ValueTask) ||
                SymbolEqualityComparer.Default.Equals(
                    namedType.OriginalDefinition,
                    knownTypes.GenericValueTask));
    }

    private static bool IsOrDerivedFrom(ITypeSymbol type, INamedTypeSymbol? baseType)
    {
        for (var candidate = type as INamedTypeSymbol; candidate is not null; candidate = candidate.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, baseType) ||
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedEnumerableScalar(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_String ||
               type is IArrayTypeSymbol
               {
                   IsSZArray: true,
                   ElementType.SpecialType: SpecialType.System_Byte
               };
    }

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        return IsExactInterface(type, interfaceType) ||
               type.AllInterfaces.Any(candidate =>
                   SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, interfaceType));
    }

    private static bool IsExactInterface(ITypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        return type is INamedTypeSymbol namedType &&
               SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, interfaceType);
    }

    private static Diagnostic CreateDiagnostic(
        IMethodSymbol method,
        ITypeSymbol taskResultType,
        string reason)
    {
        return Diagnostic.Create(
            RazQLDiagnostics.UnsupportedMapperMethodResultType,
            method.Locations.FirstOrDefault(),
            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            taskResultType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            reason);
    }
}
