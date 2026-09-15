using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Models;

internal sealed class MapperModel
{
    internal MapperModel(
        INamedTypeSymbol mapperInterface,
        string? configuredQueryGroupName,
        IReadOnlyList<string> queryGroupNameCandidates,
        Location? mapperAttributeLocation,
        INamedTypeSymbol? configuredTemplateSourceLoaderType,
        string? configuredTemplateLocation,
        Location? templateSourceLoaderAttributeLocation,
        bool hasConflictingTemplateSourceAttributes,
        IReadOnlyList<ISymbol> members,
        IReadOnlyList<MapperMethodModel> methods)
    {
        MapperInterface = mapperInterface;
        ConfiguredQueryGroupName = configuredQueryGroupName;
        QueryGroupNameCandidates = queryGroupNameCandidates;
        MapperAttributeLocation = mapperAttributeLocation;
        ConfiguredTemplateSourceLoaderType = configuredTemplateSourceLoaderType;
        ConfiguredTemplateLocation = configuredTemplateLocation;
        TemplateSourceLoaderAttributeLocation = templateSourceLoaderAttributeLocation;
        HasConflictingTemplateSourceAttributes = hasConflictingTemplateSourceAttributes;
        Members = members;
        Methods = methods;
    }

    public INamedTypeSymbol MapperInterface { get; }

    public string? ConfiguredQueryGroupName { get; }

    public IReadOnlyList<string> QueryGroupNameCandidates { get; }

    public Location? MapperAttributeLocation { get; }

    public INamedTypeSymbol? ConfiguredTemplateSourceLoaderType { get; }

    public string? ConfiguredTemplateLocation { get; }

    public Location? TemplateSourceLoaderAttributeLocation { get; }

    public bool HasConflictingTemplateSourceAttributes { get; }

    public IReadOnlyList<ISymbol> Members { get; }

    public IReadOnlyList<MapperMethodModel> Methods { get; }
}
