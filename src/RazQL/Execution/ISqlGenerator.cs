using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Evaluates a query template and produces SQL with its bound parameters.</summary>
public interface ISqlGenerator
{
    /// <summary>Applies criteria to a compiled query template.</summary>
    /// <typeparam name="TCriteria">The criteria model type.</typeparam>
    /// <typeparam name="TResult">The mapped row-object type, even when the query returns a sequence.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels template loading or compilation.</param>
    /// <returns>A task containing the generated SQL, parameters, and result map.</returns>
    Task<ParameterizedQueryResult<TResult>> ApplyCriteriaAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria, CancellationToken cancellationToken = default);
}
