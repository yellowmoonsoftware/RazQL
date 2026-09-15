using RazQL.Template;

namespace RazQL.Tests.Template;

public class QueryAttributeTemplateSourceLoaderTests
{
    [Fact]
    public async Task LoadAsync_ReturnsQueryFromMethodAttribute()
    {
        var descriptor = QueryDescriptor.ForExpression<IInlineSourceMapper, long, DescriptorResult>(
            mapper => mapper.FindAsync);
        var loader = new QueryAttributeTemplateSourceLoader();

        var source = await loader.LoadAsync(descriptor, CancellationToken.None);

        Assert.Equal("select id from artist where id = @Model.Bind()", source);
        Assert.Equal(typeof(QueryAttributeTemplateSourceLoader), descriptor.QuerySourceLoaderType);
    }

    [Fact]
    public async Task LoadAsync_RejectsDescriptorWithoutQueryAttribute()
    {
        var descriptor = QueryDescriptor.ForExpression<IInlineSourceMapper, long, DescriptorResult>(
            mapper => mapper.FindWithoutInlineSourceAsync);
        var loader = new QueryAttributeTemplateSourceLoader();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(descriptor, CancellationToken.None));

        Assert.Contains(nameof(RazQLQueryAttribute), exception.Message);
    }

    [Fact]
    public async Task LoadAsync_RejectsBlankQuerySource()
    {
        var descriptor = QueryDescriptor.ForExpression<IInlineSourceMapper, long, DescriptorResult>(
            mapper => mapper.FindWithBlankSourceAsync);
        var loader = new QueryAttributeTemplateSourceLoader();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(descriptor, CancellationToken.None));

        Assert.Contains("No effective query", exception.Message);
    }
}

[RazQLMapper]
public interface IInlineSourceMapper
{
    [RazQLQuery("select id from artist where id = @Model.Bind()")]
    Task<DescriptorResult> FindAsync(long id, CancellationToken cancellationToken);

    Task<DescriptorResult> FindWithoutInlineSourceAsync(long id, CancellationToken cancellationToken);

    [RazQLQuery("  ")]
    Task<DescriptorResult> FindWithBlankSourceAsync(long id, CancellationToken cancellationToken);
}
