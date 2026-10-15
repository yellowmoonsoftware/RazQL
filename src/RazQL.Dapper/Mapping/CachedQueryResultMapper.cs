namespace RazQL.Dapper.Mapping;

public class CachedQueryResultMapper<TResult>(CorrelatedSequentialResultMap resultMap) : QueryResultMapper<TResult>
{
    private readonly ObjectCache _cache = new();

    public (TResult, bool) ApplyResultRow(object[] objects)
    {
        var (root, newlyAdded) = _cache.Get(objects[0], typeof(TResult));

        foreach (var (relation, objectIndex, parentObjectIndex) in resultMap.CorrelatedRelations)
        {
            var (resolvedTarget, _) = _cache.Get(objects[objectIndex], relation.ChildType);
            var (resolvedOwner, _) = _cache.Get(objects[parentObjectIndex], relation.OwningType);

            if (resolvedOwner.Value == null || resolvedTarget.AssocOwners.Contains(resolvedOwner))
            {
                continue;
            }

            if (relation.ApplyRelation(resolvedOwner.Value, resolvedTarget.Value))
            {
                resolvedTarget.AssocOwners.Add(resolvedOwner);
            }
        }

        return ((TResult)root.Value!, newlyAdded);
    }
}