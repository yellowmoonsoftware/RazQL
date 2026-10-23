namespace RazQL.Dapper.Mapping;

/// <summary>Applies each multi-mapped row to a graph and identifies newly encountered result roots.</summary>
/// <typeparam name="TResult">The root row-object type.</typeparam>
public interface QueryResultMapper<TResult>
{
    /// <summary>Resolves canonical objects and applies the row's relationships.</summary>
    /// <param name="objects">The root object followed by child objects in the map's depth-first order.</param>
    /// <returns>The canonical root and whether this is its first appearance as a result root.</returns>
    (TResult, bool) ApplyResultRow(object[] objects);

    /// <summary>Gets a Dapper callback that explicitly marks repeated roots as rows to skip.</summary>
    /// <remarks>Null and default result values remain distinct from skipped rows.</remarks>
    Func<object[], MappedResult<TResult>> MultiMapFunction => objects =>
    {
        var (result, isNew) = ApplyResultRow(objects);
        return isNew ? new MappedResult<TResult>(true, result) : default;
    };
}
