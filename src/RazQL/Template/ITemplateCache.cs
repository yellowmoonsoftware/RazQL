using RazorEngineCore;

namespace RazQL.Template;

/// <summary>Loads, compiles, and caches Razor templates for mapper queries.</summary>
public interface ITemplateCache
{
    /// <summary>Gets the compiled template for a query, criteria type, and mapped row-object type.</summary>
    /// <typeparam name="TCriteria">The criteria model type expected by the template.</typeparam>
    /// <typeparam name="TResult">The mapped row-object type, even when the query returns a sequence.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="cancellationToken">A token that cancels loading or compilation.</param>
    /// <returns>A task containing the compiled Razor template.</returns>
    Task<IRazorEngineCompiledTemplate<RazQLModel<TCriteria, TResult>>> GetTemplateAsync<TCriteria, TResult>(QueryDescriptor descriptor, CancellationToken cancellationToken);
}
