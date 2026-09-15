using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Discovery;

internal static class MapperMemberCollector
{
    public static IEnumerable<ISymbol> Collect(INamedTypeSymbol mapperInterface)
    {
        return mapperInterface.AllInterfaces
            .Prepend(mapperInterface)
            .SelectMany(static type => type.GetMembers())
            .Where(static member => !member.IsImplicitlyDeclared)
            .Where(static member => member is not IMethodSymbol { AssociatedSymbol: not null })
            .Distinct<ISymbol>(SymbolEqualityComparer.Default);
    }
}
