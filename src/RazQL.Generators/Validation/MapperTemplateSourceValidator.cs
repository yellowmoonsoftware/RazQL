using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperTemplateSourceValidator
{
    private const string TemplateDirectoryName = "SqlTemplates";
    private const string TemplateFileSuffix = ".sql.cshtml";

    public static IEnumerable<Diagnostic> Validate(
        MapperModel mapper,
        KnownTypeSymbols knownTypes,
        IReadOnlyList<TemplateSourceModel> templateSources)
    {
        var normalizedSources = templateSources
            .Select(source => new NormalizedTemplateSource(source, NormalizePath(source.Path)))
            .ToArray();

        foreach (var method in mapper.Methods)
        {
            if (!UsesVerifiableTemplateSourceLoader(method, knownTypes))
            {
                continue;
            }

            var candidates = GetTemplatePathCandidates(mapper, method, knownTypes).ToArray();
            var matches = normalizedSources
                .Where(source => candidates.Any(candidate =>
                    source.Path.Equals(candidate, StringComparison.Ordinal) ||
                    source.Path.EndsWith($"/{candidate}", StringComparison.Ordinal)))
                .ToArray();
            var matchingPaths = matches
                .Select(match => match.Path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (matchingPaths.Length == 0)
            {
                yield return Diagnostic.Create(
                    RazQLDiagnostics.MissingMapperTemplateSource,
                    method.Method.Locations.FirstOrDefault(),
                    method.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", candidates));
            }
            else if (matchingPaths.Length > 1)
            {
                yield return Diagnostic.Create(
                    RazQLDiagnostics.AmbiguousMapperTemplateSource,
                    method.Method.Locations.FirstOrDefault(),
                    method.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", matchingPaths));
            }
            else
            {
                var configurationDiagnostic = ValidateConfiguration(
                    mapper,
                    method,
                    knownTypes,
                    matches);
                if (configurationDiagnostic is not null)
                {
                    yield return configurationDiagnostic;
                }
            }
        }
    }

    private static Diagnostic? ValidateConfiguration(
        MapperModel mapper,
        MapperMethodModel method,
        KnownTypeSymbols knownTypes,
        IReadOnlyList<NormalizedTemplateSource> matches)
    {
        if (SymbolEqualityComparer.Default.Equals(
                method.TemplateSourceLoaderType,
                knownTypes.ResourceTemplateSourceLoader))
        {
            return matches.Any(match => IsItemType(match.Source, "EmbeddedResource"))
                ? null
                : CreateConfigurationDiagnostic(
                    method,
                    matches[0],
                    "resource loading",
                    $"the build action must be EmbeddedResource, but it is {DescribeItemType(matches)}");
        }

        if (!SymbolEqualityComparer.Default.Equals(
                method.TemplateSourceLoaderType,
                knownTypes.FileSystemTemplateSourceLoader))
        {
            return null;
        }

        var contentMatches = matches
            .Where(match => IsItemType(match.Source, "Content"))
            .ToArray();
        if (contentMatches.Length == 0)
        {
            return CreateConfigurationDiagnostic(
                method,
                matches[0],
                "file-system loading",
                $"the build action must be Content, but it is {DescribeItemType(matches)}");
        }

        var copiedMatches = contentMatches
            .Where(match => IsCopiedToOutput(match.Source.CopyToOutputDirectory))
            .ToArray();
        if (copiedMatches.Length == 0)
        {
            return CreateConfigurationDiagnostic(
                method,
                contentMatches[0],
                "file-system loading",
                "CopyToOutputDirectory must be Always, PreserveNewest, or IfDifferent");
        }

        var targetPathCandidates = GetTemplateOutputPathCandidates(method, mapper).ToArray();
        if (copiedMatches.Any(match =>
                match.Source.TargetPath is { } targetPath &&
                targetPathCandidates.Contains(NormalizePath(targetPath), StringComparer.Ordinal)))
        {
            return null;
        }

        return CreateConfigurationDiagnostic(
            method,
            copiedMatches[0],
            "file-system loading",
            $"TargetPath must be one of: {string.Join(", ", targetPathCandidates)}");
    }

    private static Diagnostic CreateConfigurationDiagnostic(
        MapperMethodModel method,
        NormalizedTemplateSource source,
        string loaderDescription,
        string reason)
    {
        return Diagnostic.Create(
            RazQLDiagnostics.InvalidMapperTemplateSourceConfiguration,
            method.Method.Locations.FirstOrDefault(),
            source.Path,
            method.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            loaderDescription,
            reason);
    }

    private static string DescribeItemType(IReadOnlyList<NormalizedTemplateSource> sources)
    {
        var itemTypes = sources
            .Select(source => source.Source.ItemType ?? "unclassified")
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(" or ", itemTypes);
    }

    private static bool IsItemType(TemplateSourceModel source, string expected)
    {
        return string.Equals(source.ItemType, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCopiedToOutput(string? copyToOutputDirectory)
    {
        return string.Equals(copyToOutputDirectory, "Always", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(copyToOutputDirectory, "PreserveNewest", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(copyToOutputDirectory, "IfDifferent", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesVerifiableTemplateSourceLoader(
        MapperMethodModel method,
        KnownTypeSymbols knownTypes)
    {
        return SymbolEqualityComparer.Default.Equals(
                   method.TemplateSourceLoaderType,
                   knownTypes.FileSystemTemplateSourceLoader) ||
               SymbolEqualityComparer.Default.Equals(
                   method.TemplateSourceLoaderType,
                   knownTypes.ResourceTemplateSourceLoader);
    }

    private static IEnumerable<string> GetTemplatePathCandidates(
        MapperModel mapper,
        MapperMethodModel method,
        KnownTypeSymbols knownTypes)
    {
        var mapperSourcePath = mapper.MapperAttributeLocation?.SourceTree?.FilePath ??
                               mapper.MapperInterface.Locations.FirstOrDefault()?.SourceTree?.FilePath;
        var mapperSourceDirectory = string.IsNullOrEmpty(mapperSourcePath)
            ? null
            : Path.GetDirectoryName(mapperSourcePath);
        var templateLocation = GetTemplateLocationPath(method, knownTypes);

        return mapper.QueryGroupNameCandidates.SelectMany(groupName =>
            method.QueryNameCandidates.Select(queryName =>
                NormalizePath(Path.Combine(
                    mapperSourceDirectory ?? string.Empty,
                    templateLocation ?? string.Empty,
                    TemplateDirectoryName,
                    groupName,
                    $"{queryName}{TemplateFileSuffix}"))));
    }

    private static string? GetTemplateLocationPath(
        MapperMethodModel method,
        KnownTypeSymbols knownTypes)
    {
        if (method.TemplateLocation is null)
        {
            return null;
        }

        return SymbolEqualityComparer.Default.Equals(
            method.TemplateSourceLoaderType,
            knownTypes.ResourceTemplateSourceLoader)
            ? method.TemplateLocation
                .Replace('.', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
            : method.TemplateLocation;
    }

    private static IEnumerable<string> GetTemplateOutputPathCandidates(
        MapperMethodModel method,
        MapperModel mapper)
    {
        return mapper.QueryGroupNameCandidates.SelectMany(groupName =>
            method.QueryNameCandidates.Select(queryName =>
                NormalizePath(Path.Combine(
                    method.TemplateLocation ?? string.Empty,
                    TemplateDirectoryName,
                    groupName,
                    $"{queryName}{TemplateFileSuffix}"))));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed class NormalizedTemplateSource(
        TemplateSourceModel source,
        string path)
    {
        public TemplateSourceModel Source { get; } = source;

        public string Path { get; } = path;
    }
}
