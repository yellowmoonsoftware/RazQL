namespace RazQL.Dapper.Mapping;

/// <summary>Distinguishes a mapped result, including null or a default value, from a row to skip.</summary>
/// <typeparam name="TResult">The mapped result type.</typeparam>
/// <param name="HasResult">Whether this row contributes a result.</param>
/// <param name="Value">The mapped value when <paramref name="HasResult"/> is true.</param>
public readonly record struct MappedResult<TResult>(bool HasResult, TResult? Value);
