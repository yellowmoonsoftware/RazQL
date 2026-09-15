using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Models;

internal sealed class KnownTypeSymbols
{
    private KnownTypeSymbols(Compilation compilation)
    {
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        GenericTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        GenericValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        GenericEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        Enumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
        GenericAsyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        GenericAsyncEnumerator = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        Enumerator = compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
        RazQLMapperAttribute = compilation.GetTypeByMetadataName("RazQL.RazQLMapperAttribute");
        RazQLQueryAttribute = compilation.GetTypeByMetadataName("RazQL.RazQLQueryAttribute");
        TemplateSourceLoaderAttribute =
            compilation.GetTypeByMetadataName("RazQL.Template.TemplateSourceLoaderAttribute");
        TemplateSourceAttribute =
            compilation.GetTypeByMetadataName("RazQL.RazQLTemplateSourceAttribute");
        QueryTemplateSourceAttribute =
            compilation.GetTypeByMetadataName("RazQL.RazQLQueryTemplateSourceAttribute");
        TemplateSourceLoader =
            compilation.GetTypeByMetadataName("RazQL.Template.ITemplateSourceLoader");
        FileSystemTemplateSourceLoader =
            compilation.GetTypeByMetadataName("RazQL.Template.FileSystemTemplateSourceLoader");
        ResourceTemplateSourceLoader =
            compilation.GetTypeByMetadataName("RazQL.Template.ResourceTemplateSourceLoader");
        QueryAttributeTemplateSourceLoader =
            compilation.GetTypeByMetadataName("RazQL.Template.QueryAttributeTemplateSourceLoader");
    }

    public INamedTypeSymbol? Task { get; }

    public INamedTypeSymbol? GenericTask { get; }

    public INamedTypeSymbol? ValueTask { get; }

    public INamedTypeSymbol? GenericValueTask { get; }

    public INamedTypeSymbol? CancellationToken { get; }

    public INamedTypeSymbol? GenericEnumerable { get; }

    public INamedTypeSymbol? Enumerable { get; }

    public INamedTypeSymbol? GenericAsyncEnumerable { get; }

    public INamedTypeSymbol? GenericAsyncEnumerator { get; }

    public INamedTypeSymbol? Enumerator { get; }

    public INamedTypeSymbol? RazQLMapperAttribute { get; }

    public INamedTypeSymbol? RazQLQueryAttribute { get; }

    public INamedTypeSymbol? TemplateSourceLoaderAttribute { get; }

    public INamedTypeSymbol? TemplateSourceAttribute { get; }

    public INamedTypeSymbol? QueryTemplateSourceAttribute { get; }

    public INamedTypeSymbol? TemplateSourceLoader { get; }

    public INamedTypeSymbol? FileSystemTemplateSourceLoader { get; }

    public INamedTypeSymbol? ResourceTemplateSourceLoader { get; }

    public INamedTypeSymbol? QueryAttributeTemplateSourceLoader { get; }

    public static KnownTypeSymbols From(Compilation compilation)
    {
        return new KnownTypeSymbols(compilation);
    }
}
