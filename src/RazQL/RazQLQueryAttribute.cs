using RazQL.Template;

namespace RazQL;

/// <summary>Supplies inline Razor query-template source for a mapper method.</summary>
/// <param name="query">The Razor template source that produces SQL.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RazQLQueryAttribute(string query)
    : TemplateSourceLoaderAttribute(typeof(QueryAttributeTemplateSourceLoader))
{
    /// <summary>Gets the inline Razor query-template source.</summary>
    public string Query => query;
}
