using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using RazQL.Binding;

namespace RazQL.Dapper;

public static class DapperExtensions
{
    private static readonly MethodInfo MultiMapAsyncMethod = typeof(SqlMapper)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
        .Single(m => m.Name == "MultiMapAsync" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 5 &&
                     m.GetParameters()[0].ParameterType == typeof(IDbConnection) &&
                     m.GetParameters()[1].ParameterType == typeof(CommandDefinition) &&
                     m.GetParameters()[2].ParameterType == typeof(Type[]) &&
                     m.GetParameters()[3].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>) &&   
                     m.GetParameters()[4].ParameterType == typeof(string
                     ));
    
    private static readonly ConcurrentDictionary<Type, Delegate> DelegateCache = new();

    private static Func<IDbConnection, 
        CommandDefinition, 
        Type[], 
        Func<object[], TResult>, 
        string, 
        Task<IEnumerable<TResult>>> GetDelegate<TResult>()
    {
        return (Func<IDbConnection, CommandDefinition, Type[], Func<object[], TResult>, string,
            Task<IEnumerable<TResult>>>)DelegateCache.GetOrAdd(typeof(TResult), t =>
        {
            var multiMapMethodForTResult = MultiMapAsyncMethod.MakeGenericMethod(typeof(TResult));

            return Delegate.CreateDelegate(
                typeof(Func<IDbConnection, CommandDefinition, Type[], Func<object[], TResult>, string,
                    Task<IEnumerable<TResult>>>), multiMapMethodForTResult);
        });
    }
    
    public static Task<IEnumerable<TResult>> QueryMultiMapAsync<TResult>(this IDbConnection connection, CommandDefinition commandDefinition, Type[] types, Func<object[], TResult> map, string splitOn)
    {
        var multiMapDelegate = GetDelegate<TResult>();
        return multiMapDelegate(connection, commandDefinition, types, map, splitOn);
    }

    public static async Task<TResult?> QuerySingleOrDefaultMultiMapAsync<TResult>(this IDbConnection connection,
        CommandDefinition commandDefinition, Type[] types, Func<object[], TResult> map, string splitOn)
    {
        var result = await connection.QueryMultiMapAsync<TResult>(commandDefinition, types, map, splitOn);
        return result.SingleOrDefault();
    }

    public static Func<object[], TResult> Foo<TResult>(this ResultMap<TResult> resultMap)
    {
        var (types, splitOn) = resultMap.Relations.Aggregate(
            (types: new List<Type> { typeof(TResult) }, cols: new List<string>()),
            (a, spec) => {
                a.types.Add(spec.ChildType);
                a.cols.Add(spec.Column);
                return a;
            },
            a => (a.types, string.Join(',', a.cols)));

        var pObjects = Expression.Parameter(typeof(object[]), "objects");
        var resultVar = Expression.Variable(typeof(TResult), "r");
        var returnTarget = Expression.Label(typeof(TResult));

        // var r = (TResult)objects[0]
        var resultAssignCast = Expression.Assign(resultVar, Expression.Convert(Expression.ArrayIndex(pObjects, Expression.Constant(0)), typeof(TResult)));

        var relationAssignAndCast = resultMap.Relations.Select<Relation, Expression>((spec, i) =>
        {
            var resultTargetMember = Expression.PropertyOrField(resultVar, spec.PropertyInfo.Name);
            var castObjectValue = Expression.Convert(Expression.ArrayIndex(pObjects, Expression.Constant(i + 1)),
                spec.ChildType);

            return spec switch
            {
                AssociationRelation => Expression.Assign(resultTargetMember, castObjectValue),
                CollectionRelation collSpec => Expression.Call(resultTargetMember, collSpec.MethodInfo, castObjectValue),
                _ => Expression.Empty()
            };
        }).Where(x => x != Expression.Empty()).ToList();

        var block = Expression.Block([resultVar],
            [resultAssignCast, .. relationAssignAndCast, Expression.Label(returnTarget, resultVar)]);
        
        var lambda = Expression.Lambda<Func<object[], TResult>>(block, pObjects);
        var fn = lambda.Compile();
        return fn;
    }
}