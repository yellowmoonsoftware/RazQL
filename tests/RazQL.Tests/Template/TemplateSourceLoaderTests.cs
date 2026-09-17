using Microsoft.Extensions.Logging;
using NSubstitute;
using RazQL.Template;

namespace RazQL.Tests.Template;

public class TemplateSourceLoaderTests
{
    [Fact]
    public async Task FileSystemLoader_LoadsConventionBasedPath()
    {
        var descriptor = DescriptorFor(nameof(IAttributedMapper.LoadAsync));
        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "SqlTemplates",
            Assert.Single(descriptor.GetQueryGroupCandidates()));
        var path = Path.Combine(
            directory,
            $"{Assert.Single(descriptor.GetQueryNameCandidates())}.sql.cshtml");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, "select 1");

        try
        {
            var loader = new FileSystemTemplateSourceLoader(Substitute.For<ILogger<FileSystemTemplateSourceLoader>>());

            Assert.Equal("select 1", await loader.LoadAsync(descriptor, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task FileSystemLoader_InsertsExplicitLocationBeforeTemplateDirectory()
    {
        var descriptor = QueryDescriptor.ForExpression<ILocatedFileSystemMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var locationRoot = Path.Combine(AppContext.BaseDirectory, "razql-loader-tests");
        var directory = Path.Combine(
            locationRoot,
            "SqlTemplates",
            Assert.Single(descriptor.GetQueryGroupCandidates()));
        var path = Path.Combine(
            directory,
            $"{Assert.Single(descriptor.GetQueryNameCandidates())}.sql.cshtml");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, "select 2");

        try
        {
            var loader = new FileSystemTemplateSourceLoader(Substitute.For<ILogger<FileSystemTemplateSourceLoader>>());

            Assert.Equal("select 2", await loader.LoadAsync(descriptor, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(locationRoot, true);
        }
    }

    [Fact]
    public async Task FileSystemLoader_ThrowsWhenMultipleCandidatesExist()
    {
        var descriptor = QueryDescriptor.ForExpression<IConventionMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var firstDirectory = Path.Combine(AppContext.BaseDirectory, "SqlTemplates", "ConventionMapper");
        var secondDirectory = Path.Combine(AppContext.BaseDirectory, "SqlTemplates", "IConventionMapper");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        await File.WriteAllTextAsync(Path.Combine(firstDirectory, "Find.sql.cshtml"), "select 1");
        await File.WriteAllTextAsync(Path.Combine(secondDirectory, "FindAsync.sql.cshtml"), "select 2");

        try
        {
            var loader = new FileSystemTemplateSourceLoader(Substitute.For<ILogger<FileSystemTemplateSourceLoader>>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                loader.LoadAsync(descriptor, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(firstDirectory, true);
            Directory.Delete(secondDirectory, true);
        }
    }

    [Fact]
    public async Task FileSystemLoader_RejectsLocationOutsideApplicationBaseDirectory()
    {
        var descriptor = QueryDescriptor.ForExpression<IEscapingFileSystemMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var loader = new FileSystemTemplateSourceLoader(Substitute.For<ILogger<FileSystemTemplateSourceLoader>>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(descriptor, CancellationToken.None));

        Assert.Contains("within the application base directory", exception.Message);
    }

    [Fact]
    public async Task ResourceLoader_LoadsMatchingEmbeddedResource()
    {
        var descriptor = DescriptorFor(nameof(IAttributedMapper.LoadAsync));
        var loader = new ResourceTemplateSourceLoader(Substitute.For<ILogger<ResourceTemplateSourceLoader>>());

        var source = await loader.LoadAsync(descriptor, CancellationToken.None);

        Assert.Equal("select 'embedded';\n", source.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task ResourceLoader_LoadsFullInterfaceCandidateWithoutDuplicateMatch()
    {
        var descriptor = QueryDescriptor.ForExpression<IResourceConventionMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var loader = new ResourceTemplateSourceLoader(Substitute.For<ILogger<ResourceTemplateSourceLoader>>());

        var source = await loader.LoadAsync(descriptor, CancellationToken.None);

        Assert.Equal("select 'interface candidate';\n", source.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task ResourceLoader_UsesExplicitLocationAsResourcePrefix()
    {
        var descriptor = QueryDescriptor.ForExpression<ICustomResourceMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var loader = new ResourceTemplateSourceLoader(Substitute.For<ILogger<ResourceTemplateSourceLoader>>());

        var source = await loader.LoadAsync(descriptor, CancellationToken.None);

        Assert.Equal("select 'custom resource location';\n", source.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task ResourceLoader_ThrowsWhenResourceDoesNotExist()
    {
        var descriptor = DescriptorFor(nameof(IAttributedMapper.FindAsync));
        var loader = new ResourceTemplateSourceLoader(Substitute.For<ILogger<ResourceTemplateSourceLoader>>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(descriptor, CancellationToken.None));

        Assert.Contains("Template source for query method", exception.Message);
        Assert.Contains("RazQL.Tests.Template.SqlTemplates.Artists.Find.sql.cshtml", exception.Message);
    }

    private static QueryDescriptor DescriptorFor(string methodName) => methodName switch
    {
        nameof(IAttributedMapper.LoadAsync) =>
            QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(mapper => mapper.LoadAsync),
        nameof(IAttributedMapper.FindAsync) =>
            QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(mapper => mapper.FindAsync),
        _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, null)
    };
}

[RazQLMapper]
[RazQLTemplateSource(TemplateLocation = "  ")]
internal interface IResourceConventionMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper(Name = "LocatedFileSystemMapper")]
[RazQLTemplateSource(
    typeof(FileSystemTemplateSourceLoader),
    TemplateLocation = " razql-loader-tests ")]
internal interface ILocatedFileSystemMapper
{
    [RazQLQueryTemplateSource(TemplateName = "Find")]
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
[RazQLTemplateSource(
    typeof(FileSystemTemplateSourceLoader),
    TemplateLocation = "..")]
internal interface IEscapingFileSystemMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper(Name = "CustomResourceMapper")]
[RazQLTemplateSource(TemplateLocation = " CustomResources ")]
internal interface ICustomResourceMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}
