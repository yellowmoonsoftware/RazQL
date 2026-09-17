using System.Reflection;
using Microsoft.Extensions.Logging;

namespace RazQL.Template;

/// <summary>Loads inline template source from <see cref="RazQLQueryAttribute"/>.</summary>
public sealed partial class QueryAttributeTemplateSourceLoader(ILogger<QueryAttributeTemplateSourceLoader> logger) : ITemplateSourceLoader
{
    /// <inheritdoc />
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken)
    {
        var razQLQueryAttribute = queryDescriptor.QueryMethod.GetCustomAttribute<RazQLQueryAttribute>();
        if (razQLQueryAttribute is null)
        {
            throw new InvalidOperationException(
                $"Use of {nameof(RazQLQueryAttribute)} is required on {queryDescriptor.MapperType.FullName}.{queryDescriptor.QueryMethod.Name}");
        }

        var querySource = razQLQueryAttribute.Query;
        if (string.IsNullOrWhiteSpace(querySource))
        {
            throw new InvalidOperationException(
                $"No effective query was specified in the {nameof(RazQLQueryAttribute)} for {queryDescriptor.MapperType.FullName}.{queryDescriptor.QueryMethod.Name}");
        }

        LogTemplateSource(queryDescriptor);
        return Task.FromResult(querySource);
    }

    [LoggerMessage(LogLevel.Information, "Using RazQLQueryAttribute as template source for [{Descriptor}]")]
    private partial void LogTemplateSource(QueryDescriptor descriptor);
}
