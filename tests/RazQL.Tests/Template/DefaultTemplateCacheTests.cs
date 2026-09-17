using Microsoft.Extensions.Logging;
using NSubstitute;
using RazorEngineCore;
using RazQL.Template;

namespace RazQL.Tests.Template;

public class DefaultTemplateCacheTests
{
    private const string TemplateSource = "template source";

    [Fact]
    public async Task GetTemplateAsync_ResolvesLoaderAndCompilesTemplateSource()
    {
        using var cancellation = new CancellationTokenSource();
        var descriptor = CreateDescriptor();
        var resolver = Substitute.For<ITemplateSourceLoaderResolver>();
        var loader = Substitute.For<ITemplateSourceLoader>();
        var engine = Substitute.For<IRazorEngine>();
        var compiledTemplate = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<string>>>();
        resolver.Resolve(descriptor).Returns(loader);
        loader.LoadAsync(descriptor, cancellation.Token).Returns(TemplateSource);
        engine.CompileAsync<RazQLModel<string>>(
                TemplateSource,
                Arg.Any<Action<IRazorEngineCompilationOptionsBuilder>>(),
                cancellation.Token)
            .Returns(compiledTemplate);
        var cache = new DefaultTemplateCache(
            engine,
            resolver,
            Substitute.For<ILogger<DefaultTemplateCache>>());

        var result = await cache.GetTemplateAsync(descriptor, cancellation.Token);

        Assert.Same(compiledTemplate, result);
        resolver.Received(1).Resolve(descriptor);
        await loader.Received(1).LoadAsync(descriptor, cancellation.Token);
        await engine.Received(1).CompileAsync<RazQLModel<string>>(
            TemplateSource,
            Arg.Any<Action<IRazorEngineCompilationOptionsBuilder>>(),
            cancellation.Token);
    }

    [Fact]
    public async Task GetTemplateAsync_CachesByEquivalentDescriptor()
    {
        var firstDescriptor = CreateDescriptor();
        var equivalentDescriptor = CreateDescriptor();
        var resolver = Substitute.For<ITemplateSourceLoaderResolver>();
        var loader = Substitute.For<ITemplateSourceLoader>();
        var engine = Substitute.For<IRazorEngine>();
        var compiledTemplate = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<string>>>();
        resolver.Resolve(firstDescriptor).Returns(loader);
        loader.LoadAsync(firstDescriptor, CancellationToken.None).Returns(TemplateSource);
        engine.CompileAsync<RazQLModel<string>>(
                TemplateSource,
                Arg.Any<Action<IRazorEngineCompilationOptionsBuilder>>(),
                CancellationToken.None)
            .Returns(compiledTemplate);
        var cache = new DefaultTemplateCache(
            engine,
            resolver,
            Substitute.For<ILogger<DefaultTemplateCache>>());

        var first = await cache.GetTemplateAsync(firstDescriptor, CancellationToken.None);
        var second = await cache.GetTemplateAsync(equivalentDescriptor, CancellationToken.None);

        Assert.Same(first, second);
        resolver.Received(1).Resolve(firstDescriptor);
        await loader.Received(1).LoadAsync(firstDescriptor, CancellationToken.None);
        await engine.Received(1).CompileAsync<RazQLModel<string>>(
            TemplateSource,
            Arg.Any<Action<IRazorEngineCompilationOptionsBuilder>>(),
            CancellationToken.None);
    }

    private static QueryDescriptor<IAttributedMapper, string, DescriptorResult> CreateDescriptor() =>
        QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(mapper => mapper.LoadAsync);
}
