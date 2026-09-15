using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RazQL.Generators;

namespace RazQL.Generators.Tests.Validation;

public class MapperTemplateSourceDiagnosticTests
{
    private const string MapperSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using RazQL;

        namespace Consumers;

        [RazQLMapper]
        [RazQLTemplateSource(TemplateLocation = "  ")]
        public interface IArtistMapper
        {
            Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
        }

        public sealed class Artist;
        public sealed class ArtistCriteria;
        """;

    private const string CustomLoaderMapperSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using RazQL;
        using RazQL.Template;

        namespace Consumers;

        [RazQLMapper]
        [RazQLTemplateSource(typeof(CustomTemplateSourceLoader))]
        public interface IArtistMapper
        {
            [RazQLQueryTemplateSource]
            Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
        }

        public sealed class Artist;
        public sealed class ArtistCriteria;

        public sealed class CustomTemplateSourceLoader : ITemplateSourceLoader
        {
            public Task<string> LoadAsync(
                QueryDescriptor queryDescriptor,
                CancellationToken cancellationToken) => Task.FromResult(string.Empty);
        }
        """;

    [Fact]
    public void Generator_ReportsMissingTemplateSource()
    {
        var diagnostics = RunGenerator();

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL017");
        Assert.Contains("Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml", diagnostic.GetMessage());
        Assert.Contains("Consumers/SqlTemplates/IArtistMapper/FindAsync.sql.cshtml", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_AcceptsSingleMatchingTemplateSource()
    {
        var diagnostics = RunGenerator("/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml");

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_ReportsAmbiguousTemplateSources()
    {
        var diagnostics = RunGenerator(
            "/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml",
            "/project/Consumers/SqlTemplates/IArtistMapper/FindAsync.sql.cshtml");

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL018");
        Assert.Contains("ArtistMapper/Find.sql.cshtml", diagnostic.GetMessage());
        Assert.Contains("IArtistMapper/FindAsync.sql.cshtml", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_DoesNotAssumeCustomLoaderLocationConventions()
    {
        var diagnostics = RunGeneratorForSource(CustomLoaderMapperSource);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_DoesNotRequireTemplateFileForInlineQuerySource()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQuery("select id, name from artist where id = @Model.Bind()")]
                Task<Artist?> FindAsync(long id, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_ReportsBlankInlineQuerySource()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQuery("  ")]
                Task<Artist?> FindAsync(long id, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL021");
    }

    [Fact]
    public void Generator_ReportsConflictingMethodTemplateSourceAttributes()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;
            using RazQL.Template;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQuery("select id, name from artist where id = @Model.Bind()")]
                [RazQLQueryTemplateSource(typeof(FileSystemTemplateSourceLoader))]
                Task<Artist?> FindAsync(long id, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL020");
    }

    [Fact]
    public void Generator_DoesNotAssumeConventionsForUnknownDerivedSourceAttribute()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;
            using RazQL.Template;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [ApiQuery]
                Task<Artist?> FindAsync(long id, CancellationToken cancellationToken);
            }

            public sealed class Artist;

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class ApiQueryAttribute()
                : TemplateSourceLoaderAttribute(typeof(ApiTemplateSourceLoader));

            public sealed class ApiTemplateSourceLoader : ITemplateSourceLoader
            {
                public Task<string> LoadAsync(
                    QueryDescriptor queryDescriptor,
                    CancellationToken cancellationToken) => Task.FromResult(string.Empty);
            }
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_DoesNotAssumeConventionsForInheritedUnknownLoaderSelection()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;
            using RazQL.Template;

            namespace Consumers;

            [RazQLMapper]
            [ApiQueries]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource]
                Task<Artist?> FindAsync(long id, CancellationToken cancellationToken);
            }

            public sealed class Artist;

            [AttributeUsage(AttributeTargets.Interface)]
            public sealed class ApiQueriesAttribute()
                : TemplateSourceLoaderAttribute(typeof(ApiTemplateSourceLoader));

            public sealed class ApiTemplateSourceLoader : ITemplateSourceLoader
            {
                public Task<string> LoadAsync(
                    QueryDescriptor queryDescriptor,
                    CancellationToken cancellationToken) => Task.FromResult(string.Empty);
            }
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_RejectsLoaderTypeThatDoesNotImplementContract()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            [RazQLTemplateSource(typeof(string))]
            public interface IArtistMapper
            {
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL010");
    }

    [Fact]
    public void Generator_ReportsMissingTemplateAtExplicitLocation()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            [RazQLTemplateSource(TemplateLocation = "External")]
            public interface IArtistMapper
            {
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(source);

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL017");
        Assert.Contains("Consumers/External/SqlTemplates/ArtistMapper/Find.sql.cshtml", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_AcceptsTemplateAtExplicitLocation()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            [RazQLTemplateSource(TemplateLocation = "External")]
            public interface IArtistMapper
            {
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(
            source,
            "/project/Consumers/External/SqlTemplates/ArtistMapper/Find.sql.cshtml");

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_UsesMethodTemplateNameAsOnlyQueryNameCandidate()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(TemplateName = "Lookup")]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(source);

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL017");
        Assert.Contains("ArtistMapper/Lookup.sql.cshtml", diagnostic.GetMessage());
        Assert.DoesNotContain("ArtistMapper/Find.sql.cshtml", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_AcceptsTemplateMatchingMethodTemplateName()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(TemplateName = "Lookup")]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(
            source,
            "/project/Consumers/SqlTemplates/ArtistMapper/Lookup.sql.cshtml");

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_MethodTemplateConfigurationInheritsInterfaceLoader()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;
            using RazQL.Template;

            namespace Consumers;

            [RazQLMapper]
            [RazQLTemplateSource(typeof(FileSystemTemplateSourceLoader))]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(TemplateName = "Lookup")]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSourceWithTemplates(
            source,
            new TemplateFile(
                "/project/Consumers/SqlTemplates/ArtistMapper/Lookup.sql.cshtml",
                "Content",
                "PreserveNewest",
                "SqlTemplates/ArtistMapper/Lookup.sql.cshtml"));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generator_RejectsMethodLoaderTypeThatDoesNotImplementContract()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(typeof(string))]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL010");
    }

    [Fact]
    public void Generator_ReportsBlankMethodTemplateName()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(TemplateName = "  ")]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSource(source);

        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL009");
    }

    [Fact]
    public void Generator_ReportsContentTemplateSelectedForResourceLoading()
    {
        var diagnostics = RunGeneratorForSourceWithTemplates(
            MapperSource,
            new TemplateFile(
                "/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml",
                "Content",
                "PreserveNewest",
                "SqlTemplates/ArtistMapper/Find.sql.cshtml"));

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL019");
        Assert.Contains("build action must be EmbeddedResource", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_ReportsEmbeddedTemplateSelectedForFileSystemLoading()
    {
        var diagnostics = RunGeneratorForSourceWithTemplates(
            GetFileSystemMapperSource(),
            new TemplateFile(
                "/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml",
                "EmbeddedResource"));

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL019");
        Assert.Contains("build action must be Content", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_ReportsFileSystemTemplateNotCopiedToOutput()
    {
        var diagnostics = RunGeneratorForSourceWithTemplates(
            GetFileSystemMapperSource(),
            new TemplateFile(
                "/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml",
                "Content"));

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL019");
        Assert.Contains("CopyToOutputDirectory", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_ReportsFileSystemTemplateWithIncorrectTargetPath()
    {
        var diagnostics = RunGeneratorForSourceWithTemplates(
            GetFileSystemMapperSource(),
            new TemplateFile(
                "/project/Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml",
                "Content",
                "PreserveNewest",
                "Consumers/SqlTemplates/ArtistMapper/Find.sql.cshtml"));

        var diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "RAZQL019");
        Assert.Contains("TargetPath must be one of", diagnostic.GetMessage());
        Assert.Contains("SqlTemplates/ArtistMapper/Find.sql.cshtml", diagnostic.GetMessage());
    }

    [Fact]
    public void Generator_AcceptsFileSystemTemplateAtExplicitLocation()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(
                    typeof(RazQL.Template.FileSystemTemplateSourceLoader),
                    TemplateLocation = "External")]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;

        var diagnostics = RunGeneratorForSourceWithTemplates(
            source,
            new TemplateFile(
                "/project/Consumers/External/SqlTemplates/ArtistMapper/Find.sql.cshtml",
                "Content",
                "PreserveNewest",
                "External/SqlTemplates/ArtistMapper/Find.sql.cshtml"));

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(params string[] templatePaths)
    {
        return RunGeneratorForSourceWithTemplates(
            MapperSource,
            templatePaths.Select(TemplateFile.EmbeddedResource).ToArray());
    }

    private static IReadOnlyList<Diagnostic> RunGeneratorForSource(
        string mapperSource,
        params string[] templatePaths)
    {
        return RunGeneratorForSourceWithTemplates(
            mapperSource,
            templatePaths.Select(TemplateFile.EmbeddedResource).ToArray());
    }

    private static IReadOnlyList<Diagnostic> RunGeneratorForSourceWithTemplates(
        string mapperSource,
        params TemplateFile[] templateFiles)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            mapperSource,
            path: "/project/Consumers/IArtistMapper.cs");
        var compilation = CSharpCompilation.Create(
            assemblyName: "TemplateSourceDiagnosticTests",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var additionalTexts = templateFiles
            .Select(template => (AdditionalText)new TemplateAdditionalText(template.Path))
            .ToArray();
        var optionsProvider = new TemplateAnalyzerConfigOptionsProvider(templateFiles);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new RazQLMapperGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: (CSharpParseOptions)syntaxTree.Options,
            optionsProvider: optionsProvider);

        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Results
            .SelectMany(result => result.Diagnostics)
            .ToArray();
    }

    private static string GetFileSystemMapperSource()
    {
        return """
            using System.Threading;
            using System.Threading.Tasks;
            using RazQL;
            using RazQL.Template;

            namespace Consumers;

            [RazQLMapper]
            public interface IArtistMapper
            {
                [RazQLQueryTemplateSource(typeof(FileSystemTemplateSourceLoader))]
                Task<Artist?> FindAsync(ArtistCriteria criteria, CancellationToken cancellationToken);
            }

            public sealed class Artist;
            public sealed class ArtistCriteria;
            """;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.NotNull(trustedPlatformAssemblies);

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Append(typeof(RazQLMapperAttribute).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private sealed class TemplateAdditionalText(string path) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From("SELECT 1;");
        }
    }

    private sealed record TemplateFile(
        string Path,
        string ItemType,
        string? CopyToOutputDirectory = null,
        string? TargetPath = null)
    {
        public static TemplateFile EmbeddedResource(string path) =>
            new(path, "EmbeddedResource");
    }

    private sealed class TemplateAnalyzerConfigOptionsProvider(
        IEnumerable<TemplateFile> templateFiles) : AnalyzerConfigOptionsProvider
    {
        private static readonly AnalyzerConfigOptions EmptyOptions =
            new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        private readonly IReadOnlyDictionary<string, AnalyzerConfigOptions> _options = templateFiles
            .ToDictionary(
                template => template.Path,
                template => (AnalyzerConfigOptions)new TestAnalyzerConfigOptions(
                    GetMetadata(template)),
                StringComparer.Ordinal);

        public override AnalyzerConfigOptions GlobalOptions => EmptyOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            _options.TryGetValue(textFile.Path, out var options) ? options : EmptyOptions;

        private static Dictionary<string, string> GetMetadata(TemplateFile template)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_metadata.AdditionalFiles.RazQLTemplateItemType"] = template.ItemType
            };

            if (template.CopyToOutputDirectory is not null)
            {
                metadata["build_metadata.AdditionalFiles.CopyToOutputDirectory"] =
                    template.CopyToOutputDirectory;
            }

            if (template.TargetPath is not null)
            {
                metadata["build_metadata.AdditionalFiles.TargetPath"] = template.TargetPath;
            }

            return metadata;
        }
    }

    private sealed class TestAnalyzerConfigOptions(
        IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            return values.TryGetValue(key, out value!);
        }
    }
}
