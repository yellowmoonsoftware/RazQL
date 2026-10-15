using RazQL.Binding;

namespace RazQL.Dapper.Mapping;

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
        
        return new CorrelatedSequentialResultMap(typeCollector.ToArray(), string.Join(",", columns), flatRelations);
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