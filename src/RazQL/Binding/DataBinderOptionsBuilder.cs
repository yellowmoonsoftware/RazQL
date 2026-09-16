namespace RazQL.Binding;

/// <summary>Combines data-binder clause mappings before creating immutable options.</summary>
public sealed class DataBinderOptionsBuilder : IDataBinderOptionsBuilder
{
    private readonly Dictionary<OrderByDirection, string> _direction =
        DataBinderOptions.DefaultOrderByDirectionClause.ToDictionary();

    private readonly Dictionary<OrderByNulls, string> _nulls =
        DataBinderOptions.DefaultOrderByNullsClause.ToDictionary();

    /// <inheritdoc />
    public IDataBinderOptionsBuilder WithOrderByDirectionClause(OrderByDirection direction, string clause)
    {
        _direction[direction] = clause;
        return this;
    }

    /// <inheritdoc />
    public IDataBinderOptionsBuilder WithOrderByDirection(IReadOnlyDictionary<OrderByDirection, string> mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        foreach (var keyValuePair in mapping)
        {
            _direction[keyValuePair.Key] = keyValuePair.Value;
        }
        return this;
    }

    /// <inheritdoc />
    public IDataBinderOptionsBuilder WithOrderByNullsClause(OrderByNulls nullsOrder, string clause)
    {
        _nulls[nullsOrder] = clause;
        return this;
    }

    /// <inheritdoc />
    public IDataBinderOptionsBuilder WithOrderByNulls(IReadOnlyDictionary<OrderByNulls, string> mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        foreach (var keyValuePair in mapping)
        {
            _nulls[keyValuePair.Key] = keyValuePair.Value;
        }
        return this;
    }

    /// <inheritdoc />
    public IDataBinderOptionsBuilder WithOptions(DataBinderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _direction.Clear();
        _nulls.Clear();
        WithOrderByDirection(options.OrderByDirectionClause);
        WithOrderByNulls(options.OrderByNullsClause);
        return this;
    }

    /// <summary>Creates an immutable snapshot of the current mappings.</summary>
    /// <returns>The completed data-binder options.</returns>
    public DataBinderOptions Build() => new(_direction, _nulls);
}
