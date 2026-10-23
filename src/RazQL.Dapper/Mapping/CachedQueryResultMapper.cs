namespace RazQL.Dapper.Mapping;

/// <summary>Assembles relationship graphs using canonical objects shared throughout one query execution.</summary>
/// <typeparam name="TResult">The root row-object type.</typeparam>
/// <param name="resultMap">The column groups, relationship indices, and identity providers.</param>
public class CachedQueryResultMapper<TResult>(CorrelatedSequentialResultMap resultMap) : QueryResultMapper<TResult>
{
    private readonly ObjectCache _cache = new(resultMap.IdentitySelectors);
    private readonly HashSet<ObjectCache.CacheValue> _roots = new(ReferenceEqualityComparer.Instance);

    /// <inheritdoc />
    public (TResult, bool) ApplyResultRow(object[] objects)
    {
        var (root, _) = _cache.Get(objects[0], typeof(TResult));
        var newlyAdded = _roots.Add(root);

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
