using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;

namespace RazQL.Dapper;

/// <summary>Bridges Dapper's arbitrary-type multi-mapping implementation with command definitions.</summary>
/// <remarks>
/// Dapper 2.1.86 does not expose a public overload accepting both a type array and a command definition.
/// A cached delegate to its private implementation preserves command options, including cancellation.
/// </remarks>
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
    
    /// <summary>Executes arbitrary-type multi-mapping while preserving the supplied command definition.</summary>
    /// <typeparam name="TResult">The mapping callback's return type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="commandDefinition">The SQL, parameters, transaction, buffering flags, and cancellation token.</param>
    /// <param name="types">The object types in contiguous column-group order.</param>
    /// <param name="map">The callback invoked with the objects materialized for each row.</param>
    /// <param name="splitOn">The comma-separated first-column names for groups after the root.</param>
    /// <returns>The callback results, buffered when requested by the command.</returns>
    public static Task<IEnumerable<TResult>> QueryMultiMapAsync<TResult>(this IDbConnection connection, CommandDefinition commandDefinition, Type[] types, Func<object[], TResult> map, string splitOn)
    {
        var multiMapDelegate = GetDelegate<TResult>();
        return multiMapDelegate(connection, commandDefinition, types, map, splitOn);
    }

    /// <summary>Executes multi-mapping and requires at most one callback result.</summary>
    /// <typeparam name="TResult">The mapping callback's return type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="commandDefinition">The command definition to preserve.</param>
    /// <param name="types">The object types in contiguous column-group order.</param>
    /// <param name="map">The callback invoked for each physical result row.</param>
    /// <param name="splitOn">The comma-separated split-column names.</param>
    /// <returns>The callback result, or its default value when there are no rows.</returns>
    /// <exception cref="InvalidOperationException">More than one callback result is produced.</exception>
    /// <remarks>This helper checks physical callback results; graph-root filtering is performed by the execution adapter.</remarks>
    public static async Task<TResult?> QuerySingleOrDefaultMultiMapAsync<TResult>(this IDbConnection connection,
        CommandDefinition commandDefinition, Type[] types, Func<object[], TResult> map, string splitOn)
    {
        var result = await connection.QueryMultiMapAsync<TResult>(commandDefinition, types, map, splitOn);
        return result.SingleOrDefault();
    }
}
