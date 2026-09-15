namespace RazQL.Template;

/// <summary>Provides loader selection for RazQL template-source attributes.</summary>
/// <remarks>
/// This abstract type supports custom source attributes. Applications normally use
/// <see cref="RazQLTemplateSourceAttribute"/>, <see cref="RazQLQueryTemplateSourceAttribute"/>,
/// or <see cref="RazQLQueryAttribute"/> directly.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public abstract class TemplateSourceLoaderAttribute : Attribute
{
    /// <summary>Initializes an attribute with an optional concrete source-loader type.</summary>
    /// <param name="loaderType">
    /// A closed, concrete implementation of <see cref="ITemplateSourceLoader"/>, or <see langword="null"/> to
    /// defer to enclosing configuration and then the resource loader default.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="loaderType"/> is not a valid loader implementation.</exception>
    protected TemplateSourceLoaderAttribute(Type? loaderType = null)
    {
        if (loaderType is not null &&
            (!typeof(ITemplateSourceLoader).IsAssignableFrom(loaderType) ||
             loaderType.IsInterface ||
             loaderType.IsAbstract ||
             loaderType.ContainsGenericParameters))
        {
            throw new ArgumentException(
                $"Type {loaderType} must be a concrete, closed implementation of {nameof(ITemplateSourceLoader)}.",
                nameof(loaderType));
        }

        LoaderType = loaderType;
    }

    /// <summary>Gets the explicitly selected loader type, if any.</summary>
    public Type? LoaderType { get; }
}
