using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Internal;

internal static class TypeSymbolExtensions
{
    public static bool IsOrDerivesFrom(
        this INamedTypeSymbol? candidate,
        INamedTypeSymbol? baseType)
    {
        for (var current = candidate; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
