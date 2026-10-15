namespace RazQL.Dapper.Mapping;

public interface QueryResultMapper<TResult>
{
    (TResult, bool) ApplyResultRow(object[] objects);

    Func<object[], TResult?> MultiMapFunction => objects =>
    {
        var (result, isNew) = ApplyResultRow(objects);
        return isNew ? result : default;
    };
}