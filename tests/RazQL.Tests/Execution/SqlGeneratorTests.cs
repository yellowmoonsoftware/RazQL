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
        var template = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<QueryCriteria, QueryResult>>>();
        var logger = Substitute.For<ILogger<SqlGenerator>>();
        var bindingFactory = Substitute.For<IDataBinderContextFactory>();
        var binder = Substitute.For<IDataBinder<QueryCriteria>>();
        var parameters = Substitute.For<IParameterBag>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria { Name = "Petty" };
        using var cancellation = new CancellationTokenSource();
        RazQLModel<QueryCriteria, QueryResult>? initializedModel = null;
        templateCache.GetTemplateAsync<QueryCriteria, QueryResult>(descriptor, cancellation.Token).Returns(template);
        bindingFactory.Create(criteria).Returns(new DataBinderContext<QueryCriteria>(binder, parameters));
        template.RunAsync(Arg.Any<Action<RazQLModel<QueryCriteria, QueryResult>>>())
            .Returns(callInfo =>
            {
                var initializer = callInfo.Arg<Action<RazQLModel<QueryCriteria, QueryResult>>>();
                var model = new RazQLModel<QueryCriteria, QueryResult>
                {
                    Model = Substitute.For<IDataBinder<QueryCriteria>>(),
                    ResultMapBuilder = new ResultMapBuilder<QueryResult>()
                };
                initializer(model);
                model.ResultMap(map => map.Identity(value => value.Id));
                initializedModel = model;
                return Task.FromResult("rendered sql");
            });
        var generator = new SqlGenerator(templateCache, bindingFactory, logger);

        var result = await generator.ApplyCriteriaAsync<QueryCriteria, QueryResult>(descriptor, criteria, cancellation.Token);

        Assert.Equal("rendered sql", result.Sql);
        Assert.Same(binder, initializedModel?.Model);
        Assert.Same(parameters, result.Parameters);
        Assert.Single(result.ResultMap.IdentitySelectors);
        Assert.Empty(result.ResultMap.Relations);
        bindingFactory.Received(1).Create(criteria);
        await templateCache.Received(1).GetTemplateAsync<QueryCriteria, QueryResult>(descriptor, cancellation.Token);
        await template.Received(1).RunAsync(Arg.Any<Action<RazQLModel<QueryCriteria, QueryResult>>>());
    }

    [Fact]
    public async Task ApplyCriteriaAsync_CollectsIndependentRelationshipMapsFromACachedTemplate()
    {
        var engine = new RazorEngine();
        var template = await engine.CompileAsync<RazQLModel<QueryCriteria, GraphRoot>>("""
            @{
                ResultMap(root => root
                    .Identity(value => value.Id)
                    .Collection<global::RazQL.Tests.Execution.GraphChild>("child_id", value => value.Children,
                        child => child.Identity(value => value.Id)));
            }
            select 1
            """);
        var descriptor = QueryDescriptor.ForExpression<IGraphSqlMapper, QueryCriteria, GraphRoot>(mapper => mapper.FindAsync);
        var cache = Substitute.For<ITemplateCache>();
        cache.GetTemplateAsync<QueryCriteria, GraphRoot>(descriptor, CancellationToken.None).Returns(template);
        var criteria = new QueryCriteria();
        var bindingFactory = Substitute.For<IDataBinderContextFactory>();
        bindingFactory.Create(criteria).Returns(_ => new DataBinderContext<QueryCriteria>(
            Substitute.For<IDataBinder<QueryCriteria>>(), Substitute.For<IParameterBag>()));
        var generator = new SqlGenerator(cache, bindingFactory, Substitute.For<ILogger<SqlGenerator>>());

        var first = await generator.ApplyCriteriaAsync<QueryCriteria, GraphRoot>(descriptor, criteria);
        var second = await generator.ApplyCriteriaAsync<QueryCriteria, GraphRoot>(descriptor, criteria);

        Assert.Contains("select 1", first.Sql);
        Assert.NotSame(first.ResultMap, second.ResultMap);
        Assert.NotSame(first.Parameters, second.Parameters);
        Assert.Equal(2, first.ResultMap.IdentitySelectors.Count);
        Assert.Equal(2, second.ResultMap.IdentitySelectors.Count);
        Assert.IsType<CollectionRelation>(Assert.Single(first.ResultMap.Relations));
        Assert.Single(second.ResultMap.Relations);
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

[RazQLMapper]
public interface IGraphSqlMapper
{
    Task<IEnumerable<GraphRoot>> FindAsync(QueryCriteria criteria, CancellationToken cancellationToken);
}

public sealed class GraphRoot
{
    public int Id { get; init; }
    public ICollection<GraphChild> Children { get; } = new List<GraphChild>();
}

public sealed class GraphChild
{
    public int Id { get; init; }
}
