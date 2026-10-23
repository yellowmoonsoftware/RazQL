using RazQL.Binding;

namespace RazQL.Execution;

/// <summary>Provides SQL and bound parameters independently of the mapped result type.</summary>
/// <param name="Sql">The SQL text to execute.</param>
/// <param name="Parameters">The bound query parameters.</param>
public abstract record ParameterizedQueryResult(string Sql, IParameterBag Parameters);

/// <summary>Contains generated SQL, bound parameters, and the result map collected while rendering.</summary>
/// <typeparam name="TResult">The mapped row-object type.</typeparam>
/// <param name="Sql">The SQL text to execute.</param>
/// <param name="Parameters">The bound query parameters.</param>
/// <param name="ResultMap">The result identities and relationships declared by the template.</param>
public sealed record ParameterizedQueryResult<TResult>(
    string Sql,
    IParameterBag Parameters,
    ResultMap<TResult> ResultMap) : ParameterizedQueryResult(Sql, Parameters);
