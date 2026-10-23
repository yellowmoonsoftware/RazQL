using RazQL.Binding;

namespace RazQL.Dapper.Mapping;

/// <summary>Converts provider-neutral relationship maps into Dapper multi-mapping plans.</summary>
public static class ResultMapExtensions
{
    private class Indexer(int initial = 0)
    {
        public int Value { get; private set; } = initial;

        public int GetValueThenIncrement(int amount = 1)
        {
            var value = Value;
            Value += amount;
            return value;
        }
    }
    
    /// <summary>Flattens a relationship tree in the depth-first order required by the selected column groups.</summary>
    /// <typeparam name="T">The root row-object type.</typeparam>
    /// <param name="resultMap">The relationship tree and query-wide identity providers.</param>
    /// <returns>The mapped types, split columns, and owner-child indices.</returns>
    public static CorrelatedSequentialResultMap ToCorrelatedSequentialResultMap<T>(this ResultMap<T> resultMap)
    {
        var typeCollector = new List<Type> { typeof(T) };
        var columns = new List<string>();
        var flatRelations = resultMap.Relations.Flatten((rel, i, p) =>
        {
            typeCollector.Add(rel.ChildType);
            columns.Add(rel.Column);
            return new CorrelatedRelation(rel, i, p);
        }, new Indexer(1)).ToList();
        
        return new CorrelatedSequentialResultMap(typeCollector.ToArray(), string.Join(",", columns), flatRelations,
            resultMap.IdentitySelectors);
    }

    private static IEnumerable<TResult> Flatten<TResult>(this IEnumerable<Relation> relations,
        Func<Relation, int, int, TResult> callback,
        Indexer indexer,
        int parentIndex = 0)
    {
        return relations.SelectMany<Relation, TResult>((rel, idx) =>
        {
            var currObjIndex = indexer.GetValueThenIncrement();
            return
            [
                callback(rel, currObjIndex, parentIndex),
                .. rel.ChildRelations.Flatten(callback, indexer, currObjIndex)
            ];
        });
    }
}
