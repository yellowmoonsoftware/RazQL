using System.Reflection;
using Microsoft.CodeAnalysis;
using RazQL.Generators;

namespace RazQL.Generators.Tests.Validation;

public class DiagnosticMetadataTests
{
    [Fact]
    public void Rules_HaveReleaseMetadataAndHelpPages()
    {
        var diagnosticsType = typeof(RazQLMapperGenerator).Assembly.GetType(
            "RazQL.Generators.Validation.RazQLDiagnostics", throwOnError: true)!;
        var descriptors = diagnosticsType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => Assert.IsType<DiagnosticDescriptor>(field.GetValue(null)))
            .OrderBy(descriptor => descriptor.Id, StringComparer.Ordinal)
            .ToArray();
        var documentation = ReadEmbeddedResource("Diagnostics.md");
        var releases = ReadEmbeddedResource("AnalyzerReleases.Unshipped.md");

        Assert.Equal(Enumerable.Range(1, 21).Select(index => $"RAZQL{index:000}"),
            descriptors.Select(descriptor => descriptor.Id));

        foreach (var descriptor in descriptors)
        {
            Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.Equal("RazQL", descriptor.Category);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Title.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(descriptor.MessageFormat.ToString()));
            Assert.Equal(
                $"https://github.com/yellowmoonsoftware/RazQL/blob/main/docs/diagnostics.md#{descriptor.Id.ToLowerInvariant()}",
                descriptor.HelpLinkUri);
            Assert.Contains($"## {descriptor.Id}\n", documentation);
            Assert.Contains($"{descriptor.Id} | RazQL | Error |", releases);
        }
    }

    private static string ReadEmbeddedResource(string name)
    {
        var assembly = typeof(DiagnosticMetadataTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
