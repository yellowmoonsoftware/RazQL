namespace RazQL.Template;

/// <summary>Loads Razor query-template source for a query descriptor.</summary>
public interface ITemplateSourceLoader
{
    /// <summary>Loads the template source associated with a mapper query.</summary>
    /// <param name="queryDescriptor">The mapper query descriptor.</param>
    /// <param name="cancellationToken">A token that cancels source loading.</param>
    /// <returns>A task containing Razor template source.</returns>
    Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken);
}
