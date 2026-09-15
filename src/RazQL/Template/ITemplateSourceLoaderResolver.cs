namespace RazQL.Template;

/// <summary>Resolves the template source loader selected by a query descriptor.</summary>
public interface ITemplateSourceLoaderResolver
{
    /// <summary>Resolves the loader for a mapper query.</summary>
    /// <param name="queryDescriptor">The mapper query descriptor.</param>
    /// <returns>The selected template source loader.</returns>
    /// <exception cref="InvalidOperationException">The selected loader is unavailable.</exception>
    ITemplateSourceLoader Resolve(QueryDescriptor queryDescriptor);
}
