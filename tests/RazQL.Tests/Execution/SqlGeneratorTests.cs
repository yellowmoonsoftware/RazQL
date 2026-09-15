using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RazorEngineCore;
using RazQL.Binding;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.Tests.Execution;

public class SqlGeneratorTests
{
    [Fact]
    public async Task ApplyCriteriaAsync_UsesCachedTemplateAndInitializesItsDataBinder()
    {
        var templateCache = Substitute.For<ITemplateCache>();
        var template = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<SqlCriteria>>>();
        var logger = Substitute.For<ILogger<SqlGenerator>>();
        var descriptor = CreateDescriptor();
        var criteria = new SqlCriteria { Name = "Petty" };
        using var cancellation = new CancellationTokenSource();
        RazQLModel<SqlCriteria>? initializedModel = null;
        templateCache.GetTemplateAsync<SqlCriteria>(descriptor, cancellation.Token).Returns(template);
        template.RunAsync(Arg.Any<Action<RazQLModel<SqlCriteria>>>())
            .Returns(callInfo =>
            {
                var initializer = callInfo.Arg<Action<RazQLModel<SqlCriteria>>>();
                var model = new RazQLModel<SqlCriteria>
                {
                    Model = Substitute.For<IDataBinder<SqlCriteria>>()
                };
                initializer(model);
                initializedModel = model;
                return Task.FromResult("rendered sql");
            });
        var generator = new SqlGenerator(templateCache, logger);

        var (sql, parameters) = await generator.ApplyCriteriaAsync(descriptor, criteria, cancellation.Token);

        Assert.Equal("rendered sql", sql);
        Assert.Empty(parameters.ParameterNames);
        Assert.IsType<DataBinder<SqlCriteria>>(initializedModel?.Model);
        await templateCache.Received(1).GetTemplateAsync<SqlCriteria>(descriptor, cancellation.Token);
        await template.Received(1).RunAsync(Arg.Any<Action<RazQLModel<SqlCriteria>>>());
    }

    private static QueryDescriptor CreateDescriptor() =>
        QueryDescriptor.ForExpression<ISqlMapper, QueryCriteria, QueryResult>(mapper => mapper.FindAsync);
}

public sealed class SqlCriteria
{
    public string Name { get; init; } = "";
}

[RazQLMapper]
public interface ISqlMapper
{
    Task<IEnumerable<QueryResult>> FindAsync(QueryCriteria criteria, CancellationToken cancellationToken);
}
