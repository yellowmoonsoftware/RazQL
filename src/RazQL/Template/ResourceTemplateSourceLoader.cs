using Microsoft.Extensions.Logging;

namespace RazQL.Template;

/// <summary>Loads query templates from manifest resources in the mapper interface's assembly.</summary>
/// <remarks>
/// Templates are resolved as <c>{MapperNamespace}.[TemplateLocation.]SqlTemplates.{Mapper}.{Query}.sql.cshtml</c>.
/// </remarks>
public sealed partial class ResourceTemplateSourceLoader(ILogger<ResourceTemplateSourceLoader> logger) : ITemplateSourceLoader
{
    private const string TemplateDirectoryName = "SqlTemplates";

    private IEnumerable<string> GetResourceNameCandidates(QueryDescriptor queryDescriptor)
    {
        var templateLocation = queryDescriptor.TemplateLocation?
            .Replace('\\', '.')
            .Replace('/', '.');

        return queryDescriptor.GetQueryGroupCandidates()
            .SelectMany(g => queryDescriptor.GetQueryNameCandidates()
                .Select(n => JoinResourceName(
                    queryDescriptor.MapperType.Namespace,
                    templateLocation,
                    TemplateDirectoryName,
                    g,
                    $"{n}.sql.cshtml")));
    }

    private static string JoinResourceName(params string?[] parts)
    {
        return string.Join('.', parts.OfType<string>().Where(part => part.Length != 0));
    }

    /// <inheritdoc />
    public async Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken)
    {
        var resourceNameCandidates = GetResourceNameCandidates(queryDescriptor).ToList();

        var targetAssembly = queryDescriptor.MapperType.Assembly;

        var resource = targetAssembly.GetManifestResourceNames().SingleOrDefault(resourceName =>
            resourceNameCandidates.Contains(resourceName, StringComparer.Ordinal));

        if (resource is null)
        {
            throw new InvalidOperationException(
                $"Template source for query method [{queryDescriptor.MapperType.Name}.{queryDescriptor.QueryMethod.Name}] not available as any of the following resources for assembly {targetAssembly.GetName().FullName}: " +
                $"[\n  {string.Join(",\n  ", resourceNameCandidates)}\n]");
        }

        await using var stream = targetAssembly.GetManifestResourceStream(resource);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to get resource stream for template resource {resource}");
        }

        LogTemplateLoadAttempt(queryDescriptor, resource);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "Attempting to load template for [{Descriptor}] from resource: [{ResourceName}]")]
    private partial void LogTemplateLoadAttempt(QueryDescriptor descriptor, string resourceName);
}
