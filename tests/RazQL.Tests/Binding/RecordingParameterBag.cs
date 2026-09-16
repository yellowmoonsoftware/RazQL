using RazQL.Binding;

namespace RazQL.Tests.Binding;

internal sealed class RecordingParameterBag : IParameterBag
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public IEnumerable<string> ParameterNames => _values.Keys;

    public TValue Get<TValue>(string name) => (TValue)_values[name]!;

    public bool AddIfAbsent<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args)
    {
        if (_values.ContainsKey(name))
            return false;

        _values.Add(name, valueFactory(args));
        return true;
    }

    public void Add<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args) =>
        _values[name] = valueFactory(args);

    public IEnumerable<KeyValuePair<string, object?>> GetParameters() => _values.ToArray();
}
