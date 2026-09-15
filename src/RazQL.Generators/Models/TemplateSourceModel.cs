using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RazQL.Generators.Models;

internal sealed class TemplateSourceModel
{
    private const string MetadataPrefix = "build_metadata.AdditionalFiles.";

    private TemplateSourceModel(
        string path,
        string? itemType,
        string? copyToOutputDirectory,
        string? targetPath)
    {
        Path = path;
        ItemType = itemType;
        CopyToOutputDirectory = copyToOutputDirectory;
        TargetPath = targetPath;
    }

    public string Path { get; }

    public string? ItemType { get; }

    public string? CopyToOutputDirectory { get; }

    public string? TargetPath { get; }

    public static TemplateSourceModel From(
        AdditionalText source,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        var options = optionsProvider.GetOptions(source);

        return new TemplateSourceModel(
            source.Path,
            GetMetadata(options, "RazQLTemplateItemType"),
            GetMetadata(options, "CopyToOutputDirectory"),
            GetMetadata(options, "TargetPath"));
    }

    private static string? GetMetadata(AnalyzerConfigOptions options, string name)
    {
        return options.TryGetValue($"{MetadataPrefix}{name}", out var value) &&
               !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }
}
