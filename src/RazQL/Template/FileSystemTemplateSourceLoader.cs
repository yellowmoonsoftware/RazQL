namespace RazQL.Template;

/// <summary>Loads query templates from the application's output directory.</summary>
/// <remarks>
/// Templates are resolved beneath <c>[TemplateLocation/]SqlTemplates/{Mapper}/{Query}.sql.cshtml</c>.
/// Template locations must remain relative to <see cref="AppContext.BaseDirectory"/>.
/// </remarks>
public sealed class FileSystemTemplateSourceLoader : ITemplateSourceLoader
{
    private const string TemplateDirectoryName = "SqlTemplates";

    private IEnumerable<string> GetPathCandidates(QueryDescriptor queryDescriptor)
    {
        var templateRoot = GetTemplateRoot(queryDescriptor);

        return queryDescriptor.GetQueryGroupCandidates()
            .SelectMany(g => queryDescriptor.GetQueryNameCandidates()
                .Select(n => Path.Combine(templateRoot,
                    g,
                    $"{n}.sql.cshtml")));
    }

    private static string GetTemplateRoot(QueryDescriptor queryDescriptor)
    {
        var baseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (queryDescriptor.TemplateLocation is null)
        {
            return Path.Combine(baseDirectory, TemplateDirectoryName);
        }

        if (Path.IsPathRooted(queryDescriptor.TemplateLocation))
        {
            throw new InvalidOperationException(
                "File-system template location must be relative to the application base directory.");
        }

        var locationRoot = Path.GetFullPath(Path.Combine(baseDirectory, queryDescriptor.TemplateLocation));
        var relativeLocation = Path.GetRelativePath(baseDirectory, locationRoot);
        if (relativeLocation == ".." ||
            relativeLocation.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "File-system template location must remain within the application base directory.");
        }

        return Path.Combine(locationRoot, TemplateDirectoryName);
    }

    /// <inheritdoc />
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken)
    {
        var filePathCandidates = GetPathCandidates(queryDescriptor).ToList();
        var templateFilePath = filePathCandidates
            .SingleOrDefault(File.Exists);

        return templateFilePath is null
            ? throw new FileNotFoundException(
                $"Could not find template source file for query method [{queryDescriptor.MapperType.Name}.{queryDescriptor.QueryMethod.Name}] at any of the following locations: " +
                $"[\n  {string.Join(",\n  ", filePathCandidates)}\n]")
            : File.ReadAllTextAsync(templateFilePath, cancellationToken);
    }
}
