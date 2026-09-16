using RazQL.Binding;

namespace RazQL.Execution;

/// <summary>Contains generated SQL and the parameters collected while rendering it.</summary>
/// <param name="Sql">The SQL text to execute.</param>
/// <param name="Parameters">The bound query parameters.</param>
public sealed record ParameterizedQueryResult(string Sql, IParameterBag Parameters);
