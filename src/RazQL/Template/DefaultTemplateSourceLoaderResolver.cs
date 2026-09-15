using System.Collections.Frozen;

namespace RazQL.Template;

/// <summary>Resolves singleton loaders by their concrete type.</summary>
/// <param name="loaders">The available template source loaders.</param>
public class DefaultTemplateSourceLoaderResolver(IEnumerable<ITemplateSourceLoader> loaders) : ITemplateSourceLoaderResolver
{
    private readonly FrozenDictionary<Type, ITemplateSourceLoader> _loaderMap = loaders.ToFrozenDictionary(l => l.GetType());

    /// <inheritdoc />
    public ITemplateSourceLoader Resolve(QueryDescriptor queryDescriptor)
    {
        var loaderType = queryDescriptor.QuerySourceLoaderType;
        if (!_loaderMap.TryGetValue(loaderType, out var loaderInstance))
        {
            throw new InvalidOperationException($"No template source loader of type {loaderType.FullName} is registered.");
        }

        return loaderInstance;
    }
}
