using RazQL.Template;

namespace RazQL;

/// <summary>Configures external query-template loading for every method in a mapper interface.</summary>
/// <param name="loaderType">
/// The concrete template source loader, or <see langword="null"/> to use resource loading.
/// </param>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public class RazQLTemplateSourceAttribute(Type? loaderType = null)
    : TemplateSourceLoaderAttribute(loaderType)
{
    /// <summary>
    /// Gets or sets an optional location inserted before the conventional <c>SqlTemplates</c> path.
    /// </summary>
    public string? TemplateLocation { get; set; }
}
