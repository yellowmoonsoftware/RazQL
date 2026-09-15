using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Discovery;

internal static class MapperMethodCollector
{
    public static IEnumerable<IMethodSymbol> Collect(INamedTypeSymbol mapperInterface)
    {
        return Collect(MapperMemberCollector.Collect(mapperInterface));
    }

    public static IEnumerable<IMethodSymbol> Collect(IEnumerable<ISymbol> members)
    {
        return members
            .OfType<IMethodSymbol>()
            .Where(static method => method.AssociatedSymbol is null);
    }
}
