using RazQL.Template;

namespace RazQL.Tests.Template;

public class DefaultTemplateSourceLoaderResolverTests
{
    [Fact]
    public void Resolve_UsesResourceLoaderByDefault()
    {
        var resourceLoader = new ResourceTemplateSourceLoader();
        var resolver = new DefaultTemplateSourceLoaderResolver(
            [resourceLoader]);
        var descriptor = QueryDescriptor.ForExpression<IDefaultLoaderMapper, string, DescriptorResult>(
            mapper => mapper.DefaultAsync);

        var result = resolver.Resolve(descriptor);

        Assert.Same(resourceLoader, result);
    }

    [Fact]
    public void Resolve_UsesInterfaceLoaderWhenMethodDoesNotSpecifyType()
    {
        var defaultLoader = new DefaultTestSourceLoader();
        var resolver = new DefaultTemplateSourceLoaderResolver(
            [defaultLoader]);
        var descriptor = QueryDescriptor.ForExpression<ILoaderMapper, string, DescriptorResult>(
            mapper => mapper.LocationOnlyAsync);

        var result = resolver.Resolve(descriptor);

        Assert.Same(defaultLoader, result);
    }

    [Fact]
    public void Resolve_UsesMethodLoaderOverride()
    {
        var defaultLoader = new DefaultTestSourceLoader();
        var attributedLoader = new AttributedTestSourceLoader();
        var resolver = new DefaultTemplateSourceLoaderResolver(
            [defaultLoader, attributedLoader]);
        var descriptor = QueryDescriptor.ForExpression<ILoaderMapper, string, DescriptorResult>(
            mapper => mapper.AttributedAsync);

        var result = resolver.Resolve(descriptor);

        Assert.Same(attributedLoader, result);
    }

    [Fact]
    public void Resolve_UsesQueryAttributeTemplateSourceLoader()
    {
        var querySourceLoader = new QueryAttributeTemplateSourceLoader();
        var resolver = new DefaultTemplateSourceLoaderResolver(
            [querySourceLoader]);
        var descriptor = QueryDescriptor.ForExpression<IInlineLoaderMapper, string, DescriptorResult>(
            mapper => mapper.InlineAsync);

        var result = resolver.Resolve(descriptor);

        Assert.Same(querySourceLoader, result);
    }

    [Fact]
    public void Resolve_ThrowsWhenSelectedLoaderIsNotRegistered()
    {
        var resolver = new DefaultTemplateSourceLoaderResolver(
            []);
        var descriptor = QueryDescriptor.ForExpression<ILoaderMapper, string, DescriptorResult>(
            mapper => mapper.DefaultAsync);

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(descriptor));

        Assert.Contains(typeof(DefaultTestSourceLoader).FullName!, exception.Message);
    }

    [Fact]
    public void Constructor_RejectsDuplicateConcreteLoaderTypes()
    {
        Assert.Throws<ArgumentException>(() => new DefaultTemplateSourceLoaderResolver(
            [new DefaultTestSourceLoader(), new DefaultTestSourceLoader()]));
    }
}

[RazQLMapper]
[RazQLTemplateSource(typeof(DefaultTestSourceLoader))]
internal interface ILoaderMapper
{
    Task<DescriptorResult> DefaultAsync(string criteria, CancellationToken cancellationToken);

    [RazQLQueryTemplateSource(TemplateLocation = "LocationOnly")]
    Task<DescriptorResult> LocationOnlyAsync(string criteria, CancellationToken cancellationToken);

    [RazQLQueryTemplateSource(typeof(AttributedTestSourceLoader))]
    Task<DescriptorResult> AttributedAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
internal interface IDefaultLoaderMapper
{
    Task<DescriptorResult> DefaultAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
internal interface IInlineLoaderMapper
{
    [RazQLQuery("select 1")]
    Task<DescriptorResult> InlineAsync(string criteria, CancellationToken cancellationToken);
}

internal sealed class DefaultTestSourceLoader : ITemplateSourceLoader
{
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken) =>
        Task.FromResult("default");
}

internal sealed class AttributedTestSourceLoader : ITemplateSourceLoader
{
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken) =>
        Task.FromResult("attributed");
}
