using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;

namespace RazQL.Dapper;

/// <summary>Bridges Dapper's arbitrary-type multi-mapping implementation with command definitions.</summary>
/// <remarks>
/// Dapper 2.1.89 does not expose a public overload accepting both a type array and a command definition.
/// A cached delegate to its private implementation preserves command options, including cancellation.
/// </remarks>
public static class DapperExtensions
{
    private static readonly Lazy<MethodInfo> MultiMapAsyncMethod = new(() => FindMultiMapAsyncMethod(typeof(SqlMapper)));
    
    private static readonly ConcurrentDictionary<Type, Delegate> DelegateCache = new();

    internal static MethodInfo FindMultiMapAsyncMethod(Type mapperType)
    {
        var methods = mapperType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(IsMultiMapAsyncMethod).ToArray();
        if (methods.Length != 1)
        {
            throw CreateCompatibilityException(mapperType,
                $"Expected exactly one compatible non-public static MultiMapAsync<TResult> method; found {methods.Length}.");
        }

        return methods[0];
    }

    private static bool IsMultiMapAsyncMethod(MethodInfo method)
    {
        if (method.Name != "MultiMapAsync" || !method.IsGenericMethodDefinition || method.GetGenericArguments().Length != 1)
        {
            return false;
        }

        var resultType = method.GetGenericArguments()[0];
        if (resultType.GenericParameterAttributes != GenericParameterAttributes.None || resultType.GetGenericParameterConstraints().Length != 0)
        {
            return false;
        }

        return method.ReturnType == typeof(Task<>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(resultType)) &&
               method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(
               [
                   typeof(IDbConnection), typeof(CommandDefinition), typeof(Type[]),
                   typeof(Func<,>).MakeGenericType(typeof(object[]), resultType), typeof(string)
               ]);
    }

    private static NotSupportedException CreateCompatibilityException(Type mapperType, string reason, Exception? innerException = null)
    {
        var assembly = mapperType.Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unavailable";
        return new NotSupportedException(
            $"RazQL.Dapper cannot execute association/collection multi-mapping with loaded assembly '{assembly.GetName().Name}' " +
            $"(informational version '{version}', assembly version '{assembly.GetName().Version}'). {reason} " +
            "The required signature is Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection, CommandDefinition, Type[], Func<object[], TResult>, string). " +
            "This RazQL.Dapper build is validated with Dapper 2.1.89. Pin that Dapper package version or use a RazQL.Dapper release validated with your Dapper version. " +
            "Queries without mapped relationships do not use this bridge.", innerException);
    }

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
            var method = MultiMapAsyncMethod.Value;
            try
            {
                var multiMapMethodForTResult = method.MakeGenericMethod(typeof(TResult));
                return Delegate.CreateDelegate(
                    typeof(Func<IDbConnection, CommandDefinition, Type[], Func<object[], TResult>, string,
                        Task<IEnumerable<TResult>>>), multiMapMethodForTResult);
            }
            catch (Exception exception) when (exception is ArgumentException or MemberAccessException)
            {
                throw CreateCompatibilityException(typeof(SqlMapper), "The multi-mapping delegate could not be created.", exception);
            }
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
    /// <exception cref="NotSupportedException">The loaded Dapper assembly does not provide a compatible multi-mapping implementation.</exception>
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
    /// <exception cref="NotSupportedException">The loaded Dapper assembly does not provide a compatible multi-mapping implementation.</exception>
    /// <remarks>This helper checks physical callback results; graph-root filtering is performed by the execution adapter.</remarks>
    public static async Task<TResult?> QuerySingleOrDefaultMultiMapAsync<TResult>(this IDbConnection connection,
        CommandDefinition commandDefinition, Type[] types, Func<object[], TResult> map, string splitOn)
    {
        var result = await connection.QueryMultiMapAsync<TResult>(commandDefinition, types, map, splitOn);
        return result.SingleOrDefault();
    }
}
