using RazorEngineCore;

namespace RazQL.Template;

/// <summary>Loads, compiles, and caches Razor templates for mapper queries.</summary>
public interface ITemplateCache
{
    /// <summary>Gets the compiled template for a query and criteria type.</summary>
    /// <typeparam name="TMapper">The mapper interface that owns the query.</typeparam>
    /// <typeparam name="TCriteria">The criteria model type expected by the template.</typeparam>
    /// <typeparam name="TResult">The query method's task result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="cancellationToken">A token that cancels loading or compilation.</param>
    /// <returns>A task containing the compiled Razor template.</returns>
    Task<IRazorEngineCompiledTemplate<RazQLModel<TCriteria>>> GetTemplateAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, CancellationToken cancellationToken);
}
