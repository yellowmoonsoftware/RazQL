using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Evaluates a query template and produces SQL with its bound parameters.</summary>
public interface ISqlGenerator
{
    /// <summary>Applies criteria to a compiled query template.</summary>
    /// <typeparam name="TMapper">The mapper interface that owns the query.</typeparam>
    /// <typeparam name="TCriteria">The criteria model type.</typeparam>
    /// <typeparam name="TResult">The query method's task result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels template loading or compilation.</param>
    /// <returns>A task containing the generated SQL and its parameters.</returns>
    Task<ParameterizedQueryResult> ApplyCriteriaAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, TCriteria criteria, CancellationToken cancellationToken = default);
}
