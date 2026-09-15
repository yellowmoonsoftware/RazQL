using Microsoft.CodeAnalysis;
using RazQL.Generators.Internal;

namespace RazQL.Generators.Models;

internal sealed class MapperMethodModel
{
    public enum QueryResultShape
    {
        SingleOrDefault,
        Many
    }

    private const string AsyncSuffix = "Async";

    private MapperMethodModel(
        IMethodSymbol method,
        string? configuredTemplateName,
        Location? templateNameAttributeLocation,
        INamedTypeSymbol? configuredTemplateSourceLoaderType,
        string? configuredTemplateLocation,
        INamedTypeSymbol? templateSourceLoaderType,
        string? templateLocation,
        Location? templateSourceLoaderAttributeLocation,
        bool hasConflictingTemplateSourceAttributes,
        bool hasInlineQuerySource,
        string? inlineQuerySource,
        ITypeSymbol? criteriaType,
        ITypeSymbol? resultType,
        QueryResultShape? resultShape)
    {
        Method = method;
        ConfiguredTemplateName = configuredTemplateName;
        QueryNameCandidates = GetQueryNameCandidates(method.Name, configuredTemplateName);
        TemplateNameAttributeLocation = templateNameAttributeLocation;
        ConfiguredTemplateSourceLoaderType = configuredTemplateSourceLoaderType;
        ConfiguredTemplateLocation = configuredTemplateLocation;
        TemplateSourceLoaderType = templateSourceLoaderType;
        TemplateLocation = templateLocation;
        TemplateSourceLoaderAttributeLocation = templateSourceLoaderAttributeLocation;
        HasConflictingTemplateSourceAttributes = hasConflictingTemplateSourceAttributes;
        HasInlineQuerySource = hasInlineQuerySource;
        InlineQuerySource = inlineQuerySource;
        CriteriaType = criteriaType;
        ResultType = resultType;
        ResultShape = resultShape;
    }

    public IMethodSymbol Method { get; }

    public IReadOnlyList<string> QueryNameCandidates { get; }

    public string? ConfiguredTemplateName { get; }

    public Location? TemplateNameAttributeLocation { get; }

    public INamedTypeSymbol? ConfiguredTemplateSourceLoaderType { get; }

    public string? ConfiguredTemplateLocation { get; }

    public INamedTypeSymbol? TemplateSourceLoaderType { get; }

    public string? TemplateLocation { get; }

    public Location? TemplateSourceLoaderAttributeLocation { get; }

    public bool HasConflictingTemplateSourceAttributes { get; }

    public bool HasInlineQuerySource { get; }

    public string? InlineQuerySource { get; }

    public ITypeSymbol? CriteriaType { get; }

    public ITypeSymbol? ResultType { get; }

    public QueryResultShape? ResultShape { get; }

    public static MapperMethodModel From(
        IMethodSymbol method,
        KnownTypeSymbols knownTypes,
        INamedTypeSymbol? mapperTemplateSourceLoaderType,
        string? mapperTemplateLocation,
        bool mapperTemplateSourceLoaderIsKnown)
    {
        var sourceAttributes = method.GetAttributes()
            .Where(attribute => attribute.AttributeClass.IsOrDerivesFrom(
                knownTypes.TemplateSourceLoaderAttribute))
            .ToArray();
        var sourceAttribute = sourceAttributes.FirstOrDefault();
        var templateAttribute = sourceAttributes.FirstOrDefault(attribute =>
            SymbolEqualityComparer.Default.Equals(
                attribute.AttributeClass,
                knownTypes.QueryTemplateSourceAttribute));
        var queryAttribute = sourceAttributes.FirstOrDefault(attribute =>
            SymbolEqualityComparer.Default.Equals(
                attribute.AttributeClass,
                knownTypes.RazQLQueryAttribute));
        var hasUnknownSourceAttribute = sourceAttributes.Length == 1 &&
                                        templateAttribute is null &&
                                        queryAttribute is null;
        var configuredTemplateSourceLoaderType = templateAttribute?
            .ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
        var configuredTemplateLocation = NormalizeTemplateLocation(
            templateAttribute?.GetNamedString("TemplateLocation"));
        var configuredTemplateName = templateAttribute?.GetNamedString("TemplateName");
        var effectiveTemplateSourceLoaderType = queryAttribute is not null
            ? knownTypes.QueryAttributeTemplateSourceLoader
            : hasUnknownSourceAttribute || sourceAttributes.Length > 1
                ? null
                : templateAttribute is not null
                    ? configuredTemplateSourceLoaderType ??
                      (mapperTemplateSourceLoaderIsKnown
                          ? mapperTemplateSourceLoaderType ?? knownTypes.ResourceTemplateSourceLoader
                          : null)
                    : mapperTemplateSourceLoaderIsKnown
                        ? mapperTemplateSourceLoaderType ?? knownTypes.ResourceTemplateSourceLoader
                        : null;

        var (resultType, resultShape) = GetResult(method, knownTypes);

        return new MapperMethodModel(
            method,
            configuredTemplateName,
            sourceAttribute?.GetApplicationLocation(),
            configuredTemplateSourceLoaderType,
            configuredTemplateLocation,
            effectiveTemplateSourceLoaderType,
            configuredTemplateLocation ?? mapperTemplateLocation,
            sourceAttribute?.GetApplicationLocation(),
            sourceAttributes.Length > 1,
            queryAttribute is not null,
            queryAttribute?.ConstructorArguments.FirstOrDefault().Value as string,
            method.Parameters.FirstOrDefault()?.Type,
            resultType,
            resultShape);
    }

    private static string? NormalizeTemplateLocation(string? templateLocation)
    {
        return templateLocation?.Trim() is { Length: > 0 } normalized
            ? normalized
            : null;
    }

    private static IReadOnlyList<string> GetQueryNameCandidates(
        string methodName,
        string? configuredQueryName)
    {
        if (configuredQueryName is not null)
        {
            return [configuredQueryName];
        }

        return methodName.EndsWith(AsyncSuffix, StringComparison.Ordinal)
            ? [methodName.Substring(0, methodName.Length - AsyncSuffix.Length), methodName]
            : [methodName];
    }

    private static (ITypeSymbol? ResultType, QueryResultShape? ResultShape) GetResult(
        IMethodSymbol method,
        KnownTypeSymbols knownTypes)
    {
        if (method.ReturnType is not INamedTypeSymbol returnType ||
            !SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, knownTypes.GenericTask))
        {
            return (null, null);
        }

        var taskResultType = returnType.TypeArguments[0];
        if (taskResultType is INamedTypeSymbol enumerableType &&
            SymbolEqualityComparer.Default.Equals(
                enumerableType.OriginalDefinition,
                knownTypes.GenericEnumerable))
        {
            return (enumerableType.TypeArguments[0], QueryResultShape.Many);
        }

        return (taskResultType, QueryResultShape.SingleOrDefault);
    }
}
