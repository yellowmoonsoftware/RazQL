using System.Data.Common;
using Dapper;
using RazQL.Dapper.Mapping;
using RazQL.Execution;

namespace RazQL.Dapper;

/// <summary>Executes RazQL commands through Dapper.</summary>
public sealed class DapperExecutionAdapter : IExecutionAdapter
{
    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult<TResult> parameterizedQueryResult,
        CancellationToken cancellationToken)
    {
        var commandDefinition = parameterizedQueryResult.ToCommandDefinition(cancellationToken);

        if (!parameterizedQueryResult.ResultMap.HasMappedAssociations)
        {
            return await connection.QueryAsync<TResult>(commandDefinition);
        }
        
        var sequentialResultMap = parameterizedQueryResult.ResultMap.ToCorrelatedSequentialResultMap();
        var resultMapper = sequentialResultMap.CreateResultMapper<TResult>();

        var resultMapperMultiMapFunction = resultMapper.MultiMapFunction;

        var result = await connection.QueryMultiMapAsync<TResult?>(commandDefinition,
            sequentialResultMap.Types,
            resultMapperMultiMapFunction,
            sequentialResultMap.SplitOn);

        return result.Where(r => r != null).Cast<TResult>();
    }

    /// <inheritdoc />
    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult<TResult> parameterizedQueryResult,
        CancellationToken cancellationToken)
    {
        var commandDefinition = parameterizedQueryResult.ToCommandDefinition(cancellationToken);

        if (!parameterizedQueryResult.ResultMap.HasMappedAssociations)
        {
            return connection.QuerySingleOrDefaultAsync<TResult>(commandDefinition);
        }
        
        var sequentialResultMap = parameterizedQueryResult.ResultMap.ToCorrelatedSequentialResultMap();
        var resultMapper = sequentialResultMap.CreateResultMapper<TResult>();

        return connection.QuerySingleOrDefaultMultiMapAsync(commandDefinition, sequentialResultMap.Types,
            resultMapper.MultiMapFunction, sequentialResultMap.SplitOn);
    }
}
