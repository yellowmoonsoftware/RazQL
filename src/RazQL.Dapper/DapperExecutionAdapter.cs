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

        var result = await connection.QueryMultiMapAsync(commandDefinition,
            sequentialResultMap.Types,
            resultMapperMultiMapFunction,
            sequentialResultMap.SplitOn);

        return result.Where(row => row.HasResult).Select(row => row.Value!);
    }

    /// <inheritdoc />
    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult<TResult> parameterizedQueryResult,
        CancellationToken cancellationToken)
    {
        var commandDefinition = parameterizedQueryResult.ToCommandDefinition(cancellationToken);

        if (!parameterizedQueryResult.ResultMap.HasMappedAssociations)
        {
            return await connection.QuerySingleOrDefaultAsync<TResult>(commandDefinition);
        }
        
        var sequentialResultMap = parameterizedQueryResult.ResultMap.ToCorrelatedSequentialResultMap();
        var resultMapper = sequentialResultMap.CreateResultMapper<TResult>();

        var result = await connection.QueryMultiMapAsync(commandDefinition, sequentialResultMap.Types,
            resultMapper.MultiMapFunction, sequentialResultMap.SplitOn);

        return result.Where(row => row.HasResult).Select(row => row.Value!).SingleOrDefault();
    }
}
