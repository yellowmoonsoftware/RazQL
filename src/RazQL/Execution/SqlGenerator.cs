using Dapper;
using Microsoft.Extensions.Logging;
using RazQL.Binding;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Default SQL generator that evaluates compiled Razor templates against a data binder.</summary>
/// <param name="templateCache">The compiled-template cache.</param>
/// <param name="logger">The logger used for generated SQL diagnostics.</param>
public sealed class SqlGenerator(ITemplateCache templateCache, ILogger<SqlGenerator> logger) : ISqlGenerator
{
    /// <inheritdoc />
    public async Task<(string sql, DynamicParameters @params)> ApplyCriteriaAsync<TCriteria>(QueryDescriptor descriptor, TCriteria criteria, CancellationToken cancellationToken = default)
    {
        var queryTemplate = await templateCache.GetTemplateAsync<TCriteria>(descriptor, cancellationToken);
        var dbParams = new DynamicParameters();
        var sql = await queryTemplate.RunAsync(m =>
        {
            m.Model = new DataBinder<TCriteria>(criteria, dbParams, new DefaultParameterNameProvider());
        });
        logger.LogDebug(sql);
        return (sql, dbParams);
    }
}
