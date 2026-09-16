namespace RazQL.Binding;

/// <summary>Stores query parameters in a dictionary until an execution adapter consumes them.</summary>
/// <param name="parameters">The dictionary to populate with named parameters.</param>
public sealed class DictionaryParameterBag(IDictionary<string, object?> parameters) : IParameterBag
{
    private readonly IDictionary<string, object?> _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

    /// <summary>Creates an empty parameter bag.</summary>
    public DictionaryParameterBag() : this(new Dictionary<string, object?>()) { }

    /// <inheritdoc />
    public bool AddIfAbsent<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(valueFactory);
        if (_parameters.ContainsKey(name))
            return false;

        _parameters.Add(name, valueFactory(args));
        return true;
    }

    /// <inheritdoc />
    public void Add<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(valueFactory);
        _parameters.Add(name, valueFactory(args));
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, object?>> GetParameters()
    {
        return _parameters;
    }
}
