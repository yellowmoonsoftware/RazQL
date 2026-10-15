namespace RazQL.Dapper.Mapping;

public record CorrelatedSequentialResultMap(Type[] Types, string SplitOn, List<CorrelatedRelation> CorrelatedRelations) 
{
    public QueryResultMapper<TResult> CreateResultMapper<TResult>()
    {
        return new CachedQueryResultMapper<TResult>(this);
    }
}