using Microsoft.Extensions.Logging;
using RazQL.Binding;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Default SQL generator that evaluates compiled Razor templates against a data binder.</summary>
/// <param name="templateCache">The compiled-template cache.</param>
/// <param name="dataBinderContextFactory">Creates an independent binding context for each query.</param>
/// <param name="logger">The logger used for generated SQL diagnostics.</param>
public sealed partial class SqlGenerator(ITemplateCache templateCache, IDataBinderContextFactory dataBinderContextFactory, ILogger<SqlGenerator> logger) : ISqlGenerator
{
    /// <inheritdoc />
    public async Task<ParameterizedQueryResult> ApplyCriteriaAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, TCriteria criteria, CancellationToken cancellationToken = default)
    {
        var queryTemplate = await templateCache.GetTemplateAsync(descriptor, cancellationToken);
        var (dataBinder, dbParams) = dataBinderContextFactory.Create(criteria);
        var sql = await queryTemplate.RunAsync(m =>
        {
            m.Model = dataBinder;
        });
        LogGenerateSql(sql);
        return new ParameterizedQueryResult(sql, dbParams);
    }

    [LoggerMessage(LogLevel.Debug, "Generated SQL: {sql}")]
    private partial void LogGenerateSql(string sql);
}
