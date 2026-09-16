using System.Collections.Frozen;

namespace RazQL.Binding;

/// <summary>Configures trusted SQL fragments emitted by <see cref="IDataBinder{TData}.OrderBy{TEnum}"/>.</summary>
public sealed record DataBinderOptions
{
    /// <summary>Creates options with the standard SQL direction and null-placement clauses.</summary>
    public DataBinderOptions() : this(DefaultOrderByDirectionClause, DefaultOrderByNullsClause)
    {
    }

    /// <summary>Creates options from mappings that are copied into immutable dictionaries.</summary>
    /// <param name="orderByDirectionClause">SQL fragments for sort directions.</param>
    /// <param name="orderByNullsClause">SQL fragments for null placement.</param>
    public DataBinderOptions(
        IReadOnlyDictionary<OrderByDirection, string> orderByDirectionClause,
        IReadOnlyDictionary<OrderByNulls, string> orderByNullsClause)
    {
        ArgumentNullException.ThrowIfNull(orderByDirectionClause);
        ArgumentNullException.ThrowIfNull(orderByNullsClause);

        OrderByDirectionClause = orderByDirectionClause;
        OrderByNullsClause = orderByNullsClause;
    }

    private IReadOnlyDictionary<OrderByDirection, string> _orderByDirectionClause = DefaultOrderByDirectionClause;
    private IReadOnlyDictionary<OrderByNulls, string> _orderByNullsClause = DefaultOrderByNullsClause;

    /// <summary>Gets or initializes the immutable mapping from sort directions to trusted SQL fragments.</summary>
    public IReadOnlyDictionary<OrderByDirection, string> OrderByDirectionClause
    {
        get => _orderByDirectionClause;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _orderByDirectionClause = value.ToFrozenDictionary();
        }
    }

    /// <summary>Gets or initializes the immutable mapping from null-placement choices to trusted SQL fragments.</summary>
    public IReadOnlyDictionary<OrderByNulls, string> OrderByNullsClause
    {
        get => _orderByNullsClause;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _orderByNullsClause = value.ToFrozenDictionary();
        }
    }

    /// <summary>Gets the default mapping from sort directions to SQL fragments.</summary>
    public static IReadOnlyDictionary<OrderByDirection, string> DefaultOrderByDirectionClause { get; } =
        new Dictionary<OrderByDirection, string>
        {
            { OrderByDirection.Asc, "ASC" },
            { OrderByDirection.Desc, "DESC" }
        }.ToFrozenDictionary();

    /// <summary>Gets the default mapping from null-placement choices to SQL fragments.</summary>
    public static IReadOnlyDictionary<OrderByNulls, string> DefaultOrderByNullsClause { get; } =
        new Dictionary<OrderByNulls, string>
        {
            { OrderByNulls.First, "NULLS FIRST" },
            { OrderByNulls.Last, "NULLS LAST" }
        }.ToFrozenDictionary();
}
