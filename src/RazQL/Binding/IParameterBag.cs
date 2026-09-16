namespace RazQL.Binding;

/// <summary>Collects named query parameters without exposing a particular execution library.</summary>
public interface IParameterBag
{
    /// <summary>Adds a parameter only if its name is absent, evaluating the value factory only when needed.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <typeparam name="TArgs">The value factory argument type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="valueFactory">Creates the value when the parameter is added.</param>
    /// <param name="args">Arguments supplied to the value factory.</param>
    /// <returns><see langword="true"/> if the parameter was added; otherwise, <see langword="false"/>.</returns>
    bool AddIfAbsent<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args);

    /// <summary>Adds a parameter only if its name is absent, evaluating the factory only when needed.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="valueFactory">Creates the value when the parameter is added.</param>
    /// <returns><see langword="true"/> if the parameter was added; otherwise, <see langword="false"/>.</returns>
    bool AddIfAbsent<TValue>(string name, Func<TValue> valueFactory) =>
        AddIfAbsent(name, factory => factory(), valueFactory);

    /// <summary>Adds a value only if its parameter name is absent.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns><see langword="true"/> if the parameter was added; otherwise, <see langword="false"/>.</returns>
    bool AddIfAbsent<TValue>(string name, TValue value) =>
        AddIfAbsent<TValue, TValue>(name, current => current, value);

    /// <summary>Adds a parameter unconditionally, evaluating the value factory.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <typeparam name="TArgs">The value factory argument type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="valueFactory">Creates the parameter value.</param>
    /// <param name="args">Arguments supplied to the value factory.</param>
    void Add<TValue, TArgs>(string name, Func<TArgs, TValue> valueFactory, TArgs args);

    /// <summary>Adds a parameter unconditionally, evaluating the value factory.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="valueFactory">Creates the parameter value.</param>
    void Add<TValue>(string name, Func<TValue> valueFactory) =>
        Add(name, factory => factory(), valueFactory);

    /// <summary>Adds a parameter value unconditionally.</summary>
    /// <typeparam name="TValue">The parameter value type.</typeparam>
    /// <param name="name">The parameter name without the SQL prefix.</param>
    /// <param name="value">The parameter value.</param>
    void Add<TValue>(string name, TValue value) =>
        Add<TValue, TValue>(name, current => current, value);

    /// <summary>Enumerates the named parameters and their values.</summary>
    /// <returns>The parameters in this bag.</returns>
    IEnumerable<KeyValuePair<string, object?>> GetParameters();
}
