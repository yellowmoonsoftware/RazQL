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
        var template = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<QueryCriteria>>>();
        var logger = Substitute.For<ILogger<SqlGenerator>>();
        var bindingFactory = Substitute.For<IDataBinderContextFactory>();
        var binder = Substitute.For<IDataBinder<QueryCriteria>>();
        var parameters = Substitute.For<IParameterBag>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria { Name = "Petty" };
        using var cancellation = new CancellationTokenSource();
        RazQLModel<QueryCriteria>? initializedModel = null;
        templateCache.GetTemplateAsync(descriptor, cancellation.Token).Returns(template);
        bindingFactory.Create(criteria).Returns(new DataBinderContext<QueryCriteria>(binder, parameters));
        template.RunAsync(Arg.Any<Action<RazQLModel<QueryCriteria>>>())
            .Returns(callInfo =>
            {
                var initializer = callInfo.Arg<Action<RazQLModel<QueryCriteria>>>();
                var model = new RazQLModel<QueryCriteria>
                {
                    Model = Substitute.For<IDataBinder<QueryCriteria>>()
                };
                initializer(model);
                initializedModel = model;
                return Task.FromResult("rendered sql");
            });
        var generator = new SqlGenerator(templateCache, bindingFactory, logger);

        var result = await generator.ApplyCriteriaAsync(descriptor, criteria, cancellation.Token);

        Assert.Equal("rendered sql", result.Sql);
        Assert.Same(binder, initializedModel?.Model);
        Assert.Same(parameters, result.Parameters);
        bindingFactory.Received(1).Create(criteria);
        await templateCache.Received(1).GetTemplateAsync(descriptor, cancellation.Token);
        await template.Received(1).RunAsync(Arg.Any<Action<RazQLModel<QueryCriteria>>>());
    }

    private static QueryDescriptor<ISqlMapper, QueryCriteria, IEnumerable<QueryResult>> CreateDescriptor() =>
        QueryDescriptor.ForExpression<ISqlMapper, QueryCriteria, QueryResult>(mapper => mapper.FindAsync);
}

[RazQLMapper]
public interface ISqlMapper
{
    Task<IEnumerable<QueryResult>> FindAsync(QueryCriteria criteria, CancellationToken cancellationToken);
    Task<QueryResult> FindOneAsync(QueryCriteria criteria, CancellationToken cancellationToken);
}
