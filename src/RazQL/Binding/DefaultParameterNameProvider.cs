using RazQL.Internal;

namespace RazQL.Binding;

/// <summary>Provides the default stable, unique, and nested SQL parameter naming behavior.</summary>
public sealed class DefaultParameterNameProvider : IParameterNameProvider
{
    enum NameRegistrationType
    {
        Generated,
        Stable
    };

    private readonly IDictionary<string, NameRegistrationType> _paramRegistry;

    private readonly IDictionary<string, string> _stableNameCollisions;

    private readonly string _prefix;

    /// <summary>Creates a parameter-name provider with an empty registration scope.</summary>
    public DefaultParameterNameProvider() : this(null, new Dictionary<string, NameRegistrationType>(),
        new Dictionary<string, string>())
    {
    }

    private DefaultParameterNameProvider(string? prefix, IDictionary<string, NameRegistrationType> paramRegistry,
        IDictionary<string, string> stableNameCollisions)
    {
        _prefix = prefix?.Trim() ?? "";
        _paramRegistry = paramRegistry;
        _stableNameCollisions = stableNameCollisions;
    }

    /// <inheritdoc />
    public string GetStableName(string baseName)
    {
        var nameCandidate = BuildName(_prefix, baseName);

        if (_paramRegistry.GetOrAdd(nameCandidate, _ => NameRegistrationType.Stable) == NameRegistrationType.Stable)
        {
            return nameCandidate;
        }

        return _stableNameCollisions.GetOrAdd(nameCandidate, nc =>
        {
            var uniqueStableName = CreateUniqueName(nc);
            _paramRegistry.Add(uniqueStableName, NameRegistrationType.Generated);
            return uniqueStableName;
        });
    }

    /// <inheritdoc />
    public string GetUniqueName(string baseName)
    {
        var uniqueName = CreateUniqueName(BuildName(_prefix, baseName));
        _paramRegistry.Add(uniqueName, NameRegistrationType.Generated);
        return uniqueName;
    }

    private string CreateUniqueName(string baseName)
    {
        var i = 0;
        string nameCandidate;
        do
        {
            nameCandidate = $"{baseName}_{++i:x2}";
        } while (_paramRegistry.ContainsKey(nameCandidate));

        return nameCandidate;
    }

    private static string BuildName(params string?[] strings) => string.Join("_", strings.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <inheritdoc />
    public IParameterNameProvider WithPrefix(string prefix, IParameterNameProvider.EnumeratingContext? enumeratingContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var newPrefix = BuildName(_prefix, prefix, enumeratingContext?.Index.ToString());
        return new DefaultParameterNameProvider(newPrefix, _paramRegistry, _stableNameCollisions);
    }
}
