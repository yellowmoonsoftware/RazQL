namespace RazQL.Dapper.Mapping;

/// <summary>Describes a depth-first sequence of Dapper column groups and their relationship tree.</summary>
/// <param name="Types">The root type followed by child types in depth-first order.</param>
/// <param name="SplitOn">The comma-separated first-column names for child groups.</param>
/// <param name="CorrelatedRelations">The relationships and their child and owner positions.</param>
/// <param name="IdentitySelectors">The query-wide identity providers keyed by mapped type.</param>
public record CorrelatedSequentialResultMap(
    Type[] Types,
    string SplitOn,
    List<CorrelatedRelation> CorrelatedRelations,
    IReadOnlyDictionary<Type, Func<object, object?>> IdentitySelectors)
{
    /// <summary>Creates an independent graph assembler and object cache for one execution.</summary>
    /// <typeparam name="TResult">The root row-object type.</typeparam>
    /// <returns>A new result mapper with no previously seen objects or roots.</returns>
    public QueryResultMapper<TResult> CreateResultMapper<TResult>()
    {
        return new CachedQueryResultMapper<TResult>(this);
    }
}
