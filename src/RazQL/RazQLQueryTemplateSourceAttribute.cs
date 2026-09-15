namespace RazQL;

/// <summary>Overrides external query-template loading for one mapper method.</summary>
/// <param name="loaderType">
/// The concrete template source loader, or <see langword="null"/> to inherit mapper-level loader selection.
/// </param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RazQLQueryTemplateSourceAttribute(Type? loaderType = null)
    : RazQLTemplateSourceAttribute(loaderType)
{
    /// <summary>Gets or sets an explicit template name in place of method-name conventions.</summary>
    public string? TemplateName { get; set; }
}
