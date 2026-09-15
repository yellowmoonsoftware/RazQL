using Dapper;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Evaluates a query template and produces SQL with its bound Dapper parameters.</summary>
public interface ISqlGenerator
{
    /// <summary>Applies criteria to a compiled query template.</summary>
    /// <typeparam name="TCriteria">The criteria model type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels template loading or compilation.</param>
    /// <returns>A task containing the generated SQL and its Dapper parameters.</returns>
    Task<(string sql, DynamicParameters @params)> ApplyCriteriaAsync<TCriteria>(QueryDescriptor descriptor, TCriteria criteria, CancellationToken cancellationToken = default);
}
