namespace RazQL.Dapper.Mapping;

public interface QueryResultMapper<TResult>
{
    (TResult, bool) ApplyResultRow(object[] objects);

    Func<object[], MappedResult<TResult>> MultiMapFunction => objects =>
    {
        var (result, isNew) = ApplyResultRow(objects);
        return isNew ? new MappedResult<TResult>(true, result) : default;
    };
}
