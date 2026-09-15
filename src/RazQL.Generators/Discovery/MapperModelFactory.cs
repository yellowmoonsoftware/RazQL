using Microsoft.CodeAnalysis;
using RazQL.Generators.Internal;
using RazQL.Generators.Models;

namespace RazQL.Generators.Discovery;

internal static class MapperModelFactory
{
    public static MapperModel Create(
        INamedTypeSymbol mapperInterface,
        KnownTypeSymbols knownTypes)
    {
        var mapperAttribute = mapperInterface.GetAttributes().FirstOrDefault(attribute =>
            SymbolEqualityComparer.Default.Equals(
                attribute.AttributeClass,
                knownTypes.RazQLMapperAttribute));
        var configuredQueryGroupName = mapperAttribute?.GetNamedString("Name");
        var templateSourceAttributes = mapperInterface.GetAttributes()
            .Where(attribute => attribute.AttributeClass.IsOrDerivesFrom(
                knownTypes.TemplateSourceLoaderAttribute))
            .ToArray();
        var templateSourceAttribute = templateSourceAttributes.FirstOrDefault();
        var recognizedTemplateSourceAttribute = templateSourceAttributes.FirstOrDefault(attribute =>
            SymbolEqualityComparer.Default.Equals(
                attribute.AttributeClass,
                knownTypes.TemplateSourceAttribute));
        var configuredTemplateSourceLoaderType = recognizedTemplateSourceAttribute?
            .ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
        var configuredTemplateLocation = NormalizeTemplateLocation(
            recognizedTemplateSourceAttribute?.GetNamedString("TemplateLocation"));
        var effectiveTemplateSourceLoaderType = templateSourceAttributes.Length switch
        {
            0 => knownTypes.ResourceTemplateSourceLoader,
            1 when recognizedTemplateSourceAttribute is not null =>
                configuredTemplateSourceLoaderType ?? knownTypes.ResourceTemplateSourceLoader,
            _ => null
        };
        var templateSourceLoaderIsKnown = templateSourceAttributes.Length == 0 ||
                                          templateSourceAttributes.Length == 1 &&
                                          recognizedTemplateSourceAttribute is not null;
        var members = MapperMemberCollector.Collect(mapperInterface).ToArray();
        var methods = MapperMethodCollector.Collect(members)
            .Select(method => MapperMethodModel.From(
                method,
                knownTypes,
                effectiveTemplateSourceLoaderType,
                configuredTemplateLocation,
                templateSourceLoaderIsKnown))
            .ToArray();

        return new MapperModel(
            mapperInterface,
            configuredQueryGroupName,
            GetQueryGroupNameCandidates(mapperInterface.Name, configuredQueryGroupName),
            mapperAttribute?.GetApplicationLocation(),
            configuredTemplateSourceLoaderType,
            configuredTemplateLocation,
            templateSourceAttribute?.GetApplicationLocation(),
            templateSourceAttributes.Length > 1,
            members,
            methods);
    }

    private static string? NormalizeTemplateLocation(string? templateLocation)
    {
        return templateLocation?.Trim() is { Length: > 0 } normalized
            ? normalized
            : null;
    }

    private static IReadOnlyList<string> GetQueryGroupNameCandidates(
        string mapperName,
        string? configuredQueryGroupName)
    {
        if (configuredQueryGroupName is not null)
        {
            return [configuredQueryGroupName];
        }

        return mapperName.Length > 1 &&
               mapperName[0] == 'I' &&
               mapperName[1] is >= 'A' and <= 'Z'
            ? [mapperName.Substring(1), mapperName]
            : [mapperName];
    }
}
