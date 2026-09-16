using Dapper;
using RazQL.Binding;
using RazQL.Execution;

namespace RazQL.Dapper;

/// <summary>Converts provider-neutral generated queries into Dapper commands.</summary>
public static class DapperGeneratedSqlExtensions
{
    /// <summary>Copies named parameters into a Dapper parameter collection.</summary>
    /// <param name="parameters">The provider-neutral parameter bag.</param>
    /// <returns>A new Dapper parameter collection.</returns>
    public static DynamicParameters ToDynamicParameters(this IParameterBag parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return parameters.GetParameters()
            .Aggregate(new DynamicParameters(), (dp, p) =>
            {
                dp.Add(p.Key, p.Value);
                return dp;
            });
    }

    /// <summary>Builds a Dapper command from generated SQL and parameters.</summary>
    /// <param name="parameterizedQueryResult">The generated query to convert.</param>
    /// <param name="cancellationToken">A token that cancels the database operation.</param>
    /// <returns>A Dapper command definition.</returns>
    public static CommandDefinition ToCommandDefinition(this ParameterizedQueryResult parameterizedQueryResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameterizedQueryResult);
        return new CommandDefinition(
            commandText: parameterizedQueryResult.Sql,
            parameters: parameterizedQueryResult.Parameters.ToDynamicParameters(),
            cancellationToken: cancellationToken
        );
    }
}
